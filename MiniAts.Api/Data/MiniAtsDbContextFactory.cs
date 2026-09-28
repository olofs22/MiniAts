using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiniAts.Api.Data;

// Used only by the `dotnet ef` CLI to build the model at design time.
// The connection string below is never actually connected to - it exists
// so migrations can be generated before real Supabase credentials exist.
public class MiniAtsDbContextFactory : IDesignTimeDbContextFactory<MiniAtsDbContext>
{
    public MiniAtsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MiniAtsDbContext>();
        optionsBuilder
            .UseNpgsql("Host=localhost;Port=5432;Database=minats_design;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention();

        return new MiniAtsDbContext(optionsBuilder.Options);
    }
}
