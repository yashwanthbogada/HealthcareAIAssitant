using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// Register IChatClient with Microsoft.Extensions.AI pointing to local Ollama
builder.Services.AddChatClient(services =>
{
    var endpoint = new Uri("http://localhost:11434/v1");
    var credential = new ApiKeyCredential("ollama"); // Dummy key for local Ollama
    return new OpenAIClient(credential, new OpenAIClientOptions { Endpoint = endpoint })
        .GetChatClient("phi4")
        .AsIChatClient();
 
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

app.MapControllers();

app.Run();