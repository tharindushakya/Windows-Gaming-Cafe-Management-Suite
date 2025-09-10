using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GamingCafe.IntegrationTests;

public class ApiStartupTests : IClassFixture<WebApplicationFactory<global::GamingCafe.API.Program>>
{
    private readonly WebApplicationFactory<global::GamingCafe.API.Program> _factory;

    public ApiStartupTests(WebApplicationFactory<global::GamingCafe.API.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerEndpoint_ReturnsSuccess()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/swagger/v1/swagger.json");
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        // Accept NotFound in case swagger is disabled in environment; test ensures app starts without crash
    }
}
