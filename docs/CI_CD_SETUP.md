# CI/CD Configuration for Super-QA

## GitHub Actions

If you're using GitHub Actions for CI/CD, you'll need to install Playwright browsers as part of your workflow. Add this step to your `.github/workflows/*.yml` file:

### Complete Example Workflow

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    # CRITICAL: Install Playwright browsers for AI test generation
    - name: Install Playwright Browsers
      run: |
        dotnet tool install --global Microsoft.Playwright.CLI
        playwright install chromium --with-deps
    
    - name: Run Tests
      run: dotnet test --no-build --verbosity normal
```

### Minimal Playwright Setup Step

If you already have a workflow, just add this step before running tests:

```yaml
- name: Install Playwright Browsers
  run: |
    dotnet tool install --global Microsoft.Playwright.CLI
    playwright install chromium --with-deps
```

## Azure DevOps

For Azure DevOps pipelines, add this task to your `azure-pipelines.yml`:

```yaml
stages:
- stage: Build
  jobs:
  - job: BuildAndTest
    pool:
      vmImage: 'ubuntu-latest'
    
    steps:
    - task: UseDotNet@2
      displayName: 'Use .NET 9.0'
      inputs:
        version: '9.0.x'
    
    - task: DotNetCoreCLI@2
      displayName: 'Restore NuGet packages'
      inputs:
        command: 'restore'
    
    - task: DotNetCoreCLI@2
      displayName: 'Build solution'
      inputs:
        command: 'build'
        arguments: '--no-restore'
    
    # CRITICAL: Install Playwright browsers
    - task: Bash@3
      displayName: 'Install Playwright Browsers'
      inputs:
        targetType: 'inline'
        script: |
          dotnet tool install --global Microsoft.Playwright.CLI
          playwright install chromium --with-deps
    
    - task: DotNetCoreCLI@2
      displayName: 'Run Tests'
      inputs:
        command: 'test'
        arguments: '--no-build'
```

## GitLab CI

For GitLab CI, add this to your `.gitlab-ci.yml`:

```yaml
image: mcr.microsoft.com/dotnet/sdk:9.0

stages:
  - build
  - test

before_script:
  - dotnet restore

build:
  stage: build
  script:
    - dotnet build --no-restore

test:
  stage: test
  before_script:
    - dotnet tool install --global Microsoft.Playwright.CLI
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - playwright install chromium --with-deps
  script:
    - dotnet test --no-build --verbosity normal
```

## Docker

If you're running Super-QA in Docker, add these commands to your `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

# Copy project files
COPY . .

# Restore and build
RUN dotnet restore
RUN dotnet build -c Release

# Install Playwright browsers
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN playwright install chromium --with-deps

# Run tests
RUN dotnet test -c Release --no-build

# ... rest of your Dockerfile
```

## Common Issues in CI/CD

### Issue: Browser download fails
**Solution:** The CI/CD environment may have network restrictions. Try:

```bash
# Use alternative download host
export PLAYWRIGHT_DOWNLOAD_HOST=https://playwright.azureedge.net
playwright install chromium
```

### Issue: Permission errors
**Solution:** Ensure the user has permissions to install tools:

```bash
# GitHub Actions / GitLab: usually runs as root
# Azure DevOps: may need sudo
sudo playwright install chromium --with-deps
```

### Issue: Disk space
**Solution:** Chromium browser is ~160MB. Ensure CI runner has sufficient disk space.

```bash
# Check available space
df -h
```

## Caching Browsers (Recommended)

To speed up builds, cache the browser installation:

### GitHub Actions Cache

```yaml
- name: Cache Playwright Browsers
  uses: actions/cache@v3
  with:
    path: ~/.cache/ms-playwright
    key: ${{ runner.os }}-playwright-${{ hashFiles('**/package-lock.json') }}
    restore-keys: |
      ${{ runner.os }}-playwright-

- name: Install Playwright Browsers
  run: |
    dotnet tool install --global Microsoft.Playwright.CLI
    playwright install chromium --with-deps
```

### Azure DevOps Cache

```yaml
- task: Cache@2
  inputs:
    key: 'playwright | "$(Agent.OS)"'
    path: $(Pipeline.Workspace)/.playwright
  displayName: 'Cache Playwright browsers'
```

## Environment Variables

Useful environment variables for CI/CD:

```bash
# Run in headless mode (default)
export PLAYWRIGHT_HEADLESS=true

# Skip browser download if already cached
export PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1

# Custom browser download location
export PLAYWRIGHT_BROWSERS_PATH=/custom/path
```

## Verification

To verify browsers are installed correctly in CI/CD:

```bash
playwright --version
ls -la ~/.cache/ms-playwright/
```

Expected output should show `chromium_headless_shell-1148` or similar.

## Cost Optimization

If you're concerned about CI/CD costs:

1. **Cache browsers** between runs (saves ~2-3 minutes per build)
2. **Only install chromium** (not all browsers)
3. **Use matrix builds** to parallelize tests
4. **Skip browser install** for builds that don't run tests

Example of conditional installation:

```yaml
- name: Install Playwright Browsers
  if: github.event_name != 'push' || contains(github.event.head_commit.message, '[skip-tests]') == false
  run: |
    dotnet tool install --global Microsoft.Playwright.CLI
    playwright install chromium
```

## Support

For CI/CD specific issues:
- Check the [Playwright CI documentation](https://playwright.dev/docs/ci)
- Review [Super-QA Troubleshooting Guide](TROUBLESHOOTING_PLAYWRIGHT.md)
- Ensure the browser installation step runs **before** tests
