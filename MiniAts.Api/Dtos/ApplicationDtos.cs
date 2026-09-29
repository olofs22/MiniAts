namespace MiniAts.Api.Dtos;

public record ApplicationResponse(
    Guid Id,
    Guid OrgId,
    Guid CandidateId,
    Guid JobId,
    string Stage,
    double Position,
    DateTimeOffset CreatedAt);

public record CreateApplicationRequest(Guid CandidateId, Guid JobId);

public record UpdateApplicationRequest(string Stage, double Position);
