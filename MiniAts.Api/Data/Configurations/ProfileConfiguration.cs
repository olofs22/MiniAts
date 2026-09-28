using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Data.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.HasKey(p => p.UserId);

        builder.Property(p => p.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasIndex(p => p.OrgId);

        builder.HasOne(p => p.Organization)
            .WithMany(o => o.Profiles)
            .HasForeignKey(p => p.OrgId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
