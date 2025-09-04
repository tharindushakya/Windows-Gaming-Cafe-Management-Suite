using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GamingCafe.Data;
using GamingCafe.Data.Services;
using GamingCafe.Core.Models;

namespace GamingCafe.Tests;

public class TwoFactorTests
{
    [Fact]
    public async Task SetupConfirmVerifyFlow_Works()
    {
        var options = new DbContextOptionsBuilder<GamingCafeContext>()
            .UseInMemoryDatabase("test-db-2fa")
            .Options;

        await using var context = new GamingCafeContext(options);
        // seed a user
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

    var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    services.AddDataProtection();
    var sp = services.BuildServiceProvider();
    var provider = sp.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
    var loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
    var typedLogger = loggerFactory.CreateLogger<GamingCafe.Data.Services.TwoFactorService>();
    var twoFactorService = new TwoFactorService(context, typedLogger, provider);

        var setup = await twoFactorService.SetupTwoFactorAsync(user.UserId, "P@ssw0rd!");
        Assert.False(string.IsNullOrEmpty(setup.SecretKey));
        Assert.False(string.IsNullOrEmpty(setup.QrCodeDataUrl));
        Assert.NotEmpty(setup.RecoveryCodes);

        // Confirm setup using a generated code from the secret
        var secret = setup.SecretKey;
        var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret));
        var code = totp.ComputeTotp();

        var confirmed = await twoFactorService.ConfirmSetupAsync(user.UserId, code);
        Assert.True(confirmed);

        // Now verify using VerifyTwoFactorAsync
        var verified = await twoFactorService.VerifyTwoFactorAsync(user.UserId, code);
        Assert.True(verified);
    }

    [Fact]
    public async Task RecoveryCode_ConsumeAndRegenerate_Works()
    {
        var options = new DbContextOptionsBuilder<GamingCafeContext>()
            .UseInMemoryDatabase("test-db-2fa-codes")
            .Options;

        await using var context = new GamingCafeContext(options);
        var user = new User
        {
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddDataProtection();
        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
        var loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();
        var typedLogger = loggerFactory.CreateLogger<GamingCafe.Data.Services.TwoFactorService>();
        var twoFactorService = new TwoFactorService(context, typedLogger, provider);

        // Setup to generate recovery codes
        var setup = await twoFactorService.SetupTwoFactorAsync(user.UserId, "P@ssw0rd!");
        Assert.NotEmpty(setup.RecoveryCodes);

        var firstCode = setup.RecoveryCodes[0];

        // Consume the recovery code
        var consumed = await twoFactorService.VerifyRecoveryCodeAsync(user.UserId, firstCode);
        Assert.True(consumed);

        // Ensure it's no longer valid
        var consumedAgain = await twoFactorService.VerifyRecoveryCodeAsync(user.UserId, firstCode);
        Assert.False(consumedAgain);

        // Generate new codes
        var regen = await twoFactorService.GenerateNewRecoveryCodesAsync(user.UserId);
        Assert.NotEmpty(regen.RecoveryCodes);

        // Old code should not work
        var oldCodeValid = await twoFactorService.VerifyRecoveryCodeAsync(user.UserId, firstCode);
        Assert.False(oldCodeValid);

        // New code should work
        var newCode = regen.RecoveryCodes[0];
        var newValid = await twoFactorService.VerifyRecoveryCodeAsync(user.UserId, newCode);
        Assert.True(newValid);
    }
}
