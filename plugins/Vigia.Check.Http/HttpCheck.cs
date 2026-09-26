using System.Diagnostics;
using Vigia.Plugins;

namespace Vigia.Check.Http;

/// <summary>
/// Sends an HTTP request and checks status code and, optionally, body content.
/// </summary>
public sealed class HttpCheck : Check<HttpCheckConfig>
{
    private const string StatusCode = "status-code";

    /// <inheritdoc />
    public override async Task<ProbeResult> ProbeAsync(HttpCheckConfig config, ICheckContext ctx, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(new HttpMethod(config.Method), config.Url);
        foreach (var (name, value) in config.Headers ?? [])
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(config.Timeout);

        var client = ctx.CreateHttpClient();
        var started = Stopwatch.GetTimestamp();
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ProbeResult.Down($"No response within {config.Timeout.TotalSeconds}s.");
        }
        catch (HttpRequestException ex)
        {
            return ProbeResult.Down($"Request failed: {ex.Message}");
        }

        using (response)
        {
            var latency = Measurement.Latency(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            var status = new Measurement(StatusCode, (int)response.StatusCode);

            if (!config.ExpectedStatus.Contains((int)response.StatusCode))
            {
                return ProbeResult.Down($"Status {(int)response.StatusCode}, expected {string.Join(", ", config.ExpectedStatus)}.", latency, status);
            }

            if (config.BodyContains is not null)
            {
                var body = await response.Content.ReadAsStringAsync(timeout.Token);
                if (!body.Contains(config.BodyContains, StringComparison.Ordinal))
                {
                    return ProbeResult.Down($"Body does not contain '{config.BodyContains}'.", latency, status);
                }
            }

            return ProbeResult.Up(latency, status);
        }
    }
}
