using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;

namespace HealthcareAiApi.Middleware;

public class KnowledgeRetrievalService 
{
    private readonly string _knowledgeBasePath; 

    public KnowledgeRetrievalService(IWebHostEnvironment env) 
    {
        _knowledgeBasePath = Path.Combine(env.ContentRootPath, "KnowledgeBase"); 
    } 

    public async Task<string> SearchRelevantContextAsync(string userQuery) 
    {
        if (!Directory.Exists(_knowledgeBasePath)) 
            return string.Empty; 

        var policyFiles = Directory.GetFiles(_knowledgeBasePath, "*.txt"); 
        var relevantSnippets = new List<string>(); 

        foreach (var file in policyFiles) 
        {
            // Asynchronously read all lines without blocking the thread pool
            var lines = await File.ReadAllLinesAsync(file); 
            
            foreach (var line in lines) 
            {
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