namespace MiniAts.Api.Dtos;

/// <param name="Website">Honeypot: hidden in the form, so only bots fill it in.</param>
public record CreateSignupRequest(
    string? CompanyName, string? ContactName, string? Email, string? Message, string? Website);

public record SignupRequestResponse(
    Guid Id, string CompanyName, string ContactName, string Email, string? Message, DateTimeOffset CreatedAt);
