namespace MiniAts.Api.Auth;

/// <summary>Thrown when a caller's claims don't resolve to an organization they're allowed to act on.</summary>
public class OrgAccessDeniedException(string message) : Exception(message);
