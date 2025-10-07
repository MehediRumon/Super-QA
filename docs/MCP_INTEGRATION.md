# MCP Integration Guide

## Overview

Super-QA leverages the Model Context Protocol (MCP) to enable seamless communication with AI models for intelligent test automation features.

## What is MCP?

The Model Context Protocol (MCP) is a standardized protocol for applications to communicate with AI models and tools dynamically. It acts as a "brain bridge" between:

- AI models (GPT, Claude, or local LLMs)
- Runtime data in your application (requirements, test logs, code)
- Testing engines (Selenium/Playwright)

## MCP Architecture in Super-QA

```
┌─────────────────────────────────────────────────────────────┐
│                    Blazor WebAssembly UI                     │
│  ┌──────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │Dashboard │  │Test Gen Form │  │AI Assistant Chat      │  │
│  └──────────┘  └──────────────┘  └──────────────────────┘  │
└────────────────────────────┬────────────────────────────────┘
                             │ HTTP/JSON
┌────────────────────────────▼────────────────────────────────┐
│                    ASP.NET Core API                          │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────────────┐ ┌───────────┐│
│  │ Auth │ │ Data │ │ File │ │AI Integration│ │Test Runner││
│  └──────┘ └──────┘ └──────┘ └──────────────┘ └───────────┘│
│                      (EF Core)    (MCP Service)             │
└────────────────────────────┬────────────────────────────────┘
                             │ MCP Protocol
┌────────────────────────────▼────────────────────────────────┐
│                    MCP + AI Layer                            │
│  ┌─────────────────────┐    ┌──────────────────────────┐   │
│  │ LLM (GPT, Claude)   │    │ Embedding Store          │   │
│  │ Prompt Templates     │    │ (Vector DB)              │   │
│  └─────────────────────┘    └──────────────────────────┘   │
│                  Model Context Protocol (MCP)               │
└─────────────────────────────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────┐
│              Database & Storage (SQL Server)                 │
│  Projects | Requirements | Test Cases | Logs | AI Prompts   │
└─────────────────────────────────────────────────────────────┘
```

## Implementation Details

### 1. MCP Service Interface

Located in `SuperQA.Core/Interfaces/IMCPService.cs`:

```csharp
public interface IMCPService
{
    Task<string> SendPromptAsync(string prompt, string context);
    Task<T> SendStructuredPromptAsync<T>(string prompt, string context);
    Task LogPromptAsync(AIPromptLog log);
}
```

### 2. AI Features Using MCP

#### A. AI Test Case Generator

**Service**: `AITestGeneratorService`  
**Purpose**: Generate comprehensive test cases from requirements

**Flow**:
1. User uploads requirement/user story in Blazor UI
2. MCP Service sends the requirement context to AI model
3. AI generates test cases with preconditions, steps, and expected results
4. Test cases are displayed in Blazor UI and stored in database

#### B. Self-Healing Automation (Coming Soon)

**Purpose**: Automatically fix broken test locators when tests fail

#### C. Defect Prediction (Coming Soon)

**Purpose**: Predict high-risk modules using ML

#### D. Chatbot QA Assistant (Coming Soon)

**Purpose**: Context-aware assistant for QA queries

## Configuration

Update `src/SuperQA.Api/appsettings.json`:

```json
{
  "MCP": {
    "Endpoint": "http://localhost:3000",
    "Model": "gpt-4"
  }
}
```

## Supported AI Models

- **OpenAI GPT-4/GPT-3.5**: Production-ready, API-based
- **Anthropic Claude**: Alternative production option
- **Local LLMs**: Llama 2, Mistral (via Ollama)
- **Azure OpenAI**: Enterprise-grade deployment

## Resources

- [Model Context Protocol](https://modelcontextprotocol.io)
- [OpenAI API](https://platform.openai.com/docs)
- [Ollama - Local LLM](https://ollama.ai)
