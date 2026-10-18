using HealthcareAiApi.Middleware;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);
var ollamaBaseUrl = builder.Configuration["OllamaSettings:BaseUrl"] ?? "http://host.docker.internal:11434";

// Ensure the endpoint explicitly ends with /v1 for the OpenAI client compatibility layer
var endpointUri = new Uri(ollamaBaseUrl.EndsWith("/v1") ? ollamaBaseUrl : $"{ollamaBaseUrl.TrimEnd('/')}/v1");
// Register IChatClient with Microsoft.Extensions.AI pointing to local Ollama
builder.Services.AddChatClient(services =>
{
    var credential = new ApiKeyCredential("ollama"); // Dummy key for local Ollama
    IChatClient baseClient= new OpenAIClient(credential, new OpenAIClientOptions { Endpoint = endpointUri })
        .GetChatClient("phi4-mini:latest")
        .AsIChatClient();
 
 // 2. Wrap it with our enterprise telemetry decorator and resolve the logger via service provider
    var logger = services.GetRequiredService<ILogger<TelemetryChatClient>>();
    return new TelemetryChatClient(baseClient, logger);
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();
app.MapControllers();

app.Run();