using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using NexusForever.Network.Sts;
using NexusForever.Network.Sts.Model;
using NexusForever.StsServer.Network;
using NexusForever.StsServer.Network.Message.Handler;

namespace NexusForever.Game.Tests.Network;

public class LoadHarnessStsTests
{
    [Theory]
    [InlineData(1, 3, "loadtest0001@example.local", "loadtest0003@example.local")]
    [InlineData(9101, 3, "loadtest9101@example.local", "loadtest9103@example.local")]
    [InlineData(9101, 1, "loadtest9101@example.local", "loadtest9101@example.local")]
    public void ConcurrentUsersHonorSeededStartIndex(int start, int count, string first, string last)
    {
        var options = new LoginWorldOptions { StartIndex = start, ConcurrentUsers = count };
        var users = LoginWorldCommand.ConcurrentUserNames(options);
        Assert.Equal(count, users.Count);
        Assert.Equal(first, users[0]);
        Assert.Equal(last, users[^1]);
        Assert.Equal(start.ToString(), options.ToDictionary()["startIndex"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConcurrentUserSelectionRejectsNonPositiveStartIndex(int start) => Assert.Throws<ArgumentOutOfRangeException>(
        () => LoginWorldCommand.ConcurrentUserNames(new LoginWorldOptions { StartIndex = start, ConcurrentUsers = 1 }));

    [Fact]
    public void ConnectEnvelopeParsesWithMappedServerModelAndEntersConnectedState()
    {
        var document = new XmlDocument();
        document.LoadXml(StsProtocolClient.BuildConnectEnvelope("127.0.0.1"));
        var connect = new ClientConnectMessage();
        connect.Read(document);
        Assert.Equal(16042u, connect.Build);
        Assert.Equal("127.0.0.1", connect.Address);
        Assert.Equal((uint)Environment.ProcessId, connect.Process);
        Assert.Null(connect.ConnProductType);
        var session = new StsSession(null);
        Assert.Equal(SessionState.None, session.State);
        StsHandler.HandleConnect(session, connect);
        Assert.Equal(SessionState.Connected, session.State);
    }

    [Fact]
    public async Task ConnectIsSentBeforeLoginWithoutWaitingForAnAcknowledgement()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        await using var client = new StsProtocolClient("127.0.0.1", port, TimeSpan.FromMilliseconds(500));
        Task connection = client.ConnectAsync();
        using TcpClient peer = await listener.AcceptTcpClientAsync();
        Request connect = await ReadRequest(peer.GetStream());
        await connection.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.StartsWith("POST /Sts/Connect STS/1.0", connect.Header);
        Assert.Contains("s:1\r\n", connect.Header);
        Assert.StartsWith("<Connect>", connect.Body);

        Task<string> login = client.LoginAndRequestGameToken("assessment@example.local", "test");
        Request loginStart = await ReadRequest(peer.GetStream());
        Assert.StartsWith("POST /Auth/LoginStart STS/1.0", loginStart.Header);
        Assert.Contains("s:2\r\n", loginStart.Header);
        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(() => login.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Contains("/Auth/LoginStart", exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WholeRequestTimeoutBoundsPartialHeaderAndBodyProgress(bool partialBody)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        await using var client = new StsProtocolClient("127.0.0.1", port, TimeSpan.FromMilliseconds(250));
        Task connection = client.ConnectAsync();
        using TcpClient peer = await listener.AcceptTcpClientAsync();
        await ReadRequest(peer.GetStream());
        await connection;
        Task<string> login = client.LoginAndRequestGameToken("assessment@example.local", "test");
        await ReadRequest(peer.GetStream());
        string prefix = partialBody ? "STS/1.0 200 OK\r\nl:100\r\ns:2\r\n\r\n<Reply>" : "STS/1.0 200 OK\r\nl:";
        await peer.GetStream().WriteAsync(Encoding.UTF8.GetBytes(prefix));
        await Assert.ThrowsAsync<TimeoutException>(() => login.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task PeerClosingDuringReplyStillReportsEndOfStream()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        await using var client = new StsProtocolClient("127.0.0.1", port, TimeSpan.FromSeconds(2));
        Task connection = client.ConnectAsync();
        using TcpClient peer = await listener.AcceptTcpClientAsync();
        await ReadRequest(peer.GetStream());
        await connection;
        Task<string> login = client.LoginAndRequestGameToken("assessment@example.local", "test");
        await ReadRequest(peer.GetStream());
        peer.Dispose();
        await Assert.ThrowsAsync<IOException>(() => login.WaitAsync(TimeSpan.FromSeconds(3)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveTimeoutIsRejected(int milliseconds) => Assert.Throws<ArgumentOutOfRangeException>(
        () => new StsProtocolClient("127.0.0.1", 1, TimeSpan.FromMilliseconds(milliseconds)));

    private static async Task<Request> ReadRequest(NetworkStream stream)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var header = new List<byte>();
        byte[] single = new byte[1];
        while (header.Count < 4096)
        {
            await stream.ReadExactlyAsync(single, deadline.Token);
            header.Add(single[0]);
            if (header.Count >= 4 && header.TakeLast(4).SequenceEqual(new byte[] { 13, 10, 13, 10 })) break;
        }
        string headerText = Encoding.UTF8.GetString(header.ToArray());
        string length = headerText.Split("\r\n").Single(line => line.StartsWith("l:"))[2..];
        byte[] body = new byte[int.Parse(length)];
        await stream.ReadExactlyAsync(body, deadline.Token);
        return new Request(headerText, Encoding.UTF8.GetString(body));
    }

    private sealed record Request(string Header, string Body);
}
