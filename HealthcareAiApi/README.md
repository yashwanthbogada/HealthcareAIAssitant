Healthcare AI Assistant Microservice (.NET 8 + Ollama)
A production-grade, local-first enterprise AI backend service built with .NET 8 and Microsoft.Extensions.AI, integrating natively with local open-source models via Ollama.

🚀 Key Features
Local LLM Integration: Powered by local models (phi4-mini / phi4) via Ollama for zero data-leakage and secure clinical operations context.

Enterprise Telemetry Middleware: Custom decorator built on DelegatingChatClient to track execution latency, token-ready states, and lifecycle request logging.

Request Validation: Robust Data Annotations safeguarding incoming payloads against malformed or empty requests (400 Bad Request).

Real-Time SSE Streaming: Supports token-by-token streaming responses using Server-Sent Events (IAsyncEnumerable<string>).

Strict Structured Outputs: Forces the local LLM to conform to a strongly-typed C# schema (ClinicalSummaryResult), enabling seamless machine-to-machine integration.

🛠️ Tech Stack
Framework: .NET 8 (Web API)

AI Abstraction Layer: Microsoft.Extensions.AI, Microsoft.Extensions.AI.OpenAI

Local Model Engine: Ollama (phi4-mini)

Documentation: OpenAPI / Swagger UI

⚙️ Getting Started & Prerequisites
Install and Run Ollama:
Make sure Ollama is installed on your machine and running locally:

Bash
ollama run phi4-mini
Clone the Repository:

Bash
git clone https://github.com/yashwanthbogada/HealthcareAIAssitant.git
cd HealthcareAIAssitant/HealthcareAiApi
Run the API:

Bash
dotnet run
Explore the API:
Navigate to http://localhost:5047/swagger in your browser to interact with the endpoints.

📡 API Endpoints
POST /api/ClinicalAssistant/query — Standard text response endpoint.

POST /api/ClinicalAssistant/stream — Real-time Server-Sent Events streaming endpoint.

POST /api/ClinicalAssistant/structured — Strict JSON schema response mapping to C# records.