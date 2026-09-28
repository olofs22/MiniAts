namespace MiniAts.Api.Entities;

public class Job
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Open;
    public DateTimeOffset CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
