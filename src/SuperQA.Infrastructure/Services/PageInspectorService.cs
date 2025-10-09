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

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(2000);

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

        // For links/buttons, include a text selector
        const visibleText = (el.innerText || el.textContent || '').replace(/\s+/g, ' ').trim();
        if ((tag === 'a' || tag === 'button') && visibleText) {
            sels.push(`text=${visibleText}`);
        }

        // Fallback to tag if nothing else
        if (sels.length === 0) sels.push(tag);
        // Deduplicate, keep order
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
        if (el.offsetParent === null) return; // visible check
        const selectors = preferSelectors(el);
        const role = getRole(el);
        const name = getName(el);
        elements.push({
            type,
            selector: selectors[0],
            alternatives: selectors.slice(1),
            role,
            name,
            id: el.id || '',
            tag: el.tagName.toLowerCase(),
            href: el.href || undefined,
            inputType: el.type || undefined,
            placeholder: el.placeholder || undefined
        });
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
            _ = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        }
        catch
        {
        }
    }

    private static string BuildMissingBrowserMessage(string baseMsg)
    {
        return "Playwright browsers are not installed. Please install by running: 'dotnet tool install --global Microsoft.Playwright.CLI' and 'playwright install chromium'. Original: " + baseMsg;
    }
}
