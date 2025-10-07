# Quick Start Guide

Get Super-QA up and running in 5 minutes!

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) installed
- SQL Server or SQL Server LocalDB (comes with Visual Studio)
- Any modern web browser

## Quick Setup

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

Open two terminal windows:

**Terminal 1 - API Backend:**

```bash
cd src/SuperQA.Api
dotnet run
```

You should see:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
```

**Terminal 2 - Blazor Frontend:**

```bash
cd src/SuperQA.Client
dotnet run
```

You should see:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```

### 4. Open the Application

Open your browser and navigate to:

```
https://localhost:5001
```

## Using Super-QA

### Create Your First Project

1. Click on **"Projects"** in the navigation menu
2. Click **"+ Create New Project"**
3. Enter:
   - **Name**: "My First QA Project"
   - **Description**: "Testing the Super-QA system"
4. Click **"Create"**

### Add a Requirement

1. Click **"View"** on your newly created project
2. On the Requirements tab, click **"+ Add Requirement"**
3. Enter:
   - **Title**: "User Login Feature"
   - **Description**: "Users should be able to log in with email and password"
   - **Type**: Select "User Story"
4. Click **"Add"**

### Generate AI Test Cases

1. Click **"🤖 Generate Test Cases"** next to your requirement
2. Switch to the **"Test Cases"** tab
3. View your AI-generated test case!

**Note**: The AI generation currently returns a sample test case. To connect to a real AI service:
- Update `src/SuperQA.Api/appsettings.json` with your MCP endpoint
- See [MCP Integration Guide](docs/MCP_INTEGRATION.md) for details

## Troubleshooting

### Port Already in Use

If ports 7001 or 5001 are already in use, you can change them in:
- **API**: `src/SuperQA.Api/Properties/launchSettings.json`
- **Client**: `src/SuperQA.Client/Properties/launchSettings.json`

Don't forget to update the API URL in `src/SuperQA.Client/wwwroot/appsettings.json` if you change the API port.

### Database Connection Failed

If you see database errors, update the connection string in `src/SuperQA.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SuperQA;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### CORS Errors

If you see CORS errors in the browser console, make sure:
1. The API is running on the expected port (7001)
2. The CORS policy in `src/SuperQA.Api/Program.cs` includes your client URL

## What's Next?

- **Configure AI Integration**: Set up MCP to connect to real AI models
- **Add More Requirements**: Create requirements for different features
- **Generate More Tests**: Use AI to generate comprehensive test suites
- **Explore the API**: Check out the OpenAPI documentation at `https://localhost:7001/openapi/v1.json`

## Development Workflow

### Running Tests

```bash
dotnet test
```

### Database Migrations (Future)

When you need to update the database schema:

```bash
cd src/SuperQA.Infrastructure
dotnet ef migrations add YourMigrationName --startup-project ../SuperQA.Api
dotnet ef database update --startup-project ../SuperQA.Api
```

### Building for Production

```bash
dotnet build --configuration Release
dotnet publish --configuration Release --output ./publish
```

## Architecture at a Glance

```
Super-QA
├── API (ASP.NET Core)        ← Business logic & data access
├── Client (Blazor WASM)      ← User interface
├── Core (Domain)             ← Entities & interfaces  
├── Infrastructure (Data)     ← EF Core & services
└── Shared (DTOs)            ← Data transfer objects
```

## Need Help?

- 📖 [Full README](README.md)
- 🏗️ [Architecture Guide](docs/ARCHITECTURE.md)
- 🤖 [MCP Integration Guide](docs/MCP_INTEGRATION.md)
- 🐛 [Report Issues](https://github.com/MehediRumon/Super-QA/issues)

## Default Credentials

Currently, there is no authentication. This feature is planned for Phase 2.

## Sample Data

The system starts with an empty database. Use the UI to create your first project and requirements.

## Performance Tips

- Use Chrome or Edge for best Blazor performance
- Enable HTTP/2 in production
- Consider using SQL Server Express or full SQL Server for production

## Next Features (Coming Soon)

- ✨ Real AI integration with OpenAI/Claude
- 🔐 User authentication
- 🤖 Selenium/Playwright test execution
- 📊 Test analytics dashboard
- 🔧 Self-healing test automation

Happy Testing! 🚀
