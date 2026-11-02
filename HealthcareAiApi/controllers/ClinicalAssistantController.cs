using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using HealthcareAiApi.Models;
using HealthcareAiApi.Data;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ClinicalAssistantController : ControllerBase
{
    private readonly IChatClient _chatClient;
    private readonly ClinicalDbContext _context;
    public ClinicalAssistantController(IChatClient chatClient, ClinicalDbContext context)
    {
        _chatClient = chatClient;
        _context= context;
    }
    
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<ClinicalLog>>> GetHistory()
    {
        return await _context.ClinicalLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(20)
            .ToListAsync();
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
        var resultText= response.Text;
        // Added: Persist record to SQLite
        _context.ClinicalLogs.Add(new ClinicalLog {
            Prompt = request.Prompt!,
            ResponseContent = resultText ?? "",
            EndpointType = "query"
        });
        await _context.SaveChangesAsync();
        return Ok(new ClinicalQueryResponse
        {
            ModelUsed = "phi4-mini",
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
        Response.ContentType = "text/plain; charset=utf-8";
        var fullResponse = "";
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
               fullResponse += update.Text;
               await Response.WriteAsync(update.Text, cancellationToken);
               await Response.Body.FlushAsync(cancellationToken);
            }
        }
        // Save stream log asynchronously after completion
        try 
        {
            // Use a new scope or context instance if needed, or save directly if scoped context is safe
            _context.ClinicalLogs.Add(new ClinicalLog {
                Prompt = request.Prompt!,
                ResponseContent = fullResponse,
                EndpointType = "stream"
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save stream log: {ex.Message}");
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
        var resultText = response.Result.ToString();

        // Save to SQLite
        _context.ClinicalLogs.Add(new ClinicalLog {
            Prompt = request.Prompt!,
            ResponseContent = resultText ?? "",
            EndpointType = "structured"
        });
        await _context.SaveChangesAsync();
        return Ok(new
        {
            ModelUsed = "phi4-mini",
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
