using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using HealthcareAiApi.Models;
using HealthcareAiApi.Data;
using Microsoft.EntityFrameworkCore;
using HealthcareAiApi.Middleware;

[ApiController]
[Route("api/[controller]")]
public class ClinicalAssistantController : ControllerBase
{
    private readonly IChatClient _chatClient;
    private readonly ClinicalDbContext _context;
    private readonly KnowledgeRetrievalService _knowledgeRetrievalService;
    private readonly VectorKnowledgeRetrievalService _vectorRS;
    public ClinicalAssistantController(IChatClient chatClient, ClinicalDbContext context, KnowledgeRetrievalService retrievalService, VectorKnowledgeRetrievalService vectorKnowledgeRetrieval)
    {
        _chatClient = chatClient;
        _context= context;
        _knowledgeRetrievalService= retrievalService;
        _vectorRS= vectorKnowledgeRetrieval;
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

    [HttpPost("chat")]
    public async Task<IActionResult> ChatWithContext([FromBody] ChatConversationRequest request, CancellationToken cancellationToken)
    {
        if (request.Messages == null || request.Messages.Count == 0)
        {
            return BadRequest("Conversation messages cannot be empty.");
        }

        // Map incoming conversation DTOs to Microsoft.Extensions.AI ChatMessage objects
        var chatMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a secure clinical operations assistant helping hospital staff summarize administrative and workflow documentation. Never provide direct medical diagnoses.")
        };

        foreach (var m in request.Messages)
        {
            chatMessages.Add(new ChatMessage(
                m.Role.ToLower() == "assistant" ? ChatRole.Assistant : ChatRole.User,
                m.Content
            ));
        }

        // Call GetResponseAsync with the full message history
        ChatResponse response = await _chatClient.GetResponseAsync(chatMessages, cancellationToken: cancellationToken);
        var assistantReply = response.Text ?? string.Empty;

        // Persist interaction to SQLite
        var latestUserPrompt = request.Messages.LastOrDefault(m => m.Role.ToLower() == "user")?.Content ?? "";
        _context.ClinicalLogs.Add(new ClinicalLog {
            Prompt = latestUserPrompt,
            ResponseContent = assistantReply,
            EndpointType = "chat"
        });
        await _context.SaveChangesAsync();

        return Ok(new { response = assistantReply });
    }
  

    [HttpPost("chat-stream")]
    public async Task ChatStreamWithContext([FromBody] ChatConversationRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/plain; charset=utf-8";

        if (request.Messages == null || request.Messages.Count == 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Conversation messages cannot be empty.");
            return;
        }
        var latestUserPrompt = request.Messages.LastOrDefault(m => m.Role.ToLower() == "user")?.Content ?? "";
    
        // Retrieve relevant context from our local text files
        //var retrievedKnowledge = await _knowledgeRetrievalService.SearchRelevantContextAsync(latestUserPrompt);

         // Retrieve relevant context from our local text files using Vector revtrieval
        var retrievedKnowledge = _vectorRS.SearchRelevantContextAsync(latestUserPrompt);
        // Map DTOs to ChatMessage objects with system context
        var chatMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, 
                $"You are a secure clinical operations assistant. Use the following internal hospital policy context if relevant to the user's question:\n\n{retrievedKnowledge}")
        };

        foreach (var m in request.Messages)
        {
            chatMessages.Add(new ChatMessage(
                m.Role.ToLower() == "assistant" ? ChatRole.Assistant : ChatRole.User,
                m.Content
            ));
        }

        var fullResponseText = new System.Text.StringBuilder();

        try
        {
            // Stream chunks from Ollama via Microsoft.Extensions.AI
            await foreach (var update in _chatClient.GetStreamingResponseAsync(chatMessages, cancellationToken: cancellationToken))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    fullResponseText.Append(update.Text);
                    await Response.WriteAsync(update.Text);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }

            // Log the complete interaction to SQLite once streaming finishes
            var latestUserPrompt1 = request.Messages.LastOrDefault(m => m.Role.ToLower() == "user")?.Content ?? "";
            _context.ClinicalLogs.Add(new ClinicalLog {
                Prompt = latestUserPrompt1,
                ResponseContent = fullResponseText.ToString(),
                EndpointType = "chat"
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Handle stream interruption gracefully
            Console.WriteLine($"Stream error: {ex.Message}");
        }
    }

    // GET: api/ClinicalAssistant/threads
    [HttpGet("threads")]
    public async Task<IActionResult> GetChatThreads()
    {
       var threads = await _context.ClinicalLogs
        .Where(l => !string.IsNullOrEmpty(l.ThreadId))
        .OrderByDescending(l => l.Timestamp)
        .Select(l => l.ThreadId)
        .Distinct()
        .Take(10)
        .ToListAsync();
        return Ok(threads);
    }

    // GET: api/ClinicalAssistant/thread/{threadId}
    [HttpGet("thread/{threadId}")]
    public async Task<IActionResult> GetThreadMessages(string threadId)
    {
        var messages = await _context.ClinicalLogs
            .Where(l => l.ThreadId == threadId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();

        return Ok(messages);
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
public class ChatConversationRequest
{
    public List<ChatMessageDto> Messages { get; set; } = new();
}

public class ChatMessageDto
{
    public string Role { get; set; } = "user"; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
}