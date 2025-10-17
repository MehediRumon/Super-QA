# Super-QA: AI-Powered Software Quality Assurance System

An intelligent test automation and quality assurance platform leveraging AI/ML and the Model Context Protocol (MCP) for automated test generation, self-healing automation, and defect prediction.

## 🎯 Overview

Super-QA is a comprehensive testing platform that combines AI-powered test generation with modern web technologies to revolutionize software quality assurance processes.

## 🏗️ Architecture

### Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Frontend (UI)** | Blazor WebAssembly | Interactive web app, runs client-side with C# |
| **Backend (API)** | ASP.NET Core Web API | Handles authentication, test data, logs, requirements upload |
| **AI / ML Layer** | MCP-based AI Services | AI-driven test generation, analysis, and self-healing |
| **Database** | SQL Server / In-Memory | Stores projects, test data, logs, embeddings |
| **Automation** | Playwright | Executes and monitors web tests |
| **Data Science** | Python ML/ML.NET | Defect prediction, risk scoring, analytics (Coming Soon) |

### Project Structure

```
SuperQA/
├── src/
│   ├── SuperQA.Api/              # ASP.NET Core Web API
│   │   ├── Controllers/          # API endpoints
│   │   └── Program.cs           # API configuration
│   ├── SuperQA.Client/           # Blazor WebAssembly frontend
│   │   ├── Pages/               # Blazor pages
│   │   ├── Services/            # HTTP client services
│   │   └── Layout/              # Layout components
│   ├── SuperQA.Core/             # Domain entities and interfaces
│   │   ├── Entities/            # Domain models
│   │   └── Interfaces/          # Service interfaces
│   ├── SuperQA.Infrastructure/   # Data access and implementations
│   │   ├── Data/                # EF Core DbContext
│   │   └── Services/            # Service implementations
│   └── SuperQA.Shared/           # Shared DTOs
│       └── DTOs/                # Data transfer objects
└── tests/
    └── SuperQA.Tests/            # Unit tests
```

## 🚀 Features

### Phase 1: MVP (✅ Implemented)

- ✅ **Project Management**: Create and manage QA projects
- ✅ **Requirements Tracking**: Upload and organize requirements/user stories
- ✅ **AI Test Case Generator**: Generate test cases from requirements using MCP
- ✅ **Dashboard**: View projects, requirements, and test cases
- ✅ **RESTful API**: Full CRUD operations for projects, requirements, and test cases

### Phase 2: Automation (✅ Implemented)

- ✅ **Playwright integration**: Browser automation with Playwright .NET
- ✅ **Test execution engine**: Execute test cases automatically
- ✅ **Background worker for automated testing**: Run tests in background
- ✅ **Test result visualization**: View test execution results with details
- ✅ **Screenshot capture on failure**: Automatically capture screenshots when tests fail
- ✅ **OpenAI-Powered Playwright Test Generator**: Generate executable C# Playwright test scripts from FRS
- ✅ **Automated test script generation**: AI generates locators, actions, and assertions
- ✅ **Test script execution**: Execute generated Playwright scripts and view results
- ✅ **Test Case Automation Script Generation**: Generate Playwright automation scripts from test cases with actual page element inspection
- ✅ **Saved Test Scripts Management UI**: View, edit, execute, and delete AI-generated test scripts through a modern UI
- ✅ **AI Test Healing**: Intelligent self-healing for failed tests with AI-powered script repair
- ✅ **Self-Healing Locators**: Automatic locator healing when elements are not found during test execution

### Phase 3: AI Analyzer (🔄 In Progress)

- ✅ **AI Test Healing**: Automatically fix failed tests with intelligent script repair
- ✅ **Self-Healing Locators**: Automatically detect and fix broken element locators during test execution
- 🔄 Log analyzer with AI
- 🔄 Automated bug report generation
- 🔄 Root cause analysis

### Phase 4: ML Layer (🔄 Planned)

- 🔄 Risk prediction models
- 🔄 Self-healing automation
- 🔄 Defect prediction

### Phase 5: Chatbot (🔄 Planned)

