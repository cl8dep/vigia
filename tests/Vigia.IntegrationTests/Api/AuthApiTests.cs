using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Vigia.IntegrationTests.Support;

namespace Vigia.IntegrationTests.Api;

/// <summary>
/// Sign-up and sign-in through <c>AuthController</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class AuthApiTests(VigiaApiFactory factory)
{
    private static CancellationToken Ct
    {
        get { return TestContext.Current.CancellationToken; }
    }

    [Fact]
    public async Task Sign_up_then_sign_in_gives_a_working_token()
    {
        var client = factory.CreateClient();
        var email = NewEmail();

        var signUp = await client.PostAsJsonAsync("/api/v1/auth/sign-up", new { email, password = VigiaApiFactory.Password }, Ct);
        Assert.Equal(HttpStatusCode.Created, signUp.StatusCode);
        Assert.Equal(email, (await signUp.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("email").GetString());

        var signIn = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email, password = VigiaApiFactory.Password }, Ct);
        signIn.EnsureSuccessStatusCode();
        var tokens = await signIn.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Bearer", tokens.GetProperty("tokenType").GetString());
        Assert.True(tokens.GetProperty("expiresIn").GetInt64() > 0);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.GetProperty("accessToken").GetString());
        var checks = await client.GetAsync("/api/v1/checks", Ct);
        Assert.Equal(HttpStatusCode.OK, checks.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_is_unauthorized()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/v1/auth/sign-up", new { email, password = VigiaApiFactory.Password }, Ct);

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email, password = "Wr0ng!Password" }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_email_is_unauthorized_with_the_same_message()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-in", new { email = NewEmail(), password = VigiaApiFactory.Password }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Invalid email or password.", (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Weak_password_returns_password_errors()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/sign-up", new { email = NewEmail(), password = "short" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Duplicate_email_returns_email_error()
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/v1/auth/sign-up", new { email, password = VigiaApiFactory.Password }, Ct);

        var response = await client.PostAsJsonAsync("/api/v1/auth/sign-up", new { email, password = VigiaApiFactory.Password }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("email", out _));
    }

    [Fact]
    public async Task Sign_up_is_closed_once_users_exist_by_default()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/sign-up", new { email = NewEmail(), password = VigiaApiFactory.Password }, Ct);
        using var closed = factory.WithWebHostBuilder(b => b.UseSetting("Auth:OpenSignUp", "false"));

        var response = await closed.CreateClient().PostAsJsonAsync("/api/v1/auth/sign-up", new { email = NewEmail(), password = VigiaApiFactory.Password }, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string NewEmail()
    {
        return $"{Guid.NewGuid():N}@vigia.test";
    }
}
