using System.IO;
using System.Linq;
namespace HealthcareAiApi.Middleware;
public class KnowledgeRetrievalService
{
    private readonly string _knowledgeBasePath;

    public KnowledgeRetrievalService(IWebHostEnvironment env)
    {
        _knowledgeBasePath = Path.Combine(env.ContentRootPath, "KnowledgeBase");
    }

    public string SearchRelevantContext(string userQuery)
    {
        if (!Directory.Exists(_knowledgeBasePath)) return string.Empty;

        var policyFiles = Directory.GetFiles(_knowledgeBasePath, "*.txt");
        var relevantSnippets = new System.Collections.Generic.List<string>();

        foreach (var file in policyFiles)
        {
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                // Simple keyword matching (can be upgraded to embeddings/vector search later)
                if (userQuery.Split(' ').Any(word => word.Length > 3 && line.Contains(word, StringComparison.OrdinalIgnoreCase)))
                {
                    relevantSnippets.Add(line);
                }
            }
        }

        return relevantSnippets.Count > 0 
            ? string.Join("\n", relevantSnippets.Distinct()) 
            : "No specific internal hospital policy found for this query.";
    }
}