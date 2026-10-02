using Microsoft.EntityFrameworkCore;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Data;

public class MiniAtsDbContext(DbContextOptions<MiniAtsDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<SignupRequest> SignupRequests => Set<SignupRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MiniAtsDbContext).Assembly);
}
