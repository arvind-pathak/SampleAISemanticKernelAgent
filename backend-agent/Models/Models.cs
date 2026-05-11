using System.Text.Json.Serialization;

namespace BackendAgent.Models;

/// <summary>
/// Represents an employee record loaded from employees.json mock data.
/// </summary>
public class EmployeeInfo
{
    [JsonPropertyName("department")]
    public string Department { get; set; } = string.Empty;

    [JsonPropertyName("leaveBalance")]
    public int LeaveBalance { get; set; }
}

/// <summary>
/// Incoming request from the Angular chat UI.
/// SessionId groups messages into a conversation so the agent remembers context.
/// The frontend should generate a UUID per chat session and include it on every request.
/// Falls back to "default" if omitted (all messages share one history).
/// </summary>
public record ChatRequest(string Message, string? SessionId = null);

/// <summary>
/// Outgoing response from the AI Agent to the Angular chat UI.
/// </summary>
public record ChatResponse(string Response);

