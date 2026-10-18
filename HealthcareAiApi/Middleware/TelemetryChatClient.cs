using Microsoft.Extensions.AI;
using System.Diagnostics;

namespace HealthcareAiApi.Middleware;

public class TelemetryChatClient : DelegatingChatClient
{
    private readonly ILogger<TelemetryChatClient> _logger;

    public TelemetryChatClient(IChatClient innerClient, ILogger<TelemetryChatClient> logger) 
        : base(innerClient)
    {
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var modelId = options?.ModelId ?? "phi4-mini";

        _logger.LogInformation("--> Starting AI request to model '{ModelId}'", modelId);

        try
        {
            // Forward the call down the pipeline to the actual Ollama client
            var response = await base.GetResponseAsync(messages, options, cancellationToken);
            
            stopwatch.Stop();
            
            // Log enterprise telemetry metrics
            _logger.LogInformation(
                "<-- AI request completed successfully in {ElapsedMilliseconds}ms. Finish reason: {FinishReason}", 
                stopwatch.ElapsedMilliseconds, 
                response.FinishReason);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "XXX AI request failed after {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}