# SampleAISemanticKernelAgent
This usage SemanticKernel agent framework and demo basic of AI Agents 
# Employee Support AI Agent

> **v2 — Proper AI Agent with Semantic Kernel + Azure OpenAI**
> This is a rebuild of the original Ollama-based prototype. See [v1 notes](#v1-prototype-notes) at the bottom for what changed and why.

## Overview

This project demonstrates a production-quality AI Agent built with **Microsoft Semantic Kernel** and **Azure OpenAI**, deployed to **Azure App Service** with an Angular frontend.

The agent uses proper **automatic function/tool calling** — the LLM itself decides which Skill (Plugin) to invoke based on the user's message. There is no manual routing, no hardcoded if-else, and no prompt engineering tricks to work around model limitations.

The project demonstrates:

* Proper `ChatCompletionAgent` from Semantic Kernel Agents framework
* SK Plugins with `[KernelFunction]` — the industry-standard way to give an LLM tools
* `FunctionChoiceBehavior.Auto()` — LLM autonomously selects and calls tools
* Azure OpenAI GPT-4o-mini as the LLM (capable of reliable tool calling)
* Multi-turn conversation history (the agent remembers context across messages)
* SKILL.md files drive plugin descriptions and system prompts
* Azure App Service deployment with Azure AI Foundry integration

---

# Tech Stack

| Component       | Technology                                      | Why                                                   |
| --------------- | ----------------------------------------------- | ----------------------------------------------------- |
| UI              | Angular 18                                      | Existing frontend, unchanged                          |
| Backend Agent   | .NET 10 Web API                                 | Existing stack, updated to net10                      |
| AI Framework    | **Microsoft Semantic Kernel** (Agents + Core)   | Industry standard for .NET agents, Microsoft official |
| LLM             | **Azure OpenAI GPT-4o-mini**                    | Required for reliable auto tool calling               |
| Agent Object    | `ChatCompletionAgent` (SK Agents)               | Proper agent lifecycle, history, tool loop            |
| Tool Calling    | `FunctionChoiceBehavior.Auto()`                 | LLM picks tools automatically — no manual routing     |
| Skill Registry  | SK `KernelPlugin` with `[KernelFunction]`       | Standard plugin pattern, discoverable by LLM          |
| Skill Prompts   | SKILL.md files (YAML + Markdown body)           | Prompt-as-config, no C# changes to update behaviour   |
| Chat History    | `ChatHistory` (SK built-in)                     | Multi-turn memory within a session                    |
| Hosting         | Azure App Service (backend) + Azure Static Web  | App for Angular frontend                              |
| AI Resources    | Azure AI Foundry / Azure OpenAI Service         | GPT-4o-mini deployment                                |
| Config          | Azure App Service environment variables         | Secure key/endpoint management                        |

---

# Why Semantic Kernel over Microsoft Agent Framework?

| Criteria                  | Semantic Kernel (C#)                         | Microsoft Agent Framework (Python)           |
| ------------------------- | -------------------------------------------- | -------------------------------------------- |
| Language                  | C# / .NET — matches existing codebase        | Python — would require full rewrite           |
| Maturity                  | GA, production-ready                         | GA, but Python-first                         |
| Azure OpenAI integration  | First-class, built-in connector              | First-class, built-in connector              |
| Tool / Function calling   | `[KernelFunction]` + Auto behavior           | `@tool` + Auto behavior                      |
| Multi-turn history        | `ChatHistory` built-in                       | `ConversationThread` built-in               |
| Industry recommendation   | **Recommended for .NET workloads**           | Recommended for Python workloads             |

**Decision: Semantic Kernel** — same framework already referenced in the project, same language, proper agent classes now available in `Microsoft.SemanticKernel.Agents.Core`.

---

# Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Angular Frontend                         │
│              (Azure Static Web App or App Service)              │
└────────────────────────────┬────────────────────────────────────┘
                             │  POST /api/chat  { message, sessionId }
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                  .NET 10 Web API  (App Service)                  │
│                                                                  │
│  ChatController                                                  │
│       │                                                          │
│       ▼                                                          │
│  AgentService                                                    │
│       │  creates / reuses                                        │
│       ▼                                                          │
│  ChatCompletionAgent  ◄── SK Agents SDK                         │
│       │  owns                                                    │
│       ├── ChatHistory  (per-session, in-memory)                  │
│       └── Kernel                                                 │
│             │  registered plugins                                │
│             ├── HRPlugin    (KernelFunction methods)             │
│             │     ├── GetLeaveBalance(employeeName)              │
│             │     └── GetEmployeeInfo(employeeName)              │
│             └── GreetingPlugin                                   │
│                   └── SayHello()                                 │
│                                                                  │
│  FunctionChoiceBehavior.Auto()                                   │
│  → LLM decides which function to call, SK executes it           │
└──────────────────────┬──────────────────────────────────────────┘
                       │  Azure OpenAI API
                       ▼
┌─────────────────────────────────────────────────────────────────┐
│             Azure OpenAI  (GPT-4o-mini)                         │
│             via Azure AI Foundry project                         │
└─────────────────────────────────────────────────────────────────┘
```

### How the agent loop works (no more manual routing)

```
User: "How many holidays does John have?"
           │
           ▼
  ChatCompletionAgent sends message + ChatHistory + registered tools to GPT-4o-mini
           │
           ▼
  GPT-4o-mini responds: { tool_call: "GetLeaveBalance", args: { employeeName: "John" } }
           │
           ▼
  SK executes HRPlugin.GetLeaveBalance("John")  ← real C# data
           │
           ▼
  SK feeds result back to GPT-4o-mini
           │
           ▼
  GPT-4o-mini generates: "John has 12 leave days remaining."
```

---

# Azure Resources Required

| Resource                     | Purpose                                     |
| ---------------------------- | ------------------------------------------- |
| Azure AI Foundry project     | Manage model deployments, monitoring        |
| Azure OpenAI Service         | Host GPT-4o-mini deployment                 |
| Azure App Service (Linux)    | Host .NET 10 backend                        |
| Azure Static Web App         | Host Angular frontend (free tier)           |

---

# Implementation Plan

## Phase 1 — Backend Rebuild

### Step 1 — Update NuGet packages

Remove the raw `HttpClient`-to-Ollama approach. Add:

```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
<PackageReference Include="Microsoft.SemanticKernel.Agents.Core" Version="1.*" />
```

### Step 2 — Configuration (appsettings.json)

```json
"AzureOpenAI": {
  "Endpoint": "https://<your-resource>.openai.azure.com/",
  "ApiKey":   "<from Azure Portal or environment variable>",
  "DeploymentName": "gpt-4o-mini"
}
```

In production (App Service), `ApiKey` is set as an **App Service environment variable** — never committed to source control.

### Step 3 — Build the Kernel (Program.cs)

```csharp
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
    .Build();

kernel.Plugins.AddFromType<HRPlugin>();
kernel.Plugins.AddFromType<GreetingPlugin>();
```

### Step 4 — Create the ChatCompletionAgent (AgentService.cs)

```csharp
_agent = new ChatCompletionAgent
{
    Name        = "HRSupportAgent",
    Instructions = File.ReadAllText("Skills/agent-instructions.md"),
    Kernel      = kernel,
    Arguments   = new KernelArguments(
        new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        })
};
```

`FunctionChoiceBehavior.Auto()` is the key: the LLM receives the function signatures of all registered plugins and calls them autonomously. No routing prompt, no digit classification.

### Step 5 — Convert Skills to SK Plugins

Each skill becomes a class with `[KernelFunction]` + `[Description]` attributes. The `[Description]` is what the LLM reads to understand when to call the function — equivalent to the triggers/description in SKILL.md.

```csharp
public class HRPlugin
{
    [KernelFunction("GetLeaveBalance")]
    [Description("Returns the leave/holiday/vacation/PTO balance for a named employee")]
    public string GetLeaveBalance(string employeeName) { ... }

    [KernelFunction("GetEmployeeInfo")]
    [Description("Returns department and profile information for a named employee")]
    public string GetEmployeeInfo(string employeeName) { ... }
}
```

### Step 6 — Multi-turn Chat History

```csharp
// Per-session dictionary (keyed by sessionId from frontend)
private readonly ConcurrentDictionary<string, ChatHistory> _sessions = new();

public async Task<string> ChatAsync(string sessionId, string userMessage)
{
    var history = _sessions.GetOrAdd(sessionId, _ => new ChatHistory());
    await foreach (var msg in _agent.InvokeAsync(history))
        return msg.Content;
}
```

### Step 7 — Keep SKILL.md files for the agent system prompt

`Skills/agent-instructions.md` replaces the per-skill SKILL.md system prompts. It defines the agent's persona and overarching rules. Individual plugin `[Description]` attributes handle routing.

---

## Phase 2 — Azure Setup

### Step 1 — Create Azure AI Foundry project

```bash
az cognitiveservices account create \
  --name hr-agent-openai \
  --resource-group rg-hr-agent-arvind \
  --kind OpenAI \
  --sku S0 \
  --location eastus
```

### Step 2 — Deploy GPT-4o-mini model

In Azure AI Foundry portal → Model Deployments → Deploy `gpt-4o-mini`.

### Step 3 — Create App Service

```bash
az webapp create \
  --name hr-agent-backend \
  --resource-group rg-hr-agent-arvind \
  --runtime "DOTNET|10.0" \
  --plan hr-agent-plan
```

### Step 4 — Set environment variables on App Service

```bash
az webapp config appsettings set \
  --name hr-agent-backend \
  --resource-group rg-hr-agent-arvind \
  --settings \
    AzureOpenAI__Endpoint="https://..." \
    AzureOpenAI__ApiKey="<key>" \
    AzureOpenAI__DeploymentName="gpt-4o-mini"
```

### Step 5 — Deploy backend

```bash
dotnet publish -c Release
az login --tenant "f5bee52c-26bb-4685-9e60-bfdad5375f6f" --use-device-code
az webapp deploy --name hr-agent-backend --resource-group rg-hr-agent-arvind --src-path ./publish.zip --type zip
```

### Step 6 — Deploy Angular frontend to Azure Static Web App

Update `environment.ts` with the App Service backend URL, then deploy via GitHub Actions or `swa deploy`.

cd "C:\WorkSpace\AgenticAILearnings\SampleAIAgentWithSkill\frontend-angular"

# 1 — build
npx ng build --configuration production

# 2 — get token
$token = az staticwebapp secrets list --name hr-agent-frontend --resource-group rg-hr-agent-arvind --query "properties.apiKey" -o tsv

# 3 — deploy
$client = "$env:USERPROFILE\.swa\deploy\08e29138cd3dcda4ffda6d587aa580028110c1c7\StaticSitesClient.exe"
$pinfo = New-Object System.Diagnostics.ProcessStartInfo
$pinfo.FileName = $client
$pinfo.Arguments = "upload --app `"dist/frontend-angular/browser`" --outputLocation `"dist/frontend-angular/browser`" --apiToken `"$token`" --skipAppBuild --skipApiBuild --verbose"
$pinfo.WorkingDirectory = (Get-Location).Path
$pinfo.RedirectStandardOutput = $true; $pinfo.UseShellExecute = $false
$p = [System.Diagnostics.Process]::new(); $p.StartInfo = $pinfo
$p.Start() | Out-Null; Write-Host $p.StandardOutput.ReadToEnd(); $p.WaitForExit()

---

## Phase 3 — What stays the same

| Item | Status |
|---|---|
| `employees.json` mock data | Unchanged |
| Angular chat UI | Unchanged |
| `ChatController.cs` | Minor update — add `sessionId` param |
| `Models.cs` | Unchanged |
| SKILL.md files | Repurposed as agent system prompt source |

---

# Project Structure (v2)

```
SampleAIAgentWithSkill/
│
├── frontend-angular/              ← unchanged Angular app
│
├── backend-agent/
│   ├── Controllers/
│   │   └── ChatController.cs      ← updated: sessionId support
│   │
│   ├── Data/
│   │   └── employees.json         ← unchanged mock data
│   │
│   ├── Models/
│   │   └── Models.cs              ← unchanged
│   │
│   ├── Plugins/                   ← renamed from Skills/
│   │   ├── HRPlugin.cs            ← [KernelFunction] methods for leave + employee
│   │   └── GreetingPlugin.cs      ← [KernelFunction] greeting handler
│   │
│   ├── Skills/                    ← SKILL.md prompt files (kept)
│   │   ├── agent-instructions.md  ← NEW: agent persona + global rules
│   │   ├── employee/SKILL.md      ← plugin description reference
│   │   ├── leave/SKILL.md         ← plugin description reference
│   │   └── greeting/SKILL.md      ← plugin description reference
│   │
│   ├── Services/
│   │   └── AgentService.cs        ← REBUILT: ChatCompletionAgent + ChatHistory
│   │
│   ├── appsettings.json           ← AzureOpenAI config keys (no secrets)
│   ├── appsettings.Development.json
│   └── Program.cs                 ← Kernel + Plugin registration
│
└── README.md
```

---

# Key Differences: v1 (Manual) vs v2 (Proper Agent)

| Concern                | v1 — Manual (current)                    | v2 — Proper SK Agent                          |
| ---------------------- | ---------------------------------------- | --------------------------------------------- |
| Agent object           | None — `AgentService` plays agent role   | `ChatCompletionAgent` from SK Agents SDK      |
| Routing                | Manual LLM call outputting a digit       | `FunctionChoiceBehavior.Auto()` — LLM decides |
| Tool calling           | C# switch statement                      | SK executes `[KernelFunction]` automatically  |
| LLM                    | Ollama llama3.2:3b (local)               | Azure OpenAI GPT-4o-mini (cloud)              |
| Multi-turn memory      | None — each message is independent       | `ChatHistory` per session                     |
| Synonym handling       | Manual — add triggers to SKILL.md        | LLM semantic understanding — "holidays" = leave |
| Deployment             | Local only                               | Azure App Service + Azure Static Web App      |
| Skill registration     | File scan + manual switch                | `kernel.Plugins.AddFromType<T>()`             |
| Config management      | Hard-coded URL in C#                     | `appsettings.json` + Azure env vars           |

---

# v1 Prototype Notes

The original implementation was intentionally limited because `llama3.2:3b` is too small for reliable automatic tool calling. It used a two-stage manual routing approach as a workaround. Now that we target Azure OpenAI GPT-4o-mini, we can use the standard SK agent pattern that the workaround was designed to emulate.

https://green-hill-0b259a703.7.azurestaticapps.net/


