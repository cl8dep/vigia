using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Minimal UDP DNS server on a random local port. Answers A queries from a fixed table; everything else is NXDOMAIN.
/// </summary>
public sealed class LocalDnsServer : IAsyncDisposable
{
    private readonly UdpClient _socket = new(new IPEndPoint(IPAddress.Loopback, 0));
    private readonly IReadOnlyDictionary<string, IPAddress[]> _records;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _serve;

    /// <summary>Starts the server with A records keyed by lowercase host name.</summary>
    public LocalDnsServer(IReadOnlyDictionary<string, IPAddress[]> records)
    {
        _records = records;
        _serve = ServeAsync();
    }

    /// <summary>Resolver address as <c>IP:port</c>.</summary>
    public string Endpoint
    {
        get { return _socket.Client.LocalEndPoint!.ToString()!; }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _socket.Dispose();
        await _serve;
        _stop.Dispose();
    }

    private async Task ServeAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            UdpReceiveResult request;
            try
            {
                request = await _socket.ReceiveAsync(_stop.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException)
            {
                return;
            }

            var response = Answer(request.Buffer);
            await _socket.SendAsync(response, request.RemoteEndPoint, _stop.Token);
        }
    }

    private byte[] Answer(byte[] query)
    {
        var (name, questionEnd) = ReadQuestion(query);
        var qtype = BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(questionEnd - 4));
        var addresses = qtype == 1 && _records.TryGetValue(name, out var found) ? found : [];
        var known = _records.ContainsKey(name);

        var response = new List<byte>();
        response.AddRange(query.AsSpan(0, 2).ToArray());
        response.AddRange(known ? [0x81, 0x80] : [0x81, 0x83]);
        response.AddRange([0, 1]);
        response.AddRange([0, (byte)addresses.Length]);
        response.AddRange([0, 0, 0, 0]);
        response.AddRange(query.AsSpan(12, questionEnd - 12).ToArray());
        foreach (var address in addresses)
        {
            response.AddRange([0xC0, 0x0C, 0, 1, 0, 1, 0, 0, 0, 60, 0, 4]);
            response.AddRange(address.GetAddressBytes());
        }

        return [.. response];
    }

    private static (string Name, int QuestionEnd) ReadQuestion(byte[] query)
    {
        var labels = new List<string>();
        var offset = 12;
        while (query[offset] != 0)
        {
            var length = query[offset];
            labels.Add(Encoding.ASCII.GetString(query, offset + 1, length));
            offset += length + 1;
        }

        return (string.Join('.', labels).ToLowerInvariant(), offset + 1 + 4);
    }
}
