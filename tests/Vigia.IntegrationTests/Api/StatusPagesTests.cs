using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Application.Health;
using Vigia.Application.Workers;
using Vigia.Domain.Health;
using Vigia.Infrastructure.Persistence;
using Vigia.IntegrationTests.Support;
using Vigia.Plugins;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Curated status pages: configuration, the public view and daily availability.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class StatusPagesTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Public_page_shows_only_public_names_and_states()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (booking, bookingCheck) = await CreateServiceWithCheckAsync(client);
        var (sabre, sabreCheck) = await CreateServiceWithCheckAsync(client, external: true);
        var page = Unique("page");
        (await client.PostAsJsonAsync("/api/v1/status-pages", new
        {
            slug = page,
            title = "Flystern status",
            description = "Live status",
            components = new object[]
            {
                new { service = booking, name = "Bookings", group = "Core" },
                new { service = sabre, name = "Flight search provider", group = "Providers" },
            },
        }, Ct)).EnsureSuccessStatusCode();

        await PushAsync(sabreCheck, Outcome.Down);
        var response = await factory.CreateClient().GetAsync($"/status/{page}", Ct);
        var raw = await response.Content.ReadAsStringAsync(Ct);
        var status = JsonDocument.Parse(raw).RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Flystern status", status.GetProperty("title").GetString());
        Assert.Equal("down", status.GetProperty("state").GetString());
        var components = status.GetProperty("components").EnumerateArray().ToList();
        Assert.Equal(["Bookings", "Flight search provider"], components.Select(c => c.GetProperty("name").GetString()));
        Assert.Equal("operational", components[0].GetProperty("state").GetString());
        Assert.Equal("down", components[1].GetProperty("state").GetString());
        Assert.Equal("Providers", components[1].GetProperty("group").GetString());
        Assert.Equal(90, components[1].GetProperty("history").GetArrayLength());

        // Nothing internal leaks: no service or check slugs, no reasons.
        Assert.DoesNotContain(booking, raw);
        Assert.DoesNotContain(sabre, raw);
        Assert.DoesNotContain(sabreCheck, raw);
        Assert.DoesNotContain(bookingCheck, raw);
        Assert.DoesNotContain("connection refused", raw);
    }

    [Fact]
    public async Task Today_reflects_an_outage_in_the_history()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (service, check) = await CreateServiceWithCheckAsync(client);
        var page = await CreatePageAsync(client, service);

        await PushAsync(check, Outcome.Down);
        var today = (await factory.CreateClient().GetFromJsonAsync<JsonElement>($"/status/{page}?days=7", Ct))
            .GetProperty("components")[0].GetProperty("history").EnumerateArray().Last();

        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"), today.GetProperty("date").GetString());
        Assert.Equal("down", today.GetProperty("state").GetString());
        Assert.True(today.GetProperty("uptime").GetDouble() < 1);
    }

    [Fact]
    public async Task Configuration_round_trips_and_is_validated()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (service, _) = await CreateServiceWithCheckAsync(client);
        var page = await CreatePageAsync(client, service);

        var unknown = await client.PutAsJsonAsync($"/api/v1/status-pages/{page}", new { components = new[] { new { service = Unique("ghost"), name = "X" } } }, Ct);
        var twice = await client.PutAsJsonAsync($"/api/v1/status-pages/{page}", new { components = new[] { new { service, name = "A" }, new { service, name = "B" } } }, Ct);
        var fetched = await client.GetFromJsonAsync<JsonElement>($"/api/v1/status-pages/{page}", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, twice.StatusCode);
        Assert.Equal(service, fetched.GetProperty("components")[0].GetProperty("service").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().GetAsync($"/status/{Unique("nope")}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await factory.CreateClient().GetAsync($"/status/{page}?days=365", Ct)).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_service_removes_it_from_pages()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (service, _) = await CreateServiceWithCheckAsync(client);
        var page = await CreatePageAsync(client, service);

        (await client.DeleteAsync($"/api/v1/services/{service}", Ct)).EnsureSuccessStatusCode();

        Assert.Empty((await client.GetFromJsonAsync<JsonElement>($"/api/v1/status-pages/{page}", Ct)).GetProperty("components").EnumerateArray());
    }

    [Fact]
    public void Daily_availability_weights_down_fully_partial_by_half_and_skips_unknown()
    {
        var now = new DateTimeOffset(2026, 1, 3, 12, 0, 0, TimeSpan.Zero);
        var id = Guid.NewGuid();
        ServiceHealthChange[] changes =
        [
            new(id, null, HealthState.Operational, "", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            new(id, HealthState.Operational, HealthState.Down, "", new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero)),
            new(id, HealthState.Down, HealthState.PartialOutage, "", new DateTimeOffset(2026, 1, 2, 6, 0, 0, TimeSpan.Zero)),
            new(id, HealthState.PartialOutage, HealthState.Operational, "", new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero)),
            new(id, HealthState.Operational, HealthState.Unknown, "", new DateTimeOffset(2026, 1, 3, 6, 0, 0, TimeSpan.Zero)),
        ];

        var days = AvailabilityHistory.Daily(changes, 4, now);

        Assert.Equal([null, HealthState.Operational, HealthState.Down, HealthState.Operational], days.Select(d => d.State));
        Assert.Null(days[0].Uptime);
        Assert.Equal(1, days[1].Uptime);
        Assert.Equal(1 - ((6 + 3) / 24.0), days[2].Uptime!.Value, precision: 6);
        Assert.Equal(1, days[3].Uptime);
    }

    private static string Unique(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}"[..16];
    }

    private static async Task<(string Service, string Check)> CreateServiceWithCheckAsync(HttpClient client, bool external = false)
    {
        var tag = Unique("sp");
        var check = Unique("spc");
        (await client.PostAsJsonAsync("/api/v1/checks", new { slug = check, plugin = "vigia.check.http", config = new { url = "http://127.0.0.1:1/" }, tags = new { service = tag }, workers = Placement.BuiltInOnly }, Ct)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/rules", new { slug = Unique("spr"), check, when = new { outcome = "down" }, severity = "critical" }, Ct)).EnsureSuccessStatusCode();
        var service = Unique("sps");
        var tags = external ? new Dictionary<string, string?> { ["vigia:external"] = null } : null;
        (await client.PostAsJsonAsync("/api/v1/services", new { slug = service, checks = new { service = tag }, tags }, Ct)).EnsureSuccessStatusCode();
        return (service, check);
    }

    private static async Task<string> CreatePageAsync(HttpClient client, string service)
    {
        var page = Unique("page");
        (await client.PostAsJsonAsync("/api/v1/status-pages", new { slug = page, components = new[] { new { service, name = "Public name" } } }, Ct)).EnsureSuccessStatusCode();
        return page;
    }

    private async Task PushAsync(string checkSlug, Outcome outcome)
    {
        Guid checkId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            checkId = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Checks.Where(c => c.Slug == checkSlug).Select(c => c.Id).SingleAsync(Ct);
        }

        var at = DateTimeOffset.UtcNow;
        await factory.Services.GetRequiredService<IResultSink>().WriteAsync(
            new ProbeRecord(Guid.CreateVersion7(at), checkId, "builtin", outcome, new Dictionary<string, double>(), "connection refused", 1, at), Ct);
    }
}
