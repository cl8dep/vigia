using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Vigia.Infrastructure.Identity;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Refresh token flow through <c>POST /api/v1/auth/refresh</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class RefreshApiTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Refresh_returns_new_tokens_that_work()
    {
        var client = factory.CreateClient();
        var (_, tokens) = await SignUpAndInAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.GetProperty("refreshToken").GetString() }, Ct);

        response.EnsureSuccessStatusCode();
        var refreshed = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.False(string.IsNullOrEmpty(refreshed.GetProperty("refreshToken").GetString()));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/checks", Ct)).StatusCode);
    }

    [Fact]
    public async Task Garbage_token_is_unauthorized()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "not-a-token" }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Access_token_cannot_be_used_as_refresh_token()
    {
        var client = factory.CreateClient();
        var (_, tokens) = await SignUpAndInAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.GetProperty("accessToken").GetString() }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Security_stamp_change_revokes_refresh_tokens()
    {
        var client = factory.CreateClient();
        var (email, tokens) = await SignUpAndInAsync(client);

        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            await users.UpdateSecurityStampAsync((await users.FindByEmailAsync(email))!);
        }

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = tokens.GetProperty("refreshToken").GetString() }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<(string Email, JsonElement Tokens)> SignUpAndInAsync(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@vigia.test";
        await client.PostAsJsonAsync("/api/v1/auth/sign-up", new { email, password = VigiaApiFactory.Password }, Ct);
        var signIn = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email, password = VigiaApiFactory.Password }, Ct);
        signIn.EnsureSuccessStatusCode();
        return (email, await signIn.Content.ReadFromJsonAsync<JsonElement>(Ct));
    }
}
