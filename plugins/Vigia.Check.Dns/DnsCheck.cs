using System.Diagnostics;
using System.Net;
using DnsClient;
using DnsClient.Protocol;
using Vigia.Plugins;

namespace Vigia.Check.Dns;

/// <summary>
/// Resolves a record against one or more resolvers and compares the answers with the expected values.
/// Querying several resolvers separately shows whether a failure is global or limited to one resolver.
/// </summary>
public sealed class DnsCheck : Check<DnsCheckConfig>
{
    private const string FailedResolvers = "failed-resolvers";

    /// <inheritdoc />
    public override async Task<ProbeResult> ProbeAsync(DnsCheckConfig config, ICheckContext ctx, CancellationToken ct)
    {
        var type = Enum.Parse<QueryType>(config.RecordType, ignoreCase: true);
        var endpoints = new List<IPEndPoint?>();
        foreach (var resolver in config.Resolvers)
        {
            if (!TryParseResolver(resolver, out var endpoint))
            {
                return ProbeResult.Error($"Resolver '{resolver}' is not an IP or IP:port.");
            }

            endpoints.Add(endpoint);
        }

        if (endpoints.Count == 0)
        {
            endpoints.Add(null);
        }

        var answers = await Task.WhenAll(endpoints.Select(e => QueryAsync(e, config, type, ct)));
        var expected = config.Expected.Select(Normalize).ToList();
        var problems = answers
            .Select(a => (a.Resolver, Problem: Judge(a, expected)))
            .Where(p => p.Problem is not null)
            .ToList();

        var latency = Measurement.Latency(answers.Max(a => a.LatencyMs));
        var failed = new Measurement(FailedResolvers, problems.Count);
        if (problems.Count == 0)
        {
            return ProbeResult.Up(latency, failed);
        }

        var message = string.Join("; ", problems.Select(p => $"{p.Resolver}: {p.Problem}"));
        return ProbeResult.Down(message, latency, failed);
    }

    private static async Task<ResolverAnswer> QueryAsync(IPEndPoint? endpoint, DnsCheckConfig config, QueryType type, CancellationToken ct)
    {
        var options = endpoint is null ? new LookupClientOptions() : new LookupClientOptions(endpoint);
        options.Timeout = config.Timeout;
        options.Retries = 0;
        options.UseCache = false;
        options.ThrowDnsErrors = false;
        var client = new LookupClient(options);
        var name = endpoint?.ToString() ?? "system";

        var started = Stopwatch.GetTimestamp();
        try
        {
            var response = await client.QueryAsync(config.Host, type, cancellationToken: ct);
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            if (response.HasError)
            {
                return new ResolverAnswer(name, [], response.ErrorMessage, elapsed);
            }

            var values = response.Answers.Select(ValueOf).OfType<string>().Select(Normalize).ToList();
            return new ResolverAnswer(name, values, null, elapsed);
        }
        catch (DnsResponseException ex)
        {
            return new ResolverAnswer(name, [], ex.Message, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    private static string? Judge(ResolverAnswer answer, IReadOnlyList<string> expected)
    {
        if (answer.Error is not null)
        {
            return answer.Error;
        }

        if (answer.Values.Count == 0)
        {
            return "empty answer";
        }

        var missing = expected.Where(e => !answer.Values.Contains(e)).ToList();
        return missing.Count == 0 ? null : $"missing {string.Join(", ", missing)} (got {string.Join(", ", answer.Values)})";
    }

    private static string? ValueOf(DnsResourceRecord record)
    {
        return record switch
        {
            ARecord a => a.Address.ToString(),
            AaaaRecord aaaa => aaaa.Address.ToString(),
            CNameRecord cname => cname.CanonicalName.Value,
            MxRecord mx => mx.Exchange.Value,
            TxtRecord txt => string.Concat(txt.Text),
            NsRecord ns => ns.NSDName.Value,
            _ => null,
        };
    }

    private static string Normalize(string value)
    {
        return value.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static bool TryParseResolver(string value, out IPEndPoint? endpoint)
    {
        if (IPAddress.TryParse(value, out var address))
        {
            endpoint = new IPEndPoint(address, 53);
            return true;
        }

        var parsed = IPEndPoint.TryParse(value, out var withPort);
        endpoint = withPort;
        return parsed;
    }
}
