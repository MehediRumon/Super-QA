# Super-QA System Architecture

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Client Layer                               │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │              Blazor WebAssembly (Frontend)                    │  │
│  │  ┌──────────┐  ┌──────────┐  ┌───────────┐  ┌────────────┐ │  │
│  │  │Dashboard │  │ Projects │  │Test Cases │  │Requirements│ │  │
│  │  └──────────┘  └──────────┘  └───────────┘  └────────────┘ │  │
│  │  ┌──────────────────────────────────────────────────────────┐ │  │
│  │  │           HTTP Client Services (REST API)                │ │  │
│  │  └──────────────────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────┘  │
└────────────────────────────┬────────────────────────────────────────┘
                             │ HTTPS/JSON
┌────────────────────────────▼────────────────────────────────────────┐
│                        Application Layer                             │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │              ASP.NET Core Web API                             │  │
│  │  ┌────────────┐  ┌──────────────┐  ┌──────────────────────┐ │  │
│  │  │ Projects   │  │Requirements  │  │  TestCases           │ │  │
│  │  │ Controller │  │  Controller  │  │  Controller          │ │  │
│  │  └────────────┘  └──────────────┘  └──────────────────────┘ │  │
│  │  ┌─────────────────────────────────────────────────────────┐ │  │
│  │  │        Middleware (CORS, Auth, Logging)                 │ │  │
│  │  └─────────────────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────┘  │
└────────────────────────────┬────────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────────┐
│                         Business Layer                               │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                   Core Services                               │  │
│  │  ┌──────────────────┐  ┌────────────────┐  ┌──────────────┐ │  │
│  │  │AI Test Generator │  │  Self-Healing  │  │   MCP        │ │  │
│  │  │   Service        │  │    Service     │  │   Service    │ │  │
│  │  └──────────────────┘  └────────────────┘  └──────────────┘ │  │
│  └──────────────────────────────────────────────────────────────┘  │
└────────────────────────────┬────────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────────┐
│                      Infrastructure Layer                            │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                  Data Access (EF Core)                        │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────┐│  │
│  │  │SuperQADbContext│ │ Repositories │  │  Database Migrations││  │
│  │  └──────────────┘  └──────────────┘  └─────────────────────┘│  │
│  └──────────────────────────────────────────────────────────────┘  │
└────────────────────────────┬────────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────────┐
│                          Data Layer                                  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                    SQL Server Database                        │  │
│  │  ┌──────────┐  ┌─────────────┐  ┌──────────┐  ┌───────────┐│  │
│  │  │ Projects │  │Requirements │  │TestCases │  │Executions ││  │
│  │  └──────────┘  └─────────────┘  └──────────┘  └───────────┘│  │
│  │  ┌──────────────┐  ┌──────────────────────────────────────┐ │  │
│  │  │AIPromptLogs  │  │    DefectPredictions                 │ │  │
│  │  └──────────────┘  └──────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                      External Services                               │
│  ┌────────────────┐  ┌────────────────┐  ┌───────────────────────┐│
│  │  AI Models     │  │  Selenium/     │  │  Vector Database      ││
│  │  (MCP/OpenAI)  │  │  Playwright    │  │  (Future)             ││
│  └────────────────┘  └────────────────┘  └───────────────────────┘│
└─────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
SuperQA/
├── src/
│   ├── SuperQA.Api/                    # Web API Layer
│   │   ├── Controllers/                # REST API Controllers
│   │   │   ├── ProjectsController.cs
│   │   │   ├── RequirementsController.cs
│   │   │   └── TestCasesController.cs
│   │   ├── Program.cs                  # API Startup & Configuration
│   │   └── appsettings.json            # Configuration
│   │
│   ├── SuperQA.Client/                 # Blazor WebAssembly
│   │   ├── Pages/                      # Razor Pages
│   │   │   ├── Home.razor
│   │   │   ├── Projects.razor
│   │   │   └── ProjectDetails.razor
│   │   ├── Services/                   # HTTP Client Services
│   │   │   ├── ProjectService.cs
│   │   │   ├── TestCaseService.cs
│   │   │   └── RequirementService.cs
│   │   ├── Layout/                     # Layout Components
│   │   └── Program.cs                  # Client Startup
│   │
│   ├── SuperQA.Core/                   # Domain Layer
│   │   ├── Entities/                   # Domain Models
│   │   │   ├── Project.cs
│   │   │   ├── Requirement.cs
│   │   │   ├── TestCase.cs
│   │   │   ├── TestExecution.cs
│   │   │   ├── DefectPrediction.cs
│   │   │   └── AIPromptLog.cs
│   │   └── Interfaces/                 # Service Contracts
│   │       ├── IMCPService.cs
│   │       ├── IAITestGeneratorService.cs
│   │       ├── IAIAnalyzerService.cs
│   │       └── ISelfHealingService.cs
│   │
│   ├── SuperQA.Infrastructure/         # Infrastructure Layer
│   │   ├── Data/                       # Database Context
│   │   │   └── SuperQADbContext.cs
│   │   └── Services/                   # Service Implementations
│   │       ├── MCPService.cs
│   │       └── AITestGeneratorService.cs
│   │
│   └── SuperQA.Shared/                 # Shared Components
│       └── DTOs/                       # Data Transfer Objects
│           ├── ProjectDto.cs
│           ├── RequirementDto.cs
│           └── TestCaseDto.cs
│
├── tests/
│   └── SuperQA.Tests/                  # Unit Tests
│
└── docs/                               # Documentation
    └── MCP_INTEGRATION.md
