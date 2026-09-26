using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Infrastructure.Persistence;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Worker registration, enrollment, credentials and the built-in worker.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class WorkersApiTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Create_returns_a_one_time_token_and_derives_the_region_tag()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, token, created) = await CreateWorkerAsync(client, "eu-west", new Dictionary<string, string?> { ["network"] = "vpc" });

        Assert.StartsWith("vwe_", token);
        Assert.Equal("eu-west", created.GetProperty("tags").GetProperty("vigia:region").GetString());
        Assert.Equal("vpc", created.GetProperty("tags").GetProperty("network").GetString());
        Assert.False(created.GetProperty("enrolled").GetBoolean());

        var fetched = await client.GetFromJsonAsync<JsonElement>($"/api/v1/workers/{slug}", Ct);
        Assert.Equal(JsonValueKind.Null, fetched.GetProperty("enrollmentToken").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, fetched.GetProperty("enrollmentExpiresAt").ValueKind);
    }

    [Fact]
    public async Task Enrollment_token_works_once_and_yields_a_working_credential()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, token, _) = await CreateWorkerAsync(client);

        var credential = await EnrollAsync(token);
        var again = await factory.CreateClient().PostAsJsonAsync("/worker/v1/enroll", new { token }, Ct);

        Assert.StartsWith("vw_", credential);
        Assert.Equal(HttpStatusCode.Unauthorized, again.StatusCode);

        var heartbeat = await HeartbeatAsync(credential, "1.2.3");
        Assert.Equal(HttpStatusCode.OK, heartbeat.StatusCode);
        var self = await heartbeat.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(slug, self.GetProperty("slug").GetString());
        Assert.True(self.GetProperty("enrolled").GetBoolean());
        Assert.True(self.GetProperty("online").GetBoolean());
        Assert.Equal("1.2.3", self.GetProperty("version").GetString());
    }

    [Fact]
    public async Task Expired_enrollment_token_is_rejected()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, token, _) = await CreateWorkerAsync(client);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.ExecuteSqlRawAsync(
                "UPDATE workers SET enrollment_expires_at = {0} WHERE slug = {1}", [DateTimeOffset.UtcNow.AddMinutes(-1), slug], Ct);
        }

        var response = await factory.CreateClient().PostAsJsonAsync("/worker/v1/enroll", new { token }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Worker_and_user_credentials_do_not_cross()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (_, token, _) = await CreateWorkerAsync(client);
        var credential = await EnrollAsync(token);

        var userOnWorkerApi = await client.PostAsJsonAsync("/worker/v1/heartbeat", new { }, Ct);
        var anonymousOnWorkerApi = await factory.CreateClient().PostAsJsonAsync("/worker/v1/heartbeat", new { }, Ct);
        var workerOnUserApi = factory.CreateClient();
        workerOnUserApi.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Worker", credential);

        Assert.Equal(HttpStatusCode.Unauthorized, userOnWorkerApi.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousOnWorkerApi.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await workerOnUserApi.GetAsync("/api/v1/checks", Ct)).StatusCode);
    }

    [Fact]
    public async Task Revoking_stops_the_credential()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, token, _) = await CreateWorkerAsync(client);
        var credential = await EnrollAsync(token);

        (await client.PostAsync($"/api/v1/workers/{slug}/revoke", null, Ct)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await HeartbeatAsync(credential)).StatusCode);
        Assert.False((await client.GetFromJsonAsync<JsonElement>($"/api/v1/workers/{slug}", Ct)).GetProperty("enrolled").GetBoolean());
    }

    [Fact]
    public async Task Reenrolling_replaces_the_credential()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, token, _) = await CreateWorkerAsync(client);
        var oldCredential = await EnrollAsync(token);

        var reissued = await client.PostAsync($"/api/v1/workers/{slug}/enrollment-token", null, Ct);
        reissued.EnsureSuccessStatusCode();
        var newToken = (await reissued.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("enrollmentToken").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await HeartbeatAsync(oldCredential)).StatusCode);

        var newCredential = await EnrollAsync(newToken);

        Assert.Equal(HttpStatusCode.OK, (await HeartbeatAsync(newCredential)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await HeartbeatAsync(oldCredential)).StatusCode);
    }

    [Fact]
    public async Task Deleting_a_worker_stops_its_credential()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, token, _) = await CreateWorkerAsync(client);
        var credential = await EnrollAsync(token);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/workers/{slug}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await HeartbeatAsync(credential)).StatusCode);
    }

    [Fact]
    public async Task Update_changes_region_and_tags_and_rejects_reserved_tags()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, _, _) = await CreateWorkerAsync(client, "eu-west");

        var reserved = await client.PutAsJsonAsync($"/api/v1/workers/{slug}", new { tags = new Dictionary<string, string> { ["vigia:region"] = "us" } }, Ct);
        var update = await client.PutAsJsonAsync($"/api/v1/workers/{slug}", new { region = "us-east", tags = new { network = "dmz" } }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, reserved.StatusCode);
        update.EnsureSuccessStatusCode();
        var tags = (await update.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("tags");
        Assert.Equal("us-east", tags.GetProperty("vigia:region").GetString());
        Assert.Equal("dmz", tags.GetProperty("network").GetString());
    }

    [Fact]
    public async Task Duplicate_slug_conflicts()
    {
        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var (slug, _, _) = await CreateWorkerAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/workers", new { slug }, Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Built_in_worker_registers_itself_and_cannot_be_enrolled_or_deleted()
    {
        await using (var withWorker = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Worker:BuiltInEnabled", "true");
            b.UseSetting("Worker:Region", "eu-test");
        }))
        {
            withWorker.CreateClient();
            await Eventually.TrueAsync(() => BuiltInExists());
        }

        var client = await factory.CreateAuthenticatedClientAsync(Ct);
        var builtIn = await client.GetFromJsonAsync<JsonElement>("/api/v1/workers/builtin", Ct);

        Assert.True(builtIn.GetProperty("builtIn").GetBoolean());
        Assert.True(builtIn.GetProperty("tags").TryGetProperty("vigia:builtin", out var flag));
        Assert.Equal(JsonValueKind.Null, flag.ValueKind);
        Assert.Equal("eu-test", builtIn.GetProperty("tags").GetProperty("vigia:region").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync("/api/v1/workers/builtin", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/workers/builtin/enrollment-token", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/workers/builtin/revoke", null, Ct)).StatusCode);
    }

    private bool BuiltInExists()
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>().Workers.Any(w => w.Slug == "builtin" && w.BuiltIn);
    }

    private static async Task<(string Slug, string Token, JsonElement Body)> CreateWorkerAsync(
        HttpClient client, string? region = null, IReadOnlyDictionary<string, string?>? tags = null)
    {
        var slug = $"wk-{Guid.NewGuid():N}"[..20];
        var response = await client.PostAsJsonAsync("/api/v1/workers", new { slug, region, tags }, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        return (slug, body.GetProperty("enrollmentToken").GetString()!, body);
    }

    private async Task<string> EnrollAsync(string token)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/worker/v1/enroll", new { token, version = "test" }, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("credential").GetString()!;
    }

    private async Task<HttpResponseMessage> HeartbeatAsync(string credential, string? version = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Worker", credential);
        return await client.PostAsJsonAsync("/worker/v1/heartbeat", new { version }, Ct);
    }
}
