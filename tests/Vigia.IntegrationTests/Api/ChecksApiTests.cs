using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Infrastructure.Persistence;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Check endpoints through HTTP, Identity auth, Mediator, EF Core and a real Postgres.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ChecksApiTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Requires_authentication()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/checks", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_stores_normalized_config_in_postgres()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = NewSlug();

        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug,
            plugin = "vigia.check.http",
            config = new { url = "http://127.0.0.1:1/health" },
            interval = "30s",
            tags = new { service = "booking-api" },
        }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/v1/checks/{slug}", response.Headers.Location?.AbsolutePath);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var check = await db.Checks.SingleAsync(c => c.Slug == slug, Ct);
        var config = JsonDocument.Parse(check.ConfigJson).RootElement;

        Assert.Equal("1.0.0", check.PluginVersion);
        Assert.Equal("GET", config.GetProperty("method").GetString());
        Assert.Equal("10s", config.GetProperty("timeout").GetString());
        Assert.Equal(TimeSpan.FromSeconds(30), check.Interval);
        Assert.Equal("booking-api", check.Tags["service"]);
        Assert.Equal("vigia.check.http", check.Tags["vigia:plugin"]);
        Assert.NotEqual(default, check.CreatedAt);
    }

    [Fact]
    public async Task Get_and_list_return_the_check()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = NewSlug();
        await CreateAsync(client, slug, "http://127.0.0.1:1/");

        var check = await client.GetFromJsonAsync<JsonElement>($"/api/v1/checks/{slug}", Ct);
        var all = await client.GetFromJsonAsync<JsonElement[]>("/api/v1/checks", Ct);

        Assert.Equal("vigia.check.http", check.GetProperty("plugin").GetString());
        Assert.Equal("1m", check.GetProperty("interval").GetString());
        Assert.Equal("ui", check.GetProperty("managedBy").GetString());
        Assert.Contains(all!, c => c.GetProperty("slug").GetString() == slug);
    }

    [Fact]
    public async Task Invalid_config_returns_field_errors()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);

        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug = NewSlug(),
            plugin = "vigia.check.http",
            config = new { method = "GET" },
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("config.url", out _));
    }

    [Fact]
    public async Task Unknown_plugin_is_rejected()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);

        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug = NewSlug(),
            plugin = "vigia.check.nope",
            config = new { },
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("plugin", out _));
    }

    [Fact]
    public async Task Duplicate_slug_returns_conflict()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = NewSlug();
        await CreateAsync(client, slug, "http://127.0.0.1:1/");

        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug,
            plugin = "vigia.check.http",
            config = new { url = "http://127.0.0.1:1/" },
        }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Probe_runs_the_plugin_against_a_real_server()
    {
        await using var target = await LocalHttpServer.StartAsync(Ct);
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var up = NewSlug();
        var down = NewSlug();
        await CreateAsync(client, up, $"{target.BaseUrl}/ok");
        await CreateAsync(client, down, $"{target.BaseUrl}/fail");

        var upResult = await ProbeAsync(client, up);
        var downResult = await ProbeAsync(client, down);

        Assert.Equal("up", upResult.GetProperty("outcome").GetString());
        Assert.Equal(200, upResult.GetProperty("measurements").GetProperty("status-code").GetDouble());
        Assert.Equal("down", downResult.GetProperty("outcome").GetString());
    }

    [Fact]
    public async Task Validate_returns_normalized_config_without_saving()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);

        var response = await client.PostAsJsonAsync("/api/v1/checks/validate", new
        {
            plugin = "vigia.check.http",
            config = new { url = "http://127.0.0.1:1/" },
        }, Ct);

        response.EnsureSuccessStatusCode();
        var normalized = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("GET", normalized.GetProperty("method").GetString());
    }

    [Fact]
    public async Task Plugins_endpoint_exposes_schema()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);

        var plugins = await client.GetFromJsonAsync<JsonElement[]>("/api/v1/plugins", Ct);

        var http = Assert.Single(plugins!, p => p.GetProperty("id").GetString() == "vigia.check.http");
        var fields = http.GetProperty("schema").GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("name").GetString());
        Assert.Contains("url", fields);
    }

    [Fact]
    public async Task Update_replaces_settings_and_can_disable()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = NewSlug();
        await CreateAsync(client, slug, "http://127.0.0.1:1/a");

        var response = await client.PutAsJsonAsync($"/api/v1/checks/{slug}", new
        {
            name = "Renamed",
            config = new { url = "http://127.0.0.1:1/b", method = "HEAD" },
            interval = "5m",
            tags = new { env = "prod" },
            enabled = false,
        }, Ct);

        response.EnsureSuccessStatusCode();
        var check = await client.GetFromJsonAsync<JsonElement>($"/api/v1/checks/{slug}", Ct);
        Assert.Equal("Renamed", check.GetProperty("name").GetString());
        Assert.Equal("http://127.0.0.1:1/b", check.GetProperty("config").GetProperty("url").GetString());
        Assert.Equal("HEAD", check.GetProperty("config").GetProperty("method").GetString());
        Assert.Equal("5m", check.GetProperty("interval").GetString());
        Assert.Equal("prod", check.GetProperty("tags").GetProperty("env").GetString());
        Assert.False(check.GetProperty("enabled").GetBoolean());
    }

    [Fact]
    public async Task Update_cannot_change_plugin()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = NewSlug();
        await CreateAsync(client, slug, "http://127.0.0.1:1/");

        var response = await client.PutAsJsonAsync($"/api/v1/checks/{slug}", new
        {
            plugin = "vigia.check.dns",
            config = new { url = "http://127.0.0.1:1/" },
        }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_the_check()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = NewSlug();
        await CreateAsync(client, slug, "http://127.0.0.1:1/");

        var delete = await client.DeleteAsync($"/api/v1/checks/{slug}", Ct);
        var get = await client.GetAsync($"/api/v1/checks/{slug}", Ct);
        var again = await client.DeleteAsync($"/api/v1/checks/{slug}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Rollups_endpoint_validates_range()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var slug = NewSlug();
        await CreateAsync(client, slug, "http://127.0.0.1:1/");

        var ok = await client.GetAsync($"/api/v1/checks/{slug}/rollups", Ct);
        var inverted = await client.GetAsync($"/api/v1/checks/{slug}/rollups?from=2026-02-01T00:00:00Z&to=2026-01-01T00:00:00Z", Ct);
        var tooWide = await client.GetAsync($"/api/v1/checks/{slug}/rollups?from=2025-01-01T00:00:00Z&to=2026-01-01T00:00:00Z", Ct);

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, inverted.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooWide.StatusCode);
    }

    private static string NewSlug()
    {
        return $"check-{Guid.NewGuid():N}"[..20];
    }

    private static async Task CreateAsync(HttpClient client, string slug, string url)
    {
        var response = await client.PostAsJsonAsync("/api/v1/checks", new
        {
            slug,
            plugin = "vigia.check.http",
            config = new { url },
        }, Ct);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> ProbeAsync(HttpClient client, string slug)
    {
        var response = await client.PostAsync($"/api/v1/checks/{slug}/probe", null, Ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }
}
