namespace MiniAts.Api.Dtos;

public record CvJobMatchResponse(Guid JobId, string JobTitle, int Score, string Reason);

public record CvAnalysisResponse(
    string? Name,
    string? Email,
    string? Phone,
    string? LinkedInUrl,
    IReadOnlyList<CvJobMatchResponse> Matches);
