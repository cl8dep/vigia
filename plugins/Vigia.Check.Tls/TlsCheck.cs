using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Vigia.Plugins;

namespace Vigia.Check.Tls;

/// <summary>
/// Performs a TLS handshake and reports the certificate's remaining validity.
/// Down when the certificate is expired, or untrusted when chain validation is on.
/// </summary>
public sealed class TlsCheck : Check<TlsCheckConfig>
{
    private const string DaysToExpiry = "days-to-expiry";

    /// <inheritdoc />
    public override async Task<ProbeResult> ProbeAsync(TlsCheckConfig config, ICheckContext ctx, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(config.Timeout);
        using var client = new TcpClient();

        var policyErrors = SslPolicyErrors.None;
        var started = Stopwatch.GetTimestamp();
        try
        {
            await client.ConnectAsync(config.Host, config.Port, timeout.Token);
            await using var ssl = new SslStream(client.GetStream());
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = string.IsNullOrWhiteSpace(config.ServerName) ? config.Host : config.ServerName,
                // Accept every certificate here and judge it below, so an untrusted one still reports its expiry.
                RemoteCertificateValidationCallback = (_, _, _, errors) =>
                {
                    policyErrors = errors;
                    return true;
                },
            }, timeout.Token);

            var latency = Measurement.Latency(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            if (ssl.RemoteCertificate is not X509Certificate2 certificate)
            {
                return ProbeResult.Down("Server presented no certificate.", latency);
            }

            return Evaluate(certificate, policyErrors, config, ctx.Time.GetUtcNow(), latency);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ProbeResult.Down($"No handshake within {config.Timeout.TotalSeconds}s.");
        }
        catch (Exception ex) when (ex is SocketException or AuthenticationException or IOException)
        {
            return ProbeResult.Down($"TLS handshake failed: {ex.Message}");
        }
    }

    private static ProbeResult Evaluate(X509Certificate2 certificate, SslPolicyErrors errors, TlsCheckConfig config, DateTimeOffset now, Measurement latency)
    {
        var notAfter = new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero);
        var days = new Measurement(DaysToExpiry, (notAfter - now).TotalDays);

        if (notAfter <= now)
        {
            return ProbeResult.Down($"Certificate expired on {notAfter:yyyy-MM-dd}.", latency, days);
        }

        if (config.ValidateChain && errors != SslPolicyErrors.None)
        {
            return ProbeResult.Down($"Certificate is not valid: {errors}.", latency, days);
        }

        return ProbeResult.Up(latency, days);
    }
}
