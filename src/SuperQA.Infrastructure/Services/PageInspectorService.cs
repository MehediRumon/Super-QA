using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;
using SuperQA.Core.Interfaces;

namespace SuperQA.Infrastructure.Services;

public class PageInspectorService : IPageInspectorService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PageInspectorService> _logger;

    public PageInspectorService(IConfiguration configuration, ILogger<PageInspectorService>? logger = null)
    {
        _configuration = configuration;
        _logger = logger ?? NullLogger<PageInspectorService>.Instance;
    }

    public async Task<string> GetPageStructureAsync(string url, string? frsText = null)
    {
        try
        {
            _logger.LogInformation("[Inspector] Starting page inspection. Url={Url}", url);
            var result = await InspectOnceAsync(url, frsText);
            LogInspectionSummary(result);
            return result;
        }
        catch (Exception ex) when (IsBrowserNotInstalled(ex))
        {
            _logger.LogWarning(ex, "[Inspector] Browser not installed. Attempting auto-install of Chromium...");
            try
            {
                TryInstallChromium();
                var retry = await InspectOnceAsync(url, frsText);
                LogInspectionSummary(retry, retried: true);
                return retry;
            }
            catch (Exception retryEx)
            {
                var errorMessage = BuildMissingBrowserMessage(retryEx.Message);
                _logger.LogError(retryEx, "[Inspector] Retry after auto-install failed. {Message}", errorMessage);
                return JsonSerializer.Serialize(new[] { new { error = $"Failed to inspect page: {errorMessage}" } });
            }
        }
        catch (Exception ex)
        {
            var errorMessage = ex.Message;
            if (IsBrowserNotInstalled(ex))
            {
                errorMessage = BuildMissingBrowserMessage(errorMessage);
            }
            _logger.LogError(ex, "[Inspector] Inspection failed. {Message}", errorMessage);
            return JsonSerializer.Serialize(new[] { new { error = $"Failed to inspect page: {errorMessage}" } });
        }
    }

    private static string[] ExtractKeywords(string? frs)
    {
        if (string.IsNullOrWhiteSpace(frs)) return Array.Empty<string>();
        
        // Extract meaningful keywords from FRS text dynamically
        var text = frs.ToLowerInvariant();
        
        // Common stop words to filter out
        var stopWords = new HashSet<string> { 
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", 
            "of", "with", "by", "from", "as", "is", "was", "are", "be", "has", 
            "have", "had", "do", "does", "did", "will", "would", "should", "could",
            "this", "that", "these", "those", "it", "its", "we", "you", "they", "them",
            "then", "when", "where", "who", "what", "which", "how", "can", "may", "must"
        };
        
        // Split text into words and extract meaningful keywords
        var words = Regex.Split(text, @"[^a-z0-9]+")
            .Where(w => w.Length >= 3) // At least 3 characters
            .Where(w => !stopWords.Contains(w))
            .Distinct()
            .ToArray();
        
        // Also extract common phrases (2-3 words)
        var phrases = new List<string>();
        var originalWords = Regex.Split(frs, @"[^a-zA-Z0-9\s]+");
        for (int i = 0; i < originalWords.Length - 1; i++)
        {
            var word1 = originalWords[i].Trim().ToLowerInvariant();
            var word2 = originalWords[i + 1].Trim().ToLowerInvariant();
            if (word1.Length >= 3 && word2.Length >= 3 && !stopWords.Contains(word1) && !stopWords.Contains(word2))
            {
                phrases.Add($"{word1} {word2}");
            }
        }
        
        // Combine words and phrases, prioritize longer matches
        var allKeywords = phrases.Concat(words).Distinct().ToArray();
        
        return allKeywords;
    }

    private async Task<string> InspectOnceAsync(string url, string? frsText)
    {
        var playwright = await Playwright.CreateAsync();
        var headless = _configuration.GetValue<bool>("Playwright:Headless", true);
        _logger.LogInformation("[Inspector] Launching Chromium. Headless={Headless}", headless);
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1500);

        var keywords = ExtractKeywords(frsText);
        if (keywords.Length > 0)
        {
            _logger.LogInformation("[Inspector] Extracted FRS keywords: {Keywords}", string.Join(", ", keywords));
            for (int i = 0; i < 3; i++)
            {
                await page.Mouse.WheelAsync(0, 800);
                await page.WaitForTimeoutAsync(300);
            }

            foreach (var kw in keywords)
            {
                try
                {
                    var roleCandidates = new[] { AriaRole.Button, AriaRole.Link };
                    foreach (var role in roleCandidates)
                    {
                        var locator = page.GetByRole(role, new() { Name = kw, Exact = false });
                        var count = await locator.CountAsync();
                        if (count > 0)
                        {
                            _logger.LogInformation("[Inspector] Found role match. Role={Role}, Name~={Name}, Count={Count}", role, kw, count);
                            await locator.First.Or(locator.Nth(0)).ScrollIntoViewIfNeededAsync();
                        }
                    }

                    var input = page.Locator($"input[placeholder*='{kw}'], input[name*='{kw}']");
                    var icount = await input.CountAsync();
                    if (icount > 0)
                    {
                        _logger.LogInformation("[Inspector] Found input match. Keyword={Keyword}, Count={Count}", kw, icount);
                        await input.First.ScrollIntoViewIfNeededAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[Inspector] Keyword handling error for '{Keyword}'", kw);
                }
            }
        }
        else
        {
            _logger.LogInformation("[Inspector] No FRS keywords detected. Performing general scan.");
        }

        var js = """
() => {
    const elements = [];
    const preferSelectors = (el) => {
        const sels = [];
        const val = (name) => el.getAttribute(name);
        const tag = el.tagName.toLowerCase();
        const esc = (s) => s && s.replace(/(["\\\[\]\(\)#\.\+\*\^\$\?\|\{\}])/g, '\\$1');
        const dtid = val('data-testid') || val('data-test') || val('data-qa');
        if (dtid) sels.push(`[data-testid="${esc(dtid)}"]`, `[data-test="${esc(dtid)}"]`, `[data-qa="${esc(dtid)}"]`);
        if (el.id) sels.push(`#${esc(el.id)}`);
        if ((tag === 'input' || tag === 'textarea' || tag === 'select') && el.name) {
            sels.push(`${tag}[name="${esc(el.name)}"]`);
        }
        if ((tag === 'input' || tag === 'textarea') && el.placeholder) {
            sels.push(`${tag}[placeholder="${esc(el.placeholder)}"]`);
        }
        const aria = val('aria-label');
        if (aria) sels.push(`[aria-label="${esc(aria)}"]`);
        const visibleText = (el.innerText || el.textContent || '').replace(/\s+/g, ' ').trim();
        if ((tag === 'a' || tag === 'button') && visibleText) {
            sels.push(`text=${visibleText}`);
        }
        if (sels.length === 0) sels.push(tag);
        return [...new Set(sels)];
    };
    const getRole = (el) => {
        const tag = el.tagName.toLowerCase();
        if (tag === 'button' || (tag === 'input' && (el.type === 'submit' || el.type === 'button' || el.type === 'reset'))) return 'button';
        if (tag === 'a' && el.href) return 'link';
        if (tag === 'input' && (!el.type || ['text','email','password','search','tel','url','number'].includes(el.type))) return 'textbox';
        if (tag === 'select') return 'combobox';
        if (tag === 'textarea') return 'textbox';
        return '';
    };
    const getName = (el) => {
        const preferred = (s) => s && s.replace(/\s+/g, ' ').trim();
        return preferred(el.getAttribute('aria-label'))
            || preferred(el.innerText || el.textContent)
            || preferred(el.getAttribute('title'))
            || preferred(el.getAttribute('alt'))
            || preferred(el.getAttribute('placeholder'))
            || '';
    };
    const pushIfVisible = (el, type) => {
        if (el.offsetParent === null) return;
        const selectors = preferSelectors(el);
        const role = getRole(el);
        const name = getName(el);
        elements.push({ type, selector: selectors[0], alternatives: selectors.slice(1), role, name, id: el.id || '', tag: el.tagName.toLowerCase(), href: el.href || undefined, inputType: el.type || undefined, placeholder: el.placeholder || undefined });
    };
    document.querySelectorAll('input').forEach(el => pushIfVisible(el, 'input'));
    document.querySelectorAll('button, input[type="submit"], input[type="button"]').forEach(el => pushIfVisible(el, 'button'));
    document.querySelectorAll('a').forEach(el => pushIfVisible(el, 'link'));
    document.querySelectorAll('textarea').forEach(el => pushIfVisible(el, 'textarea'));
    document.querySelectorAll('select').forEach(el => pushIfVisible(el, 'select'));
    return JSON.stringify(elements, null, 2);
}
""";
        var pageStructure = await page.EvaluateAsync<string>(js);
        return pageStructure ?? "[]";
    }

    private void LogInspectionSummary(string json, bool retried = false)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("[Inspector] Unexpected inspection output (not array). Length={Length}. Retried={Retried}", json.Length, retried);
                return;
            }
            var total = doc.RootElement.GetArrayLength();
            var byType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var type = el.TryGetProperty("type", out var t) ? t.GetString() ?? "?" : "?";
                byType[type] = byType.TryGetValue(type, out var c) ? c + 1 : 1;
            }
            var breakdown = string.Join(", ", byType.Select(kv => $"{kv.Key}:{kv.Value}"));
            _logger.LogInformation("[Inspector] Extracted elements. Total={Total}. Breakdown={Breakdown}. Retried={Retried}", total, breakdown, retried);

            // Log a sample of first few selectors for visibility
            var first = doc.RootElement.EnumerateArray().Take(5).Select(e => e.TryGetProperty("selector", out var s) ? s.GetString() : null).Where(s => !string.IsNullOrWhiteSpace(s))!;
            _logger.LogInformation("[Inspector] Sample selectors: {Selectors}", string.Join(" | ", first!));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[Inspector] Failed to summarize inspection JSON. Length={Length}", json?.Length ?? 0);
        }
    }

    private static bool IsBrowserNotInstalled(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;
        return msg.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("Browser is not installed", StringComparison.OrdinalIgnoreCase)
            || (msg.Contains("Failed to launch", StringComparison.OrdinalIgnoreCase) && msg.Contains("download", StringComparison.OrdinalIgnoreCase));
    }

    private static void TryInstallChromium()
    {
        try { _ = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" }); } catch { }
    }

    private static string BuildMissingBrowserMessage(string baseMsg)
    {
        return "Playwright browsers are not installed. Please install by running: 'dotnet tool install --global Microsoft.Playwright.CLI' and 'playwright install chromium'. Original: " + baseMsg;
    }
}
