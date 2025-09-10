using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using GamingCafe.Core.DTOs;

namespace GamingCafe.IntegrationTests;

public class RegisterLoginProfileTests : IClassFixture<WebApplicationFactory<global::GamingCafe.API.Program>>
{
    private readonly WebApplicationFactory<global::GamingCafe.API.Program> _factory;

    public RegisterLoginProfileTests(WebApplicationFactory<global::GamingCafe.API.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_Then_Login_Then_GetProfile()
    {
        var client = _factory.CreateClient();

        var registerReq = new RegisterRequest
        {
            Username = "testuser123",
            Email = "testuser123@example.com",
            Password = "Pa$$w0rd!",
            FirstName = "Test",
            LastName = "User"
        };

        var regResp = await client.PostAsJsonAsync("/api/v1/auth/register", registerReq);
        regResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var loginReq = new LoginRequest { Email = registerReq.Email, Password = registerReq.Password };
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        loginResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var loginBody = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        loginBody.Should().NotBeNull();
        loginBody!.AccessToken.Should().NotBeNullOrEmpty();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.AccessToken);
        var profileResp = await client.GetAsync("/api/v1/auth/profile");
        profileResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
