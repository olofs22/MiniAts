using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Data.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(JobStatus.Open)
            .IsRequired();

        builder.Property(j => j.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasIndex(j => j.OrgId);

        builder.HasOne(j => j.Organization)
            .WithMany(o => o.Jobs)
            .HasForeignKey(j => j.OrgId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
