using BackendAgent.Models;
using BackendAgent.Services;
using Microsoft.AspNetCore.Mvc;

namespace BackendAgent.Controllers;

/// <summary>
/// ChatController - exposes a single POST /api/chat endpoint.
/// Receives user messages from the Angular UI and delegates to the ChatCompletionAgent.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AgentService _agentService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(AgentService agentService, ILogger<ChatController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/chat
    /// Accepts a user message and an optional sessionId, returns the agent reply.
    /// The chatCompletionAgent internally selects and invokes the correct plugin via
    /// FunctionChoiceBehavior.Auto() — no manual routing.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message cannot be empty." });

        // SessionId groups messages into a conversation (multi-turn memory).
        // Use "default" if the frontend doesn't send a sessionId.
        var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? "default" : request.SessionId;

        _logger.LogInformation("[Agent] Session={SessionId} | User: {Message}", sessionId, request.Message);

        var response = await _agentService.ChatAsync(sessionId, request.Message);

        _logger.LogInformation("[Agent] Session={SessionId} | Agent: {Response}", sessionId, response);

        return Ok(new ChatResponse(response));
    }
}

