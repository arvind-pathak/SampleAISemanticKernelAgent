using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace BackendAgent.Plugins;

/// <summary>
/// GreetingPlugin — handles greetings and "what can you do?" questions.
///
/// The [Description] is read by the LLM to decide when to call SayHello().
/// The function returns a friendly intro that also proves SKILL.md integration.
/// </summary>
public class GreetingPlugin
{
    [KernelFunction]
    [Description("Responds to greetings (hi, hello, hey, good morning), help requests, and questions about what the agent can do.")]
    public string SayHello()
    {
        return "Hello! I'm your HR Support Agent. I can help you with:\n" +
               "  • Employee leave balances — e.g. \"How many holidays does John have?\"\n" +
               "  • Employee department info — e.g. \"Which department does Sarah work in?\"\n" +
               "[Powered by: GreetingPlugin.SayHello → Skills/greeting/SKILL.md]";
    }
}
