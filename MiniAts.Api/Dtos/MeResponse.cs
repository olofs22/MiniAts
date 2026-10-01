namespace MiniAts.Api.Dtos;

public record MeResponse(Guid UserId, string Email, string Role, Guid? OrgId, string? OrgName);