```

## Component Responsibilities

### Client Layer (Blazor WebAssembly)

**Responsibilities:**
- User interface rendering
- User interaction handling
- API communication
- Client-side routing

**Key Components:**
- **Pages**: Razor components for each view
- **Services**: HTTP client wrappers for API calls
- **Layout**: Shared layout components (navigation, etc.)

### Application Layer (ASP.NET Core API)

**Responsibilities:**
- HTTP request handling
- Request validation
- Response formatting
- CORS management
- Authentication/Authorization (planned)

**Key Components:**
- **Controllers**: REST API endpoints
- **Middleware**: Cross-cutting concerns
- **Configuration**: App settings and DI setup

### Business Layer (Core Services)

**Responsibilities:**
- Business logic implementation
- Domain model definitions
- Service contracts (interfaces)
- Business rules enforcement

**Key Components:**
- **Entities**: Domain models
- **Interfaces**: Service contracts
- **Services**: AI integration, test generation, self-healing

### Infrastructure Layer

**Responsibilities:**
- Database access
- External service integration
- File I/O
- Caching (future)

**Key Components:**
- **DbContext**: Entity Framework Core context
- **Services**: Concrete implementations of core interfaces
- **Migrations**: Database schema versioning

### Data Layer

**Responsibilities:**
- Data persistence
- Query optimization
- Data integrity
- Backup and recovery

**Key Components:**
- **SQL Server**: Relational database
- **Tables**: Normalized schema
- **Indexes**: Performance optimization

## Data Flow Patterns

### 1. Create Project Flow

```
User → Blazor UI → ProjectService → API Controller → DbContext → Database
                       ↓
                   Update UI
```

### 2. Generate Test Cases Flow (with AI)

```
User → Blazor UI → TestCaseService → API Controller 
                                          ↓
                                   AITestGenerator
                                          ↓
                                     MCPService
                                          ↓
                                      AI Model
                                          ↓
                                     DbContext → Database
                                          ↓
                                      Return to UI
```

### 3. View Requirements Flow

```
User → Blazor UI → RequirementService → API Controller → DbContext → Database
                                              ↓
                                         Map to DTO
                                              ↓
                                         Return JSON
                                              ↓
                                         Display in UI
```

## Technology Decisions

### Why Blazor WebAssembly?

- **C# Full Stack**: Same language for frontend and backend
- **Performance**: Runs in WebAssembly, near-native speed
- **Offline Capable**: PWA support
- **Type Safety**: Compile-time checking
- **Component Reusability**: Share components easily

### Why ASP.NET Core?

- **Cross-platform**: Runs on Windows, Linux, macOS
- **Performance**: High-throughput, low-latency
- **Modern**: Built-in DI, middleware pipeline
- **Ecosystem**: Rich NuGet package ecosystem

### Why Entity Framework Core?

- **Productivity**: LINQ queries, migrations
- **Flexibility**: Code-first or database-first
- **Performance**: Optimized queries, change tracking
- **Tooling**: Visual Studio integration

### Why SQL Server?

- **Reliability**: ACID compliance
- **Features**: Full-text search, JSON support
- **Tooling**: SSMS, Azure integration
- **Performance**: Query optimization, indexing

## Security Architecture

### Current Implementation

- HTTPS enforcement
- CORS configuration
- Input validation in DTOs

### Planned Features

- JWT authentication
- Role-based authorization (Admin, Tester, Viewer)
- API rate limiting
- SQL injection prevention (via EF Core parameterization)
- XSS prevention (Blazor auto-escaping)

## Scalability Considerations

### Current Limitations

- Single database instance
- Synchronous AI calls
- No caching layer

### Future Improvements

- **Horizontal Scaling**: Multiple API instances behind load balancer
- **Database Scaling**: Read replicas, sharding
- **Caching**: Redis for frequently accessed data
- **Message Queue**: RabbitMQ/Azure Service Bus for async processing
- **CDN**: Static asset delivery
- **Microservices**: Break into smaller services if needed

## Deployment Architecture

### Development

```
Local Machine
├── SQL Server LocalDB
├── ASP.NET Core API (Port 7001)
└── Blazor WASM (Port 5001)
```

### Production (Planned)

```
Azure Cloud
├── Azure SQL Database
├── Azure App Service (API)
├── Azure Static Web Apps (Blazor)
├── Azure Key Vault (Secrets)
└── Application Insights (Monitoring)
```

## Monitoring and Observability

### Metrics to Track

- API response times
- Database query performance
- AI service latency
- Error rates
- User activity

### Tools (Planned)

- Application Insights
- Azure Monitor
- Serilog for structured logging
- Health check endpoints

## Future Architecture Enhancements

1. **Event Sourcing**: For audit trail and replay
2. **CQRS**: Separate read/write models
3. **GraphQL**: More flexible API queries
4. **gRPC**: High-performance inter-service communication
5. **Docker**: Containerization for easy deployment
6. **Kubernetes**: Container orchestration
