using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using SuperQA.Core.Interfaces;

namespace SuperQA.Infrastructure.Services;

public class PageInspectorService : IPageInspectorService
{
    private readonly IConfiguration _configuration;

    public PageInspectorService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> GetPageStructureAsync(string url)
    {
        try
        {
            return await InspectOnceAsync(url);
        }
        catch (Exception ex) when (IsBrowserNotInstalled(ex))
        {
            // Attempt to install browsers, then retry once
            try
            {
                TryInstallChromium();
                return await InspectOnceAsync(url);
            }
            catch (Exception retryEx)
            {
                var errorMessage = BuildMissingBrowserMessage(retryEx.Message);
                return JsonSerializer.Serialize(new[] { new { error = $"Failed to inspect page: {errorMessage}" } });
            }
        }
        catch (Exception ex)
        {
            // Provide helpful error message if browsers aren't installed
            var errorMessage = ex.Message;
            if (IsBrowserNotInstalled(ex))
            {
                errorMessage = BuildMissingBrowserMessage(errorMessage);
            }
            return JsonSerializer.Serialize(new[] { new { error = $"Failed to inspect page: {errorMessage}" } });
        }
    }

    private async Task<string> InspectOnceAsync(string url)
    {
        var playwright = await Playwright.CreateAsync();
        var headless = _configuration.GetValue<bool>("Playwright:Headless", true);
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless
        });
        var page = await browser.NewPageAsync();

        // Navigate to the URL
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait a bit for dynamic content
        await page.WaitForTimeoutAsync(2000);

        // Extract page structure
        var pageStructure = await page.EvaluateAsync<string>(@"() => {
                const elements = [];
                function getSelector(el) {
                    if (el.id) return '#' + el.id;
                    if (el.name) return el.tagName.toLowerCase() + '[name=""' + el.name + '""]';
                    if (el.className && typeof el.className === 'string' && el.className.trim()) {
                        const classes = el.className.trim().split(/\s+/);
                        if (classes.length > 0) return el.tagName.toLowerCase() + '.' + classes[0];
                    }
                    if (el.type) return el.tagName.toLowerCase() + '[type=""' + el.type + '""]';
                    return el.tagName.toLowerCase();
                }
                function getVisibleText(el) {
                    const text = el.innerText || el.textContent || '';
                    return text.replace(/\s+/g, ' ').trim().substring(0, 50);
                }
                function getPlaceholder(el) { return el.placeholder || ''; }
                function getAriaLabel(el) { return el.getAttribute('aria-label') || ''; }
                document.querySelectorAll('input').forEach(el => {
                    if (el.offsetParent !== null) {
                        elements.push({ type: 'input', selector: getSelector(el), inputType: el.type || 'text', name: el.name || '', id: el.id || '', placeholder: getPlaceholder(el), ariaLabel: getAriaLabel(el), value: el.value || '' });
                    }
                });
                document.querySelectorAll('button, input[type=""submit""], input[type=""button""]').forEach(el => {
                    if (el.offsetParent !== null) {
                        elements.push({ type: 'button', selector: getSelector(el), text: getVisibleText(el), id: el.id || '', name: el.name || '', ariaLabel: getAriaLabel(el) });
                    }
                });
                document.querySelectorAll('a').forEach(el => {
                    if (el.offsetParent !== null && el.href) {
                        elements.push({ type: 'link', selector: getSelector(el), text: getVisibleText(el), href: el.href, id: el.id || '' });
                    }
                });
                document.querySelectorAll('textarea').forEach(el => {
                    if (el.offsetParent !== null) {
                        elements.push({ type: 'textarea', selector: getSelector(el), name: el.name || '', id: el.id || '', placeholder: getPlaceholder(el) });
                    }
                });
                document.querySelectorAll('select').forEach(el => {
                    if (el.offsetParent !== null) {
                        elements.push({ type: 'select', selector: getSelector(el), name: el.name || '', id: el.id || '' });
                    }
                });
                return JSON.stringify(elements, null, 2);
            }");
        await browser.CloseAsync();
        return pageStructure ?? "[]";
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
        try
        {
            // Install Chromium for the current Playwright version used by the app
            _ = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        }
        catch
        {
            // Swallow exceptions; we'll report failure on retry if it still can't launch
        }
    }

    private static string BuildMissingBrowserMessage(string baseMsg)
    {
        return "Playwright browsers are not installed. Please install by running: 'dotnet tool install --global Microsoft.Playwright.CLI' and 'playwright install chromium'. Original: " + baseMsg;
    }
}
