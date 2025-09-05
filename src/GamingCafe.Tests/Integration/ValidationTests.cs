using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using GamingCafe.API;
using GamingCafe.Core.DTOs;

namespace GamingCafe.Tests.Integration;

public class ValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ValidationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InvalidLoginReturnsProblemDetails()
    {
        var client = _factory.CreateClient();
        var req = new LoginRequest { Email = "", Password = "" }; // invalid per validators
        var res = await client.PostAsJsonAsync("/api/auth/login", req);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);

        var body = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(body.TryGetProperty("title", out var title));
        Assert.Contains("validation errors", title.GetString() ?? string.Empty, System.StringComparison.OrdinalIgnoreCase);
    }
}
