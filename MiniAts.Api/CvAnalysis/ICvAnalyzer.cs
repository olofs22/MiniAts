namespace MiniAts.Api.CvAnalysis;

public interface ICvAnalyzer
{
    Task<CvAnalysisResult> AnalyzeAsync(byte[] pdf, IReadOnlyList<CvJobOption> openJobs, CancellationToken ct = default);
}

public record CvJobOption(Guid Id, string Title, string? Description);

public record CvJobMatch(Guid JobId, int Score, string Reason);

public record CvAnalysisResult(
    string? Name,
    string? Email,
    string? Phone,
    string? LinkedInUrl,
    IReadOnlyList<CvJobMatch> Matches);

public class CvAnalysisNotConfiguredException() : Exception("CV analysis is not configured (Anthropic:ApiKey is missing).");

public class CvAnalysisFailedException(string message) : Exception(message);
