namespace MiniAts.Api.Entities;

public class Application
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public Guid OrgId { get; set; }
    public ApplicationStage Stage { get; set; } = ApplicationStage.New;
    public double Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Candidate Candidate { get; set; } = null!;
    public Job Job { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
