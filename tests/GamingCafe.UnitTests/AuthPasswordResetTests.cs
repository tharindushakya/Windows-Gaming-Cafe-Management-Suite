using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GamingCafe.Core.Models;
using GamingCafe.API.Services;
using GamingCafe.Data;

namespace GamingCafe.UnitTests
{
    public class AuthPasswordResetTests
    {
        private GamingCafeContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<GamingCafeContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new GamingCafeContext(options);
        }

        private IConfigurationRoot CreateConfiguration()
        {
            var dict = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Jwt:Key", "test-jwt-key-which-is-long-enough" },
                { "RefreshToken:HashKey", "refresh-key" },
                { "Auth:TwoFactor:TransientTtlMinutes", "1" }
            };

            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        [Fact]
        public async Task InitiatePasswordReset_ReturnsFalse_ForUnknownEmail()
        {
            var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var config = CreateConfiguration();
            var services = new ServiceCollection().BuildServiceProvider();
            var cache = new MemoryCache(new MemoryCacheOptions());

            var authService = new AuthService(ctx, config, services, cache);

            var result = await authService.InitiatePasswordResetAsync("noone@example.com");

            Assert.False(result);
        }

        [Fact]
        public async Task ResetPassword_Works_ForValidToken()
        {
            var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var config = CreateConfiguration();
            var services = new ServiceCollection().BuildServiceProvider();
            var cache = new MemoryCache(new MemoryCacheOptions());

            var user = new User
            {
                Username = "resetuser",
                Email = "reset@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass"),
                IsActive = true
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var authService = new AuthService(ctx, config, services, cache);

            var initiated = await authService.InitiatePasswordResetAsync(user.Email);
            Assert.True(initiated);

            // Reload user to get token
            var persisted = await ctx.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            Assert.NotNull(persisted.PasswordResetToken);

            var request = new GamingCafe.Core.DTOs.PasswordResetConfirmRequest
            {
                Email = user.Email,
                Token = persisted.PasswordResetToken!,
                NewPassword = "NewP@ssw0rd1"
            };

            var reset = await authService.ResetPasswordAsync(request);
            Assert.True(reset);

            var updated = await ctx.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            Assert.NotNull(updated);
            Assert.NotEqual(persisted.PasswordHash, updated.PasswordHash);
        }
    }
}
