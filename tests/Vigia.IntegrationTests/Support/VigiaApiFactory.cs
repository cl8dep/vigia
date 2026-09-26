using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Runs the real API against a throwaway Postgres container and the staged built-in plugins.
/// Migrations are applied by the app itself on startup, so they are tested too.
/// </summary>
public sealed class VigiaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Password that satisfies the default Identity policy.</summary>
    public const string Password = "Str0ng!Passw0rd";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>Registers a new user, logs in, and returns a client sending its bearer token.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(CancellationToken ct)
    {
        var client = CreateClient();
        var email = $"{Guid.NewGuid():N}@vigia.test";

        var register = await client.PostAsJsonAsync("/api/v1/auth/sign-up", new { email, password = Password }, ct);
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email, password = Password }, ct);
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Vigia", _postgres.GetConnectionString());
        builder.UseSetting("Plugins:Path", RepoPaths.Plugins);

        // Tests create many users; the closed default is covered by AuthApiTests.
        builder.UseSetting("Auth:OpenSignUp", "true");

        // Background probing is off for API tests; BuiltInWorkerTests turns it on explicitly.
        builder.UseSetting("Worker:BuiltInEnabled", "false");
        builder.UseSetting("Logging:LogLevel:Microsoft.EntityFrameworkCore", "Warning");

        // Tests call ResultMaintenance directly so runs are deterministic.
        builder.UseSetting("Retention:Enabled", "false");
    }
}
