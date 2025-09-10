using System.Linq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Data.Services;

namespace GamingCafe.UnitTests;

public class TwoFactorBackupCodesTests
{
    private GamingCafe.Data.GamingCafeContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<GamingCafe.Data.GamingCafeContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        return new GamingCafe.Data.GamingCafeContext(options);
    }

    [Fact]
    public void GenerateBackupCodes_FormatIsCorrect()
    {
        using var ctx = CreateInMemoryContext();
        var logger = new NullLogger<TwoFactorService>();
        var provider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("GamingCafe.Tests");
        var svc = new TwoFactorService(ctx, logger, provider);

        var codes = svc.GenerateBackupCodes(5);
        codes.Should().HaveCount(5);
        foreach (var c in codes)
        {
            c.Should().MatchRegex("^[A-Z0-9]{4}-[A-Z0-9]{4}$");
        }
    }
}
