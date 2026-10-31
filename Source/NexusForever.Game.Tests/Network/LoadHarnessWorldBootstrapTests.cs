using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using NexusForever.Cryptography;
using NexusForever.Network;
using NexusForever.Network.Message;

namespace NexusForever.Game.Tests.Network;

[Collection(LoadHarnessWorldBootstrapCollection.Name)]
public class LoadHarnessWorldBootstrapTests
{
    private const ulong Key = 0x12345678;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RealEntrySequenceWaitsForBothBootstrapPacketsBeforeSendingEnteredWorld(bool encrypted, bool reverse)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        await using var client = new GameProtocolClient("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port, Key);
        Task connecting = client.ConnectAsync();
        using TcpClient peer = await listener.AcceptTcpClientAsync();
        await connecting;
        var sample = new LoginWorldSample();
        Task entering = LoginWorldCommand.CompleteWorldEntry(client, 2550, 0, sample, TimeSpan.FromSeconds(2));
        GameMessageOpcode first = reverse ? GameMessageOpcode.ServerPlayerCreate : GameMessageOpcode.ServerChangeWorld;
        GameMessageOpcode second = reverse ? GameMessageOpcode.ServerChangeWorld : GameMessageOpcode.ServerPlayerCreate;

        await Send(peer, GameMessageOpcode.ServerPlayerEnteredWorld, encrypted); // An unsolicited old reply cannot satisfy the barrier.
        await Send(peer, first, encrypted);
        await Task.Delay(40);
        Assert.False(entering.IsCompleted);
        Assert.Equal(0, peer.Available);

        byte[] remaining = Frame(second, encrypted);
        await peer.GetStream().WriteAsync(remaining.AsMemory(0, 3));
        await Task.Delay(30);
        Assert.Equal(0, peer.Available); // Partial frame header is not readiness.
        await peer.GetStream().WriteAsync(remaining.AsMemory(3));

        GameFrame request = await Receive(peer);
        Assert.Equal(GameMessageOpcode.ClientEncrypted, request.Opcode);
        using var envelope = new MemoryStream(request.Body);
        using var envelopeReader = new GamePacketReader(envelope);
        uint length = envelopeReader.ReadUInt();
        byte[] cipher = envelopeReader.ReadBytes(length - 4);
        GameFrame inner = PacketWriters.ParsePayload(new PacketCrypt(Key).Decrypt(cipher, cipher.Length));
        Assert.Equal(GameMessageOpcode.ClientEnteredWorld, inner.Opcode);
        using var body = new MemoryStream(inner.Body);
        using var bodyReader = new GamePacketReader(body);
        Assert.Equal((ushort)2550, bodyReader.ReadUShort(15));
        Assert.False(entering.IsCompleted);

        await Send(peer, GameMessageOpcode.ServerPlayerEnteredWorld, encrypted);
        await entering.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(new[] { "world-bootstrap", "world-entered-world" }, sample.Steps.Select(step => step.Name));
    }

    [Theory]
    [InlineData(GameMessageOpcode.ServerCharacterSelectFail)]
    [InlineData(GameMessageOpcode.ServerForceKick)]
    public async Task RejectedBootstrapFailsWithoutSendingEntry(GameMessageOpcode rejection)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        await using var client = new GameProtocolClient("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port, Key);
        Task connecting = client.ConnectAsync();
        using TcpClient peer = await listener.AcceptTcpClientAsync();
        await connecting;
        Task entering = LoginWorldCommand.CompleteWorldEntry(client, 0, 0, new LoginWorldSample(), TimeSpan.FromSeconds(1));
        await Send(peer, rejection, true);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => entering);
        Assert.Contains(rejection.ToString(), exception.Message);
        Assert.Equal(0, peer.Available);
    }

    [Fact]
    public async Task IncompleteBootstrapHasOneBoundedDeadlineAndSendsNoEntry()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        await using var client = new GameProtocolClient("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port, Key);
        Task connecting = client.ConnectAsync();
        using TcpClient peer = await listener.AcceptTcpClientAsync();
        await connecting;
        Task entering = LoginWorldCommand.CompleteWorldEntry(client, 0, 0, new LoginWorldSample(), TimeSpan.FromMilliseconds(150));
        await Send(peer, GameMessageOpcode.ServerChangeWorld, false);
        TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(() => entering.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Contains("playerCreate=False", exception.Message);
        Assert.Equal(0, peer.Available);
    }

    [Fact]
    public async Task IrrelevantProgressCannotRestartTheBootstrapDeadline()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        await using var client = new GameProtocolClient("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port, Key);
        Task connecting = client.ConnectAsync();
        using TcpClient peer = await listener.AcceptTcpClientAsync();
        await connecting;
        using var stopProgress = new CancellationTokenSource();
        Task entering = LoginWorldCommand.CompleteWorldEntry(client, 0, 0, new LoginWorldSample(), TimeSpan.FromMilliseconds(150));
        Task progress = Task.Run(async () =>
        {
            try
            {
                while (!stopProgress.IsCancellationRequested)
                {
                    await Send(peer, GameMessageOpcode.ServerHello, false);
                    await Task.Delay(20, stopProgress.Token);
                }
            }
            catch (OperationCanceledException) when (stopProgress.IsCancellationRequested) { }
        });
        try
        {
            TimeoutException exception = await Assert.ThrowsAsync<TimeoutException>(() => entering.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.Contains("world bootstrap", exception.Message);
            Assert.Contains("changeWorld=False, playerCreate=False", exception.Message);
            Assert.Equal(0, peer.Available);
        }
        finally
        {
            stopProgress.Cancel();
            await progress;
        }
    }

    private static Task Send(TcpClient peer, GameMessageOpcode opcode, bool encrypted)
        => peer.GetStream().WriteAsync(Frame(opcode, encrypted)).AsTask();

    private static byte[] Frame(GameMessageOpcode opcode, bool encrypted)
    {
        if (!encrypted) return PacketWriters.BuildFrame(opcode, _ => { });
        byte[] inner = PacketWriters.BuildInner(opcode, _ => { });
        byte[] cipher = new PacketCrypt(Key).Encrypt(inner, inner.Length);
        return PacketWriters.BuildFrame(GameMessageOpcode.ServerRealmEncrypted, writer =>
        {
            writer.Write((uint)cipher.Length + 4);
            writer.WriteBytes(cipher);
        });
    }

    private static async Task<GameFrame> Receive(TcpClient peer)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        byte[] header = new byte[4];
        await peer.GetStream().ReadExactlyAsync(header, deadline.Token);
        byte[] payload = new byte[BinaryPrimitives.ReadUInt32LittleEndian(header) - 4];
        await peer.GetStream().ReadExactlyAsync(payload, deadline.Token);
        return PacketWriters.ParsePayload(payload);
    }
}
