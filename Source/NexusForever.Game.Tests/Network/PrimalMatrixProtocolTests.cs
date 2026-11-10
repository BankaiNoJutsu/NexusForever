using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.World.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.PrimalMatrix;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.PrimalMatrix;

namespace NexusForever.Game.Tests.Network;

public sealed class PrimalMatrixProtocolTests
{
    [Fact]
    public void NativeSaveRequestHasARegisteredPublicDecoder()
    {
        MessageManager registry = Registry();
        Type type = registry.GetMessageType((GameMessageOpcode)0x0072);
        Assert.NotNull(type);
        Assert.Equal("ClientPrimalMatrixSave", type.Name);
    }

    [Fact]
    public void FormerAuctionSerializerCannotBeDispatchedAsAMarketplacePacket()
    {
        MessageManager registry = Registry();
        Assert.Null(registry.GetOpcode(new ServerAuctionPostAux()));
    }

    [Fact]
    public void FormerSerializerKeepsItsExistingWireBytesForCompatibility()
    {
        byte[] data = Write(new ServerAuctionPostAux
        {
            Values = { 0x01020304u, 0x11223344u }, Data = [1, 3], Value = 0x55667788
        });
        Assert.Equal(Convert.FromHexString("020000000403020144332211010388776655"), data);
        var canonical = new ServerPrimalMatrixSnapshot
        {
            NodeIds = { 0x01020304u, 0x11223344u }, SavedAllocationCounts = [1, 3], MatrixAmpPower = 0x55667788
        };
        Assert.Equal(data, Write(canonical));
    }

