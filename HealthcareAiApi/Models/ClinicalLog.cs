namespace HealthcareAiApi.Models
{
    public class ClinicalLog
    {
        public int Id { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string ResponseContent { get; set; } = string.Empty;
        public string EndpointType { get; set; } = string.Empty; // "structured", "query", or "stream"
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}