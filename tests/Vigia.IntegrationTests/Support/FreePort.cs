using System.Net;
using System.Net.Sockets;

namespace Vigia.IntegrationTests.Support;

/// <summary>
/// Local ports with nothing listening, for "connection refused" cases.
/// </summary>
public static class FreePort
{
    /// <summary>A TCP port that was free a moment ago.</summary>
    public static int Tcp()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>A UDP port that was free a moment ago.</summary>
    public static int Udp()
    {
        using var socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.Client.LocalEndPoint!).Port;
    }
}
