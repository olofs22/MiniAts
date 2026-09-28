using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Data.Configurations;

public class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Email).HasMaxLength(320);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.LinkedInUrl).HasMaxLength(500);

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasIndex(c => c.OrgId);

        builder.HasOne(c => c.Organization)
            .WithMany(o => o.Candidates)
            .HasForeignKey(c => c.OrgId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
