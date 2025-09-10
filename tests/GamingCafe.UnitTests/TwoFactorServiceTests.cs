using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using GamingCafe.Data.Services;
using GamingCafe.Core.Models;
using System.Linq;

namespace GamingCafe.UnitTests;

public class TwoFactorServiceTests
{
    private GamingCafe.Data.GamingCafeContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<GamingCafe.Data.GamingCafeContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        return new GamingCafe.Data.GamingCafeContext(options);
    }

    [Fact]
    public async Task GenerateNewRecoveryCodes_Then_VerifyOne_IsConsumed()
    {
        // Arrange
    using var ctx = CreateInMemoryContext();
    var user = new GamingCafe.Core.Models.User { UserId = 1, Email = "test@example.com", Username = "testuser", PasswordHash = "x" };
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        var logger = new NullLogger<TwoFactorService>();
    var provider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("GamingCafe.Tests");
        var svc = new TwoFactorService(ctx, logger, provider);

        // Act: generate new recovery codes
        var resp = await svc.GenerateNewRecoveryCodesAsync(1);
        resp.Should().NotBeNull();
        resp.RecoveryCodes.Should().NotBeEmpty();

        var first = resp.RecoveryCodes.First();

        // Verify recovery code should succeed and consume the code
        var ok = await svc.VerifyRecoveryCodeAsync(1, first);
        ok.Should().BeTrue();

        // A second attempt with the same code should fail
        var ok2 = await svc.VerifyRecoveryCodeAsync(1, first);
        ok2.Should().BeFalse();
    }

    [Fact]
    public void GenerateQrCodeDataUrl_ReturnsDataUri()
    {
    using var ctx = CreateInMemoryContext();
    var logger = new NullLogger<TwoFactorService>();
    var provider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("GamingCafe.Tests");
    var svc = new TwoFactorService(ctx, logger, provider);

        var url = svc.GenerateQrCodeDataUrl("user@example.com", "SECRETKEY123");
        url.Should().StartWith("data:image/png;base64,");
    }
}
