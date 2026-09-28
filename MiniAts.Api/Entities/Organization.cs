namespace MiniAts.Api.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Profile> Profiles { get; set; } = new List<Profile>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
}
