using HealthcareAiApi.Middleware;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);
var ollamaBaseUrl = builder.Configuration["OllamaSettings:BaseUrl"] ?? "http://localhost:11434";
// Register IChatClient with Microsoft.Extensions.AI pointing to local Ollama
builder.Services.AddChatClient(services =>
{
    var endpoint = new Uri(ollamaBaseUrl);
    var credential = new ApiKeyCredential("ollama"); // Dummy key for local Ollama
    IChatClient baseClient= new OpenAIClient(credential, new OpenAIClientOptions { Endpoint = endpoint })
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