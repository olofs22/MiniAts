namespace MiniAts.Api.Entities;

/// <summary>A company asking to get an organization on Mini-ATS, submitted from the public welcome page.</summary>
public class SignupRequest
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = null!;
    public string ContactName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
