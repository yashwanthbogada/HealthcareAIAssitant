using System.ComponentModel;
using Microsoft.Extensions.AI;
using HealthcareAiApi.Middleware;

namespace HealthcareAiApi.Mcp;

public class HospitalMcpTools
{
    private readonly KnowledgeRetrievalService _knowledgeService;

    public HospitalMcpTools(KnowledgeRetrievalService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    [Description("Searches internal hospital guidelines for protocols such as sepsis, pneumonia, and CIDP.")]
    public async Task<string> SearchHospitalGuidelinesAsync(string query)
    {
        var results = await _knowledgeService.SearchRelevantContextAsync(query);
        return results;
    }
}