using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using OpenTelemetry.Trace;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using System.Collections.Generic;
using System.Linq;

namespace GamingCafe.IntegrationTests;

public class ObservabilityTests : IClassFixture<WebApplicationFactory<global::GamingCafe.API.Program>>
{
    private readonly WebApplicationFactory<global::GamingCafe.API.Program> _factory;

    public ObservabilityTests(WebApplicationFactory<global::GamingCafe.API.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CriticalFlow_EmitsSpans_InMemory()
    {
        // Arrange
        var exported = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("Microsoft.AspNetCore")
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("GamingCafe.Test"))
            .AddInMemoryExporter(exported)
            .Build();

        var client = _factory.CreateClient();

        // Act: hit a known critical endpoint (swagger used as smoke); instrumentation should create spans
        var resp = await client.GetAsync("/swagger/v1/swagger.json");
        resp.EnsureSuccessStatusCode();

        // Allow some time for background instrumentation to flush
        await Task.Delay(250);

        // Assert at least one span recorded
        exported.Count.Should().BeGreaterThan(0, "expected at least one span to be emitted during the request flow");
    }
}
