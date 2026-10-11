namespace HealthcareAiApi.Models;

public record ClinicalSummaryResult(
    string Topic,
    string Summary,
    string[] KeyActionItems,
    string ComplianceRiskLevel
);