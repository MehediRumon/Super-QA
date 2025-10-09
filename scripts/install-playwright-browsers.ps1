# PowerShell script to install Playwright browsers for Super-QA
# This is required for AI test generation to extract actual page elements

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Super-QA Playwright Browser Installation" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK found (version $dotnetVersion)" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: .NET SDK is not installed" -ForegroundColor Red
    Write-Host "Please install .NET 9.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
}

# Install Playwright CLI globally if not already installed
try {
    $playwrightVersion = playwright --version
    Write-Host "✓ Playwright CLI already installed" -ForegroundColor Green
} catch {
    Write-Host "📦 Installing Microsoft.Playwright.CLI..." -ForegroundColor Yellow
    dotnet tool install --global Microsoft.Playwright.CLI
    Write-Host "✓ Playwright CLI installed" -ForegroundColor Green
}

# Install Chromium browser
Write-Host "📥 Installing Chromium browser..." -ForegroundColor Yellow
Write-Host "This may take a few minutes..." -ForegroundColor Yellow
playwright install chromium --with-deps

Write-Host ""
Write-Host "✅ Installation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Playwright browsers are now installed and ready for:" -ForegroundColor Cyan
Write-Host "  • AI-powered test generation with actual page elements"
Write-Host "  • Test execution and automation"
Write-Host ""
Write-Host "You can now use the Playwright Test Generator feature in Super-QA." -ForegroundColor Green
