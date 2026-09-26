using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Real Kestrel server on a random local port, used as a probe target.
/// </summary>
public sealed class LocalHttpServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private LocalHttpServer(WebApplication app, string baseUrl)
    {
        _app = app;
        BaseUrl = baseUrl;
    }

    /// <summary>Base URL, for example <c>http://127.0.0.1:54321</c>.</summary>
    public string BaseUrl { get; }

    /// <summary>Starts a server with <c>/ok</c> (200, body "healthy") and <c>/fail</c> (500).</summary>
    public static async Task<LocalHttpServer> StartAsync(CancellationToken ct)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapGet("/ok", Ok);
        app.MapGet("/fail", Fail);
        await app.StartAsync(ct);

        var address = app.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.First();
        return new LocalHttpServer(app, address);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
    }

    private static string Ok()
    {
        return "healthy";
    }

    private static IResult Fail()
    {
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
}
