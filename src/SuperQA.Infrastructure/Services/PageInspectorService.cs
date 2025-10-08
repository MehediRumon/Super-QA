using System.Text;
using Microsoft.Playwright;
using SuperQA.Core.Interfaces;

namespace SuperQA.Infrastructure.Services;

public class PageInspectorService : IPageInspectorService
{
    public async Task<string> GetPageStructureAsync(string url)
    {
        try
        {
            var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            
            var page = await browser.NewPageAsync();
            
            // Navigate to the URL
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            
            // Wait a bit for dynamic content
            await page.WaitForTimeoutAsync(2000);
            
            // Extract page structure
            var pageStructure = await page.EvaluateAsync<string>(@"() => {
                const elements = [];
                
                // Helper function to get a unique selector for an element
                function getSelector(el) {
                    if (el.id) {
                        return '#' + el.id;
                    }
                    
                    if (el.name) {
                        return el.tagName.toLowerCase() + '[name=""' + el.name + '""]';
                    }
                    
                    if (el.className && typeof el.className === 'string' && el.className.trim()) {
                        const classes = el.className.trim().split(/\s+/);
                        if (classes.length > 0) {
                            return el.tagName.toLowerCase() + '.' + classes[0];
                        }
                    }
                    
                    if (el.type) {
                        return el.tagName.toLowerCase() + '[type=""' + el.type + '""]';
                    }
                    
                    return el.tagName.toLowerCase();
                }
                
                // Helper function to get visible text
                function getVisibleText(el) {
                    const text = el.innerText || el.textContent || '';
                    // Replace newlines with spaces and limit to 50 chars
                    return text.replace(/\s+/g, ' ').trim().substring(0, 50);
                }
                
                // Helper function to get placeholder
                function getPlaceholder(el) {
                    return el.placeholder || '';
                }
                
                // Helper function to get aria-label
                function getAriaLabel(el) {
                    return el.getAttribute('aria-label') || '';
                }
                
                // Collect inputs
                document.querySelectorAll('input').forEach(el => {
                    if (el.offsetParent !== null) { // Check if visible
                        elements.push({
                            type: 'input',
                            selector: getSelector(el),
                            inputType: el.type || 'text',
                            name: el.name || '',
                            id: el.id || '',
                            placeholder: getPlaceholder(el),
                            ariaLabel: getAriaLabel(el),
                            value: el.value || ''
                        });
                    }
                });
                
                // Collect buttons
                document.querySelectorAll('button, input[type=""submit""], input[type=""button""]').forEach(el => {
                    if (el.offsetParent !== null) {
                        elements.push({
                            type: 'button',
                            selector: getSelector(el),
                            text: getVisibleText(el),
                            id: el.id || '',
                            name: el.name || '',
                            ariaLabel: getAriaLabel(el)
                        });
                    }
                });
                
                // Collect links
                document.querySelectorAll('a').forEach(el => {
                    if (el.offsetParent !== null && el.href) {
                        elements.push({
                            type: 'link',
                            selector: getSelector(el),
                            text: getVisibleText(el),
                            href: el.href,
                            id: el.id || ''
                        });
                    }
                });
                
                // Collect textareas
                document.querySelectorAll('textarea').forEach(el => {
                    if (el.offsetParent !== null) {
                        elements.push({
                            type: 'textarea',
                            selector: getSelector(el),
                            name: el.name || '',
                            id: el.id || '',
                            placeholder: getPlaceholder(el)
                        });
                    }
                });
                
                // Collect select dropdowns
                document.querySelectorAll('select').forEach(el => {
                    if (el.offsetParent !== null) {
                        elements.push({
                            type: 'select',
                            selector: getSelector(el),
                            name: el.name || '',
                            id: el.id || ''
                        });
                    }
                });
                
                return JSON.stringify(elements, null, 2);
            }");
            
            await browser.CloseAsync();
            
            return pageStructure ?? "[]";
        }
        catch (Exception ex)
        {
            // Return empty structure if inspection fails
            return $"[{{\"error\": \"Failed to inspect page: {ex.Message}\"}}]";
        }
    }
}
