namespace MiniAts.Api.Dtos;

public record CreateOrganizationRequest(string Name);

public record UpdateOrganizationRequest(string Name);

public record OrganizationResponse(Guid Id, string Name, DateTimeOffset CreatedAt);

public record CreateUserRequest(string Email, Guid? OrgId, string Role);

public record UserResponse(Guid UserId, string Email, Guid? OrgId, string Role, DateTimeOffset CreatedAt);

public record CreateUserPartialFailureResponse(
    Guid SupabaseUserId, string Email, Guid? OrgId, string Role, string Message);
