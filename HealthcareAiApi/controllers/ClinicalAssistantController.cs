using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

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
    public async Task<IActionResult> QueryAssistant([FromBody] ClinicalQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest("Prompt cannot be empty.");
        }

       var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, "You are a secure clinical operations assistant helping hospital staff summarize administrative and workflow documentation. Never provide direct medical diagnoses."),
            new ChatMessage(ChatRole.User, request.Prompt)
        };

        ChatResponse response = await _chatClient.GetResponseAsync(messages);

        return Ok(new 
        {
            ModelUsed = "phi4-local",
            Response = response.Text
        });
    }
}

public record ClinicalQueryRequest(string Prompt);