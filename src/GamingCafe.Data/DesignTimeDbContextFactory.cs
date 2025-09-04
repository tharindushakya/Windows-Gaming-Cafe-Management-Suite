using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GamingCafe.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GamingCafeContext>
{
    public GamingCafeContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<GamingCafeContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            "Host=localhost;Database=GamingCafeDB;Username=gamingcafe;Password=cafe123";
        
        optionsBuilder.UseNpgsql(connectionString);

        return new GamingCafeContext(optionsBuilder.Options);
    }
}
