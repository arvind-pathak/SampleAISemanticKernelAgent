using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace BackendAgent.Services;

/// <summary>
/// AgentService — wraps a Semantic Kernel ChatCompletionAgent.
///
/// === HOW THIS WORKS (v2 — proper agent) ===
///
///   1. At startup a ChatCompletionAgent is created with:
///      - Instructions loaded from Skills/agent-instructions.md (agent persona + rules)
///      - The Kernel (pre-loaded with HRPlugin + GreetingPlugin)
///      - FunctionChoiceBehavior.Auto() — the LLM sees all plugin function signatures
///        and calls them autonomously.  No manual routing, no digit classification.
///
///   2. Per chat message:
///      - A ChatHistory is looked up (or created) by sessionId
///      - The user message is appended
///      - agent.InvokeAsync(history) is called
///      - SK + Azure OpenAI handle the tool-calling loop internally:
///          GPT-4o-mini → picks tool → SK executes C# function → feeds result back → final reply
///      - The assistant reply is appended to history (multi-turn memory)
///
/// === WHY THIS IS BETTER THAN v1 ===
///   v1 used a manual two-stage routing because llama3.2:3b couldn't do auto tool calling.
///   v2 uses Azure OpenAI GPT-4o-mini which handles FunctionChoiceBehavior.Auto() reliably.
///   The agent now understands synonyms ("holidays" = "leave") semantically.
/// </summary>
public class AgentService
{
    private readonly ChatCompletionAgent _agent;

    // One ChatHistory per session — enables multi-turn memory within a session.
    // Key = sessionId sent by the frontend (or "default" if not provided).
    private readonly ConcurrentDictionary<string, ChatHistory> _sessions = new();

    public AgentService(Kernel kernel, IWebHostEnvironment env)
    {
        // Load agent persona + rules from Skills/agent-instructions.md
        var instructionsPath = Path.Combine(env.ContentRootPath, "Skills", "agent-instructions.md");
        var instructions     = File.ReadAllText(instructionsPath);

        // ChatCompletionAgent — the proper SK agent object.
        // FunctionChoiceBehavior.Auto() tells GPT-4o-mini to call plugin functions autonomously.
        _agent = new ChatCompletionAgent
        {
            Name         = "HRSupportAgent",
            Instructions = instructions,
            Kernel       = kernel,
            Arguments    = new KernelArguments(
                new PromptExecutionSettings
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
        };

        Console.WriteLine("\n[Agent] ChatCompletionAgent ready.");
        Console.WriteLine($"[Agent] Plugins: {string.Join(", ", kernel.Plugins.Select(p => p.Name))}");
        Console.WriteLine($"[Agent] Instructions loaded from: {instructionsPath}\n");
    }

    /// <summary>
    /// Process a user message within a session.
    /// The SK agent loop handles: intent → tool selection → tool execution → final reply.
    /// </summary>
    public async Task<string> ChatAsync(string sessionId, string userMessage)
    {
        // Get or create the chat history for this session (multi-turn memory)
        var history = _sessions.GetOrAdd(sessionId, _ => new ChatHistory());
        history.AddUserMessage(userMessage);

        var fullResponse = new System.Text.StringBuilder();

        // InvokeAsync runs the full agent loop:
        //   1. Send history + registered tools to Azure OpenAI
        //   2. If GPT-4o-mini calls a tool → SK executes the C# KernelFunction
        //   3. Tool result is fed back to the LLM
        //   4. LLM generates the final natural-language reply
        await foreach (var message in _agent.InvokeAsync(history))
        {
            fullResponse.Append(message.Message.Content);
        }

        var response = fullResponse.ToString().Trim();
        Console.WriteLine($"[Agent] Session={sessionId} | Q: {userMessage} | A: {response}");
        return response;
    }
}