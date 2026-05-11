using System.Text.Json;
using BackendAgent.Models;
using BackendAgent.Plugins;
using BackendAgent.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// CORS — Allow Angular frontend (different port) to call the API
// =====================================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// =====================================================
// MOCK EMPLOYEE DATA
// employees.json simulates a database for this demo.
// =====================================================
var dataPath    = Path.Combine(AppContext.BaseDirectory, "Data", "employees.json");
var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var employees   = JsonSerializer.Deserialize<Dictionary<string, EmployeeInfo>>(
                      File.ReadAllText(dataPath), jsonOptions)
                  ?? new Dictionary<string, EmployeeInfo>();

// =====================================================
// SEMANTIC KERNEL + AZURE OPENAI
// Kernel is the brain of the agent. We register plugins
// here so the LLM knows which tools it can call.
// =====================================================
var azCfg = builder.Configuration.GetSection("AzureOpenAI");

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: azCfg["DeploymentName"]!,
        endpoint:       azCfg["Endpoint"]!,
        apiKey:         azCfg["ApiKey"]!)
    .Build();

// Register plugins — each [KernelFunction] becomes a tool the LLM can call.
// FunctionChoiceBehavior.Auto() (set in AgentService) lets GPT-4o-mini pick tools automatically.
kernel.Plugins.AddFromObject(new HRPlugin(employees), "HRPlugin");
kernel.Plugins.AddFromObject(new GreetingPlugin(),    "GreetingPlugin");

builder.Services.AddSingleton(employees);
builder.Services.AddSingleton(kernel);

// =====================================================
// AI AGENT SERVICE
// AgentService creates and owns the ChatCompletionAgent.
// Singleton because the agent + kernel are expensive to build.
// =====================================================
builder.Services.AddSingleton<AgentService>();

builder.Services.AddControllers();

var app = builder.Build();

// Pre-warm the agent so the first request is fast
app.Services.GetRequiredService<AgentService>();

app.UseCors();
app.MapControllers();
app.Run();

