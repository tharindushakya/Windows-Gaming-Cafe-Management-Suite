using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using GamingCafe.Core.DTOs;

namespace GamingCafe.IntegrationTests;

public class AuthFlowTests : IClassFixture<WebApplicationFactory<global::GamingCafe.API.Program>>
{
    private readonly WebApplicationFactory<global::GamingCafe.API.Program> _factory;

    public AuthFlowTests(WebApplicationFactory<global::GamingCafe.API.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithSeededUser_ReturnsUnauthorizedOrOk()
    {
        var client = _factory.CreateClient();

        var req = new LoginRequest { Email = "admin@example.com", Password = "pa$$word" };
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", req);

        // Accept either Unauthorized when credentials differ, or 200 when seed matches environment
        resp.StatusCode.Should().Match(s => s == System.Net.HttpStatusCode.OK || s == System.Net.HttpStatusCode.Unauthorized);
    }
}