- 🔄 Conversational QA Assistant
- 🔄 Vector embeddings support
- 🔄 Natural language test queries

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- Modern web browser
- *(Optional)* [OpenAI API Key](https://platform.openai.com/api-keys) (for Playwright test generation)
- *(Optional)* [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or SQL Server LocalDB for production
- *(Optional)* [PowerShell](https://docs.microsoft.com/en-us/powershell/) (for Playwright browser installation)
- *(Optional)* [Node.js](https://nodejs.org/) (for MCP server)

**Note**: The application uses an in-memory database in development mode by default, so SQL Server is not required for local development!

## 🛠️ Setup

### 1. Clone the Repository

```bash
git clone https://github.com/MehediRumon/Super-QA.git
cd Super-QA
```

### 2. Build the Solution

```bash
dotnet build
```

### 3. Run the Application

**The application uses an in-memory database by default in development mode**, so you can skip the database setup steps and start right away!

#### Backend API

```bash
cd src/SuperQA.Api
dotnet run
```

The API will be available at `https://localhost:7001`

#### Frontend (Blazor)

```bash
cd src/SuperQA.Client
dotnet run
```

The client will be available at `https://localhost:5001`

### 4. Install Playwright Browsers (Required for AI Test Generation & Execution)

**⚠️ CRITICAL REQUIREMENT**: Playwright browsers **MUST be installed** for:
- ✅ AI-powered test script generation with **actual page elements** (recommended)
- ✅ Test execution and automation
- ❌ Without browsers: AI will generate generic selectors (not recommended)

**Quick Setup (Recommended)**:

```bash
# Linux/macOS
./scripts/install-playwright-browsers.sh

# Windows
.\scripts\install-playwright-browsers.ps1
```

**Manual Installation**:

```bash
# 1. Install Playwright CLI globally
dotnet tool install --global Microsoft.Playwright.CLI

# 2. Add to PATH (Linux/macOS)
export PATH="$PATH:$HOME/.dotnet/tools"

# 2. Add to PATH (Windows PowerShell)
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# 3. Install Chromium browser
playwright install chromium
```

**Verify Installation**:
```bash
# Check CLI version
playwright --version

# Verify browsers installed (Linux/macOS)
ls ~/.cache/ms-playwright/

# Verify browsers installed (Windows)
dir $env:USERPROFILE\.cache\ms-playwright\
```

**🚨 Troubleshooting**:

If you encounter errors during installation:
1. The installation may complete despite showing JavaScript errors
2. Verify browsers were installed using the commands above
3. See [Troubleshooting Guide](docs/TROUBLESHOOTING_PLAYWRIGHT.md) for detailed help

**Without Browser Installation**:
- ⚠️ The AI test generator will work but use **generic selectors**
- ⚠️ Generated tests will likely **require manual adjustment**
- ⚠️ You'll see warnings in the API response

**📖 Documentation**:
- [Quick Reference: Browser Installation](docs/QUICK_REFERENCE_BROWSERS.md) - Quick setup guide
- [Troubleshooting Guide](docs/TROUBLESHOOTING_PLAYWRIGHT.md) - Common issues and solutions
- [Test Case Automation](docs/TEST_CASE_AUTOMATION.md) - Generate automation scripts from test cases
- [CI/CD Setup](docs/CI_CD_SETUP.md) - GitHub Actions, Azure DevOps, GitLab CI
- [PHASE2_QUICKSTART.md](PHASE2_QUICKSTART.md) - Complete test automation guide

### 5. (Optional) Configure SQL Server Database

If you want to use SQL Server instead of the in-memory database:

1. Set `UseInMemoryDatabase` to `false` in `src/SuperQA.Api/appsettings.Development.json`
2. Update the connection string in `src/SuperQA.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SuperQA;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

3. Apply database migrations:

```bash
cd src/SuperQA.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../SuperQA.Api
dotnet ef database update --startup-project ../SuperQA.Api
```

### 6. (Optional) Configure MCP Integration

Update `src/SuperQA.Api/appsettings.json`:

```json
{
  "MCP": {
    "Endpoint": "http://localhost:3000",
    "Model": "gpt-4"
  }
}
```

### 7. (Optional) Configure Playwright Browser Visibility

By default, Playwright runs in **non-headless mode in development** (browser visible) so you can watch tests execute. In production, it runs in **headless mode** (browser invisible) for better performance.

To change the headless setting:

**Development** (`src/SuperQA.Api/appsettings.Development.json`):
```json
{
  "Playwright": {
    "Headless": false  // false = browser visible, true = browser invisible
  }
}
```

**Production** (`src/SuperQA.Api/appsettings.json`):
```json
{
  "Playwright": {
    "Headless": true  // Recommended for production
  }
}
```

## 🧪 Running Tests

```bash
dotnet test
```

## 🎭 Using the Playwright Test Generator

The Playwright Test Generator allows you to generate executable C# test scripts using OpenAI's GPT models:

1. **Navigate to Playwright Generator**: Click "Playwright Generator" in the navigation menu
2. **Enter OpenAI API Key**: Provide your OpenAI API key (get one from [OpenAI Platform](https://platform.openai.com/api-keys))
3. **Select AI Model**: Choose GPT-4 (recommended), GPT-4 Turbo, or GPT-3.5 Turbo
4. **Enter Application URL**: Specify the URL of the application you want to test
5. **Write FRS**: Describe the functional requirements and expected behavior
6. **Generate**: Click "Generate Test Script" to create a Playwright C# test
7. **Execute** (Optional): Click "Execute Test" to run the generated script and view results

**📖 See [docs/PLAYWRIGHT_GENERATOR.md](docs/PLAYWRIGHT_GENERATOR.md) for detailed guide and examples**

## 📝 Managing Saved Test Scripts

The Saved Test Scripts feature provides a comprehensive UI for managing your AI-generated test scripts:

### Features
- **View All Scripts**: Browse all saved test scripts in a clean, organized list
- **Edit Scripts**: Modify test script title, description, and automation code in-place
- **Execute Tests**: Run Playwright tests directly from the UI and view detailed results
- **Delete Scripts**: Remove unwanted scripts with confirmation
- **Copy Code**: Quickly copy test script code to clipboard

### How to Use
1. **Navigate to Saved Test Scripts**: Click "Saved Test Scripts" in the navigation menu
2. **Select a Script**: Click on any test script from the list to view details
3. **Edit** (Optional): Click "Edit" to modify the script, then "Save" to persist changes
4. **Execute**: Click "Execute Test" to run the script and view results
5. **Delete** (Optional): Click "Delete" and confirm to remove the script

**📖 See [SAVED_TEST_SCRIPTS_FEATURE.md](SAVED_TEST_SCRIPTS_FEATURE.md) for detailed documentation**

## 🔧 AI Test Healing

When a test fails, you can use the AI Test Healing feature to automatically analyze and fix the issue:

1. **View Failed Test**: Navigate to Test Executions and identify failed tests
2. **Click AI Heal**: Click the "AI Heal" button next to any failed test
3. **Provide API Key**: Enter your OpenAI API key
4. **Review Healed Script**: The AI will analyze the failure and generate an improved, more resilient test script
5. **Apply Fix**: Click "Apply Healed Script" to automatically update the test case, or copy the script manually

The AI analyzes error messages, stack traces, and screenshots to suggest fixes for:
- Selector issues (element not found, changed selectors)
- Timing problems (elements not ready, async operations)
- Navigation issues (page not loaded, redirects)
- Data validation failures

**New in v2.0**: The healed script can now be automatically applied to your test case with a single click, eliminating manual copy/paste!

**📖 See [AI_TEST_HEALING_GUIDE.md](AI_TEST_HEALING_GUIDE.md) for detailed guide and examples**

## 🔧 Self-Healing Locators

When tests fail due to changed element locators, the self-healing system automatically:

1. **Detects Failure**: Identifies when a test fails because an element cannot be found
2. **Analyzes Page**: Examines the HTML structure to find alternative selectors
3. **Suggests Alternative**: Proposes a more stable locator (prefers id > data-testid > class)
4. **Retries Test**: Attempts the action with the new locator
5. **Updates Test Case**: Saves the healed locator for future test runs

The self-healing system works automatically during test execution and requires no configuration.

**Example:** If `#loginButton` fails, the system might find `[data-testid='loginButton']` and automatically update the test.

**📖 See [SELF_HEALING_LOCATORS_GUIDE.md](SELF_HEALING_LOCATORS_GUIDE.md) for detailed guide and examples**

## 📊 Database Schema

### Entities

- **Project**: Test projects
- **Requirement**: User stories and features
- **TestCase**: Test cases (manual or AI-generated)
- **TestExecution**: Test run results
- **DefectPrediction**: ML-based defect predictions
- **AIPromptLog**: AI interaction logs

## 🔌 API Endpoints

### Projects

- `GET /api/projects` - Get all projects
- `GET /api/projects/{id}` - Get project by ID
- `POST /api/projects` - Create new project
- `DELETE /api/projects/{id}` - Delete project

### Requirements

- `GET /api/requirements/project/{projectId}` - Get requirements for a project
- `POST /api/requirements` - Create new requirement

### Test Cases

- `GET /api/testcases/project/{projectId}` - Get test cases for a project
- `POST /api/testcases/generate` - Generate test cases using AI

### Test Executions

- `POST /api/testexecutions/execute` - Execute a single test case
- `GET /api/testexecutions/project/{projectId}` - Get all test executions for a project
- `GET /api/testexecutions/{executionId}` - Get details of a specific test execution
- `POST /api/testexecutions/project/{projectId}/run-all` - Run all tests for a project in background
- `GET /api/testexecutions/project/{projectId}/status` - Get test run status for a project

### Playwright Test Generator

- `POST /api/playwright/generate` - Generate Playwright C# test script from FRS using OpenAI
- `POST /api/playwright/execute` - Execute a Playwright test script and return results

## 🔐 Security (Coming Soon)

- JWT authentication
- Role-based authorization
- API rate limiting

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License.

## 🌟 MCP Integration

Super-QA uses the Model Context Protocol (MCP) to communicate with AI models for:

- **Test Generation**: Convert requirements into comprehensive test cases
- **Self-Healing**: Automatically fix broken test selectors
- **Analysis**: Analyze test failures and suggest fixes
- **Prediction**: Predict high-risk modules based on historical data

### MCP Flow

```
Blazor UI → API → MCP Service → AI Model (GPT/Claude/Local)
                     ↓
              Database (Logs)
```

## 🚧 Roadmap

- [x] MVP with AI test generation
- [x] Playwright integration
- [x] Real-time test execution
- [x] OpenAI-powered Playwright test script generation
- [x] AI Test Healing for failed tests
- [ ] ML-based defect prediction
- [ ] Self-healing automation
- [ ] Conversational AI assistant
- [ ] Docker containerization
- [ ] CI/CD pipeline integration

## 📧 Contact

For questions or support, please open an issue on GitHub.

## 🙏 Acknowledgments

- Built with [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- Powered by [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
- AI integration via [Model Context Protocol (MCP)](https://modelcontextprotocol.io)