    [Fact]
    public void SnapshotHasOnePrimalMatrixRegistrationAndTheFormerAuctionTypeIsSerializerOnly()
    {
        MessageManager registry = Registry();
        Assert.Equal((ushort)0x0072, (ushort)GameMessageOpcode.ClientPrimalMatrixSave);
        Assert.Equal((ushort)0x06DF, (ushort)GameMessageOpcode.ServerPrimalMatrixSnapshot);
        Assert.Equal(GameMessageOpcode.ServerPrimalMatrixSnapshot, registry.GetOpcode(new ServerPrimalMatrixSnapshot()));
        Assert.Null(typeof(ServerAuctionPostAux).GetCustomAttribute<MessageAttribute>());
        var registrations = typeof(ServerPrimalMatrixSnapshot).Assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<MessageAttribute>()?.Opcode == (GameMessageOpcode)0x06DF).ToArray();
        Assert.Equal([typeof(ServerPrimalMatrixSnapshot)], registrations);
    }

    [Fact]
    public void PublicPacketReaderAndRegisteredHandlerDecodeIncrementsAndASessionWithoutAPlayerIsLeftAlone()
    {
        MessageManager registry = Registry();
        using var reader = new ClientGamePacketReader();
        reader.Initialise(new ClientGamePacket
        {
            IsEncrypted = false,
            Data = Convert.FromHexString("72000200000004030201443322110103")
        }, null);
        GameMessageOpcode opcode = reader.ReadHeader();
        var request = Assert.IsType<ClientPrimalMatrixSave>(Activator.CreateInstance(registry.GetMessageType(opcode)));
        Assert.Equal(0u, reader.ReadBody(request));
        Assert.Equal(new uint[] { 0x01020304, 0x11223344 }, request.NodeIds);
        Assert.Equal(new byte[] { 1, 3 }, request.AllocationIncrements);
        Assert.Equal(typeof(ClientPrimalMatrixSaveHandler), registry.GetMessageHandlerType(opcode));
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var state);
        registry.GetMessageHandlerDelegate(opcode)(new ClientPrimalMatrixSaveHandler(NullLogger<ClientPrimalMatrixSaveHandler>.Instance), session, request);
        // the handler now applies the request through the player's Primal Matrix manager; without a player it reads the session and does nothing else
        Assert.All(state.Invocations, invocation => Assert.Equal("get_Player", invocation.MethodName));
    }

    [Theory]
    [InlineData("030000")]
    [InlineData("FFFFFFFF")]
    [InlineData("02000000040302014433221101")]
    [InlineData("01000000040302")]
    public void TruncatedOrHostileCountsFailBeforeAllocatingTheCountedArrays(string hex)
    {
        byte[] bytes = Convert.FromHexString(hex);
        using var reader = new GamePacketReader(new MemoryStream(bytes));
        var request = new ClientPrimalMatrixSave();
        Assert.Throws<EndOfStreamException>(() => request.Read(reader));
        Assert.Empty(request.NodeIds); Assert.Empty(request.AllocationIncrements);
        Assert.Equal((uint)Math.Min(bytes.Length, 4), reader.BytePosition);
    }

    [Fact]
    public void ReaderPreservesRawZeroAndHighByteValuesAndLeavesTrailingDataToTheFrameReader()
    {
        // Parsing is not an allocation-policy decision: zeros/high bits remain
        // intact for future authoritative validation, rather than being clamped.
        using var reader = new GamePacketReader(new MemoryStream(Convert.FromHexString("02000000010000000200000000FFA5")));
        var request = new ClientPrimalMatrixSave(); request.Read(reader);
        Assert.Equal(new uint[] { 1, 2 }, request.NodeIds);
        Assert.Equal(new byte[] { 0, 255 }, request.AllocationIncrements);
        Assert.Equal((byte)0xA5, reader.ReadByte());
    }

    [Fact]
    public void EmptyRequestIsStructurallyDecodedWithoutFabricatingAnAllocationResult()
    {
        using var reader = new GamePacketReader(new MemoryStream(new byte[4]));
        var request = new ClientPrimalMatrixSave(); request.Read(reader);
        Assert.Empty(request.NodeIds); Assert.Empty(request.AllocationIncrements);
        Assert.Equal(0u, reader.BytesRemaining);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(8)]
    public void SnapshotWritesAbsoluteSavedCountsAndFullWidthMatrixContributionAcrossCompositeBoundaries(int count)
    {
        var snapshot = new ServerPrimalMatrixSnapshot { MatrixAmpPower = uint.MaxValue };
        snapshot.NodeIds.AddRange(Enumerable.Range(0, count).Select(index => 0x80000000u + (uint)index));
        snapshot.SavedAllocationCounts = Enumerable.Range(0, count).Select(index => (byte)(index * 17)).ToArray();
        using var expected = new MemoryStream();
        using (var writer = new BinaryWriter(expected, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((uint)count);
            foreach (uint id in snapshot.NodeIds) writer.Write(id);
            writer.Write(snapshot.SavedAllocationCounts);
            writer.Write(uint.MaxValue);
        }
        Assert.Equal(expected.ToArray(), Write(snapshot));
    }

    [Fact]
    public void MismatchedSnapshotColumnsRejectBeforeWritingAnyPartialPayload()
    {
        using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream);
        var snapshot = new ServerPrimalMatrixSnapshot { NodeIds = { 1, 2 }, SavedAllocationCounts = [1] };
        Assert.Throws<InvalidOperationException>(() => snapshot.Write(writer));
        Assert.Equal(0, stream.Length);
        var legacy = new ServerAuctionPostAux { Values = { 1 }, Data = [] };
        Assert.Throws<InvalidOperationException>(() => legacy.Write(writer));
        Assert.Equal(0, stream.Length);
    }

    private static MessageManager Registry()
    {
        var manager = new MessageManager(NullLogger<MessageManager>.Instance);
        manager.RegisterNetworkManagerWorldMessages();
        manager.RegisterNetworkManagerWorldHandlers();
        return manager;
    }

    private static byte[] Write(IWritable message)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream)) { message.Write(writer); writer.FlushBits(); }
        return stream.ToArray();
    }
}
