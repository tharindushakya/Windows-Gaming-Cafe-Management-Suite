using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using GamingCafe.Admin.Components.Layout;
using GamingCafe.Admin.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GamingCafe.UI.Tests
{
    public class NavMenuTests : TestContext
    {
        [Fact]
        public void Shows_Login_When_NotAuthenticated()
        {
            // Arrange: create empty HttpContext (not authenticated)
            var httpContext = new DefaultHttpContext();
            var accessor = new HttpContextAccessor { HttpContext = httpContext };

            Services.AddSingleton<IHttpContextAccessor>(accessor);
            Services.AddSingleton<AdminAuthService>();

            // Act
            var cut = RenderComponent<NavMenu>();

            // Assert
            Assert.Contains("Login", cut.Markup);
            Assert.Contains("Gaming Café Admin", cut.Markup);
        }

        [Fact]
        public void Shows_Protected_Links_When_Authenticated()
        {
            // Arrange: create HttpContext with authenticated user
            var claims = new[] { new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);
            var httpContext = new DefaultHttpContext { User = principal };
            var accessor = new HttpContextAccessor { HttpContext = httpContext };

            Services.AddSingleton<IHttpContextAccessor>(accessor);
            Services.AddSingleton<AdminAuthService>();

            var cut = RenderComponent<NavMenu>();

            Assert.Contains("Users", cut.Markup);
            Assert.Contains("Logout", cut.Markup);
        }
    }
}
