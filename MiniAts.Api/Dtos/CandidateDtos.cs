namespace MiniAts.Api.Dtos;

public record CandidateResponse(
    Guid Id,
    Guid OrgId,
    string Name,
    string? Email,
    string? Phone,
    string? LinkedInUrl,
    string? Notes,
    DateTimeOffset CreatedAt);

public record CreateCandidateRequest(string Name, string? Email, string? Phone, string? LinkedInUrl, string? Notes);

public record UpdateCandidateRequest(string Name, string? Email, string? Phone, string? LinkedInUrl, string? Notes);
