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
    public async System.Threading.Tasks.Task GenerateBackupCodes_FormatIsCorrect()
    {
        using var ctx = CreateInMemoryContext();
        var logger = new NullLogger<TwoFactorService>();
        var provider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("GamingCafe.Tests");
        var svc = new TwoFactorService(ctx, logger, provider);

        var resp = svc.GenerateNewRecoveryCodesAsync(1);
        // no user exists - method will throw ArgumentException; ensure handled via try-catch and then create user
    try { await resp; } catch { }

        var user = new GamingCafe.Core.Models.User { UserId = 1, Username = "u1", Email = "u1@example.com", PasswordHash = "x" };
        ctx.Users.Add(user);
        ctx.SaveChanges();

    var result = await svc.GenerateNewRecoveryCodesAsync(1);
        result.RecoveryCodes.Should().HaveCount(10);
        foreach (var c in result.RecoveryCodes)
        {
            c.Should().MatchRegex("^[A-Z0-9]{4}-[A-Z0-9]{4}$");
        }
    }
}
