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
| **Database** | SQL Server | Stores projects, test data, logs, embeddings |
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

### Phase 3: AI Analyzer (🔄 Planned)

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
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or SQL Server LocalDB
- [PowerShell](https://docs.microsoft.com/en-us/powershell/) (for Playwright browser installation)
- [Node.js](https://nodejs.org/) (for MCP server - optional)
- Modern web browser

## 🛠️ Setup

### 1. Clone the Repository

```bash
git clone https://github.com/MehediRumon/Super-QA.git
cd Super-QA
```

### 2. Configure Database

Update the connection string in `src/SuperQA.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SuperQA;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Apply Database Migrations

```bash
cd src/SuperQA.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../SuperQA.Api
dotnet ef database update --startup-project ../SuperQA.Api
```

### 4. Run the Application

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

### 5. Install Playwright Browsers (For Phase 2 Test Execution)

```bash
# After building the Infrastructure project
cd src/SuperQA.Infrastructure/bin/Debug/net9.0
pwsh playwright.ps1 install chromium
```

Or install globally:
```bash
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

**📖 See [PHASE2_QUICKSTART.md](PHASE2_QUICKSTART.md) for detailed test automation guide**

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

## 🧪 Running Tests

```bash
dotnet test
```

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
