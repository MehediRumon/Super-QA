#!/bin/bash
# Script to install Playwright browsers for Super-QA
# This is required for AI test generation to extract actual page elements

set -e

echo "=========================================="
echo "Super-QA Playwright Browser Installation"
echo "=========================================="
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed"
    echo "Please install .NET 9.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✓ .NET SDK found"

# Install Playwright CLI globally if not already installed
if ! command -v playwright &> /dev/null; then
    echo "📦 Installing Microsoft.Playwright.CLI..."
    dotnet tool install --global Microsoft.Playwright.CLI
    echo "✓ Playwright CLI installed"
else
    echo "✓ Playwright CLI already installed"
fi

# Install Chromium browser
echo "📥 Installing Chromium browser..."
echo "This may take a few minutes..."
playwright install chromium --with-deps

echo ""
echo "✅ Installation complete!"
echo ""
echo "Playwright browsers are now installed and ready for:"
echo "  • AI-powered test generation with actual page elements"
echo "  • Test execution and automation"
echo ""
echo "You can now use the Playwright Test Generator feature in Super-QA."
