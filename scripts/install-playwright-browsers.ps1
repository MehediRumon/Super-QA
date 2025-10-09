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

# Navigate to project root
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location (Join-Path $scriptPath "..")

# Install Playwright CLI globally if not already installed
try {
    $playwrightVersion = playwright --version 2>&1
    Write-Host "✓ Playwright CLI already installed" -ForegroundColor Green
} catch {
    Write-Host "📦 Installing Microsoft.Playwright.CLI..." -ForegroundColor Yellow
    dotnet tool install --global Microsoft.Playwright.CLI
    # Refresh PATH to include dotnet tools
    $env:PATH = "$env:PATH;$env:USERPROFILE\.dotnet\tools"
    Write-Host "✓ Playwright CLI installed" -ForegroundColor Green
}

# Install Chromium browser
Write-Host "📥 Installing Chromium browser..." -ForegroundColor Yellow
Write-Host "This may take a few minutes..." -ForegroundColor Yellow
Write-Host ""

try {
    playwright install chromium
    Write-Host ""
    Write-Host "✅ Installation complete!" -ForegroundColor Green
} catch {
    Write-Host ""
    Write-Host "⚠️ Installation encountered errors, but browsers may have been partially installed." -ForegroundColor Yellow
    Write-Host "You can verify by checking: $env:USERPROFILE\.cache\ms-playwright\" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Playwright browsers are now installed and ready for:" -ForegroundColor Cyan
Write-Host "  • AI-powered test generation with actual page elements"
Write-Host "  • Test execution and automation"
Write-Host ""
Write-Host "Note: If you see errors during generation, verify browsers with:" -ForegroundColor Yellow
Write-Host "  dir $env:USERPROFILE\.cache\ms-playwright\" -ForegroundColor Yellow
Write-Host ""
Write-Host "You can now use the Playwright Test Generator feature in Super-QA." -ForegroundColor Green
