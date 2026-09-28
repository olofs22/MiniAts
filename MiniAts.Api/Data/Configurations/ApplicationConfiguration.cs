using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Data.Configurations;

public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.Stage)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ApplicationStage.New)
            .IsRequired();

        builder.Property(a => a.Position).IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasIndex(a => a.OrgId);
        builder.HasIndex(a => new { a.JobId, a.Stage, a.Position });

        builder.HasOne(a => a.Candidate)
            .WithMany(c => c.Applications)
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Job)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Organization)
            .WithMany()
            .HasForeignKey(a => a.OrgId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
