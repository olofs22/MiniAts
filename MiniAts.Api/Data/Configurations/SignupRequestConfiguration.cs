using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniAts.Api.Entities;

namespace MiniAts.Api.Data.Configurations;

public class SignupRequestConfiguration : IEntityTypeConfiguration<SignupRequest>
{
    public const int CompanyNameMaxLength = 200;
    public const int ContactNameMaxLength = 200;
    public const int EmailMaxLength = 320;
    public const int MessageMaxLength = 2000;

    public void Configure(EntityTypeBuilder<SignupRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.CompanyName).IsRequired().HasMaxLength(CompanyNameMaxLength);
        builder.Property(r => r.ContactName).IsRequired().HasMaxLength(ContactNameMaxLength);
        builder.Property(r => r.Email).IsRequired().HasMaxLength(EmailMaxLength);
        builder.Property(r => r.Message).HasMaxLength(MessageMaxLength);

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
    }
}
