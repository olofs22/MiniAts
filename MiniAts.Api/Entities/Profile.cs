namespace MiniAts.Api.Entities;

public class Profile
{
    public Guid UserId { get; set; }
    public Guid? OrgId { get; set; }
    public ProfileRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Organization? Organization { get; set; }
}
