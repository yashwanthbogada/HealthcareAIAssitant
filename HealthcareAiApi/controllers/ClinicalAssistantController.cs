using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using HealthcareAiApi.Models;

[ApiController]
[Route("api/[controller]")]
public class ClinicalAssistantController : ControllerBase
{
    private readonly IChatClient _chatClient;

    public ClinicalAssistantController(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    [HttpPost("query")]
    public async Task<IActionResult> QueryAssistant([FromBody] ClinicalQueryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a secure clinical operations assistant helping hospital staff summarize administrative and workflow documentation. Never provide direct medical diagnoses."),
            new ChatMessage(ChatRole.User, request.Prompt)
        };
        ChatResponse response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

        return Ok(new ClinicalQueryResponse
        {
            ModelUsed = "phi4-local",
            ResponseText = response.Text,
            Timestamp = DateTime.UtcNow
        });

        
    }
    [HttpPost("stream")]
    public async IAsyncEnumerable<string> StreamAssistant(
        [FromBody] ClinicalQueryRequest request, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            yield break;
        }

        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a secure clinical operations assistant helping hospital staff summarize administrative and workflow documentation. Never provide direct medical diagnoses."),
            new ChatMessage(ChatRole.User, request.Prompt!)
        };

        // Stream chunks from the local model as they are generated
        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text;
            }
        }
    }
    [HttpPost("structured")]
    public async Task<IActionResult> GetStructuredSummary([FromBody] ClinicalQueryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var prompt = $"Analyze and summarize the following clinical operations query: {request.Prompt}";

        // This enforces that the local model maps its response directly into our C# type contract
        var response = await _chatClient.GetResponseAsync<ClinicalSummaryResult>(prompt, cancellationToken: cancellationToken);

        return Ok(new
        {
            ModelUsed = "phi4-local",
            Data = response.Result,
            Timestamp = DateTime.UtcNow
        });
    }
}
public class ClinicalQueryRequest
{
    [Required(ErrorMessage = "The prompt field is mandatory.")]
    [StringLength(2000, MinimumLength = 3, ErrorMessage = "Prompt must be between 3 and 2000 characters.")]
    public string? Prompt { get; set; }
}

public class ClinicalQueryResponse
{
    public string ModelUsed { get; set; } = string.Empty;
    public string? ResponseText { get; set; }
    public DateTime Timestamp { get; set; }
}
