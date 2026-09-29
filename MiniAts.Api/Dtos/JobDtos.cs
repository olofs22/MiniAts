namespace MiniAts.Api.Dtos;

public record JobResponse(Guid Id, Guid OrgId, string Title, string? Description, string Status, DateTimeOffset CreatedAt);

public record CreateJobRequest(string Title, string? Description, string? Status);

public record UpdateJobRequest(string Title, string? Description, string Status);
