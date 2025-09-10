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
    public class AuthServiceTests2
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
                { "Jwt:Issuer", "test-issuer" },
                { "Jwt:Audience", "test-audience" },
                { "RefreshToken:HashKey", "refresh-key" },
                { "Auth:TwoFactor:TransientTtlMinutes", "1" }
            };

            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        [Fact]
        public async Task RegisterAsync_CreatesUser_WhenUnique()
        {
            var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var config = CreateConfiguration();
            var services = new ServiceCollection().BuildServiceProvider();
            var cache = new MemoryCache(new MemoryCacheOptions());

            var authService = new AuthService(ctx, config, services, cache);

            var user = new User
            {
                Username = "testuser",
                Email = "testuser@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Customer
            };

            var result = await authService.RegisterAsync(user, "P@ssw0rd!");

            Assert.NotNull(result);
            Assert.True(result.UserId > 0);

            var persisted = await ctx.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
            Assert.NotNull(persisted);
            Assert.NotNull(persisted.PasswordHash);
        }

        [Fact]
        public async Task Authenticate_Legacy_ReturnsToken_OnValidCredentials()
        {
            var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());
            var config = CreateConfiguration();
            var services = new ServiceCollection().BuildServiceProvider();
            var cache = new MemoryCache(new MemoryCacheOptions());

            // Seed a user
            var password = "P@ssw0rd!";
            var user = new User
            {
                Username = "legacyuser",
                Email = "legacy@example.com",
                FirstName = "Legacy",
                LastName = "User",
                Role = UserRole.Customer,
                IsActive = true
            };
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var authService = new AuthService(ctx, config, services, cache);

            var token = await authService.AuthenticateAsync("legacyuser", password);

            Assert.NotNull(token);
            Assert.IsType<string>(token);
            Assert.True(token.Length > 10);
        }
    }
}
