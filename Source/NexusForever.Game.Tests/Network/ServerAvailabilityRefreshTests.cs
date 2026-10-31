using System.Net;
using System.Net.Sockets;
using NexusForever.Database.Auth.Model;
using NexusForever.Game.Server;

namespace NexusForever.Game.Tests.Network;

public class ServerAvailabilityRefreshTests
{
    [Fact]
    public async Task MetadataRefreshKeepsAReachableRealmAvailableBeforeTheNextHeartbeat()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        ushort port = checked((ushort)((IPEndPoint)listener.LocalEndpoint).Port);
        var manager = new ServerManager();
        manager.RefreshServerDefinitions([Model(port)]);
        var before = Assert.Single(manager.Servers);
        Assert.False(before.IsOnline);
        await before.PingHostAsync();
        Assert.True(before.IsOnline);

        manager.RefreshServerDefinitions([Model(port, name: "Renamed", type: 1)]);
        var refreshed = Assert.Single(manager.Servers);
        Assert.NotSame(before, refreshed);
        Assert.True(refreshed.IsOnline);
        Assert.Equal("Renamed", refreshed.Model.Name);
        Assert.Equal(1, refreshed.Model.Type);

        listener.Stop();
        await refreshed.PingHostAsync();
        Assert.False(refreshed.IsOnline);
        manager.RefreshServerDefinitions([Model(port)]);
        Assert.False(Assert.Single(manager.Servers).IsOnline);
    }

    [Theory]
    [InlineData("realm")]
    [InlineData("address")]
    [InlineData("port")]
    public async Task ChangedEndpointsAndRealmsMustPassTheirOwnHeartbeat(string change)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        ushort port = checked((ushort)((IPEndPoint)listener.LocalEndpoint).Port);
        var manager = new ServerManager();
        manager.RefreshServerDefinitions([Model(port)]);
        await Assert.Single(manager.Servers).PingHostAsync();
        Assert.True(manager.Servers[0].IsOnline);
        ServerModel next = Model(port);
        if (change == "realm") next.Id = 2;
        if (change == "address") next.Host = "127.0.0.2";
        if (change == "port") next.Port = port == ushort.MaxValue ? (ushort)(port - 1) : (ushort)(port + 1);
        manager.RefreshServerDefinitions([next]);
        Assert.False(Assert.Single(manager.Servers).IsOnline);
    }

    [Fact]
    public async Task RemovingThenReaddingARealmDoesNotRestoreOldHealth()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        ushort port = checked((ushort)((IPEndPoint)listener.LocalEndpoint).Port);
        var manager = new ServerManager();
        manager.RefreshServerDefinitions([Model(port)]);
        await Assert.Single(manager.Servers).PingHostAsync();
        Assert.True(manager.Servers[0].IsOnline);
        manager.RefreshServerDefinitions([]);
        Assert.Empty(manager.Servers);
        manager.RefreshServerDefinitions([Model(port)]);
        Assert.False(Assert.Single(manager.Servers).IsOnline);
    }

    private static ServerModel Model(ushort port, string name = "Assessment", byte type = 0) =>
        new() { Id = 1, Host = "127.0.0.1", Port = port, Name = name, Type = type };
}
