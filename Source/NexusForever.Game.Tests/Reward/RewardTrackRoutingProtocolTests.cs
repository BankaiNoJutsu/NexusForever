using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Map;
using NexusForever.Network.World.Message.Model.Reward;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Map;

namespace NexusForever.Game.Tests.Reward;

public sealed class RewardTrackRoutingProtocolTests
{
    [Fact]
    public void MessageRegistryUsesVerifiedOwnersAndDoesNotRegisterFormerCrossOwnerAliases()
    {
        MessageManager manager = Registry();
        Assert.Equal(typeof(ClientRewardTrackChooseTrack), manager.GetMessageType((GameMessageOpcode)0x07CF));
        Assert.Equal(typeof(ClientRewardTrackClaimRewardPoint), manager.GetMessageType((GameMessageOpcode)0x07CE));
        Assert.NotEqual(typeof(ClientGenericMapNodeRequestHandler), manager.GetMessageHandlerType((GameMessageOpcode)0x07CF));
        Assert.Equal((GameMessageOpcode)0x07CD, manager.GetOpcode(new ServerRewardTrackAdd()));
        Assert.Equal((GameMessageOpcode)0x07D3, manager.GetOpcode(new ServerRewardTracksReload()));
        Assert.Equal((GameMessageOpcode)0x07D5, manager.GetOpcode(new ServerRewardTrackUpdate()));
        Assert.Equal((GameMessageOpcode)0x07D0, manager.GetOpcode(new ServerRewardTrackRemove()));
        Assert.Null(manager.GetOpcode(new ServerRewardRotationContentContext()));
        Assert.Null(manager.GetOpcode(new ServerRewardRotationContentContextArray()));
        Assert.Null(manager.GetOpcode(new ServerAuctionsByFilterAux()));
        Assert.Null(typeof(ClientGenericMapNodeRequest).GetCustomAttribute<MessageAttribute>());
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(24u)]
    [InlineData(16383u)]
    public void RegisteredChooseReaderConsumesOneFourteenBitTrackId(uint id)
    {
        MessageManager manager = Registry();
        byte[] data = Payload(writer => { writer.Write(id, 14); writer.Write((byte)0xA5); });
        using var reader = Reader(data);
        var message = Assert.IsType<ClientRewardTrackChooseTrack>(Activator.CreateInstance(manager.GetMessageType((GameMessageOpcode)0x07CF)));
        message.Read(reader);
        Assert.Equal(id, (uint)message.RewardTrackId);
        Assert.Equal((byte)0xA5, reader.ReadByte());
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(2u)]
    [InlineData(uint.MaxValue)]
    public void ClaimReaderMatchesNativeRewardRowAndChoiceSentinel(uint choice)
    {
        byte[] data = Payload(writer => { writer.Write(475u, 14); writer.Write(choice); writer.Write((byte)0x5A); });
        using var reader = Reader(data);
        var message = new ClientRewardTrackClaimRewardPoint();
        message.Read(reader);
        Assert.Equal((ushort)475, message.RewardTrackRewardsId);
        Assert.Equal(choice, message.ChoiceIndex);
        Assert.Equal((byte)0x5A, reader.ReadByte());
    }

    [Fact]
    public void AddRowMatchesExactNativeReaderOrderAndClaimMaskBitPositions()
    {
        RewardTrack track = Track(39, true);
        track.ClaimedRewards.SetBit(0, true);
        track.ClaimedRewards.SetBit(8, true);
        track.ClaimedRewards.SetBit(31, true);
        using var reader = Reader(Write(new ServerRewardTrackAdd { RewardTrack = track }));
        ReadRow(reader, 39, 0x80000101u, true);
    }

    [Fact]
    public void ReloadKeepsBitPackedRowBoundariesAcrossDifferentActiveFlags()
    {
        var packet = new ServerRewardTracksReload { RewardsTracks = [Track(24, false), Track(39, true)] };
        using var reader = Reader(Write(packet));
        Assert.Equal(2u, reader.ReadUInt());
        ReadRow(reader, 24, 0, false);
        ReadRow(reader, 39, 0, true);
    }

    [Fact]
    public void ProgressUpdateMatchesNativePointsClaimedUnlockActiveOrder()
    {
        var packet = new ServerRewardTrackUpdate { RewardTrackId = 9, RewardPointsEarned = 1600, Unlocked = 3, IsActive = true };
        packet.ClaimedRewards.SetBit(31, true);
        using var reader = Reader(Write(packet));
        Assert.Equal(9u, reader.ReadUInt(14));
        Assert.Equal(1600u, reader.ReadUInt());
        Assert.Equal(0x80000000u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
    }

    [Fact]
    public void RemoveZeroRetainsNativeClearAllValue()
    {
        using var reader = Reader(Write(new ServerRewardTrackRemove { RewardTrackId = 0 }));
        Assert.Equal(0u, reader.ReadUInt(14));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void MalformedTrackRowsFailBeforeSerialization(int invalid)
    {
        RewardTrack track = Track(9, false);
        if (invalid == 0) track.RewardTrackId = 0x4000;
        if (invalid == 1) track.ClaimedRewards = new NetworkBitArray(16, NetworkBitArray.BitOrder.LeastSignificantBit);
        if (invalid == 2) track.RewardTrackRewardsIds = null;
        if (invalid == 3) track.RewardTrackRewardsIds = Enumerable.Repeat(475u, 33).ToList();
        Assert.ThrowsAny<Exception>(() => Write(new ServerRewardTrackAdd { RewardTrack = track }));
    }

    [Fact]
    public void RotationDeltasKeepTheirServiceTypesWithVerifiedRemoveUpdateUpsertOpcodes()
    {
        MessageManager manager = Registry();
        Assert.Equal((GameMessageOpcode)0x07C7, manager.GetOpcode(new ServerRewardRotationEntryStateRemove()));
        Assert.Equal((GameMessageOpcode)0x07C9, manager.GetOpcode(new ServerRewardRotationEntryStateUpdate()));
        Assert.Equal((GameMessageOpcode)0x07CB, manager.GetOpcode(new ServerRewardRotationEntryStateUpsert()));
        Assert.Equal((GameMessageOpcode)0x07C8, manager.GetOpcode(new ServerRewardRotationEntryStateArray()));
        Assert.Equal((GameMessageOpcode)0x07CA, manager.GetOpcode(new ServerRewardRotationScheduleArray()));
    }

    [Fact]
    public void ExistingGenericMapChosen0754StillRoutesThroughRealHandlerDelegate()
    {
        MessageManager manager = Registry();
        Assert.Equal(typeof(ClientGenericMapNodeChosen), manager.GetMessageType((GameMessageOpcode)0x0754));
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        var table = (GameTable<GenericMapNodeEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<GenericMapNodeEntry>));
        typeof(GameTable<GenericMapNodeEntry>).GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new[] { new GenericMapNodeEntry { Id = 7 } });
        typeof(GameTable<GenericMapNodeEntry>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = 8 });
        typeof(GameTable<GenericMapNodeEntry>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new[] { -1, -1, -1, -1, -1, -1, -1, 0 });
        tableProxy.SetProperty(nameof(IGameTableManager.GenericMapNode), table);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        var player = RecordingDispatchProxy<IPlayer>.Create(out _);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        using var reader = Reader(Payload(writer => writer.Write(7u, 14)));
        var message = (IReadable)Activator.CreateInstance(manager.GetMessageType((GameMessageOpcode)0x0754));
        message.Read(reader);
        var handler = new ClientGenericMapNodeChosenHandler(NullLogger<ClientGenericMapNodeChosenHandler>.Instance, tables);
        manager.GetMessageHandlerDelegate((GameMessageOpcode)0x0754)(handler, session, message);
        Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]).OfType<ServerGenericMapNode>());
    }

    private static MessageManager Registry()
    {
        var manager = new MessageManager(NullLogger<MessageManager>.Instance);
        foreach (Type type in typeof(ClientRewardTrackChooseTrack).Assembly.GetTypes()) manager.RegisterMessage(type);
        manager.RegisterMessageHandler(typeof(ClientGenericMapNodeRequestHandler));
        manager.RegisterMessageHandler(typeof(ClientGenericMapNodeChosenHandler));
        return manager;
    }

    private static RewardTrack Track(uint id, bool active) => new()
    {
        RewardTrackId = id, RewardPointsEarned = 1600, Unlocked = 3, IsActive = active, RewardTrackRewardsIds = [475, 476]
    };

    private static void ReadRow(GamePacketReader reader, uint id, uint claimed, bool active)
    {
        Assert.Equal(id, reader.ReadUInt(14));
        Assert.Equal(1600u, reader.ReadUInt());
        Assert.Equal(claimed, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(475u, reader.ReadUInt());
        Assert.Equal(476u, reader.ReadUInt());
        Assert.Equal(active, reader.ReadBit());
    }
    private static GamePacketReader Reader(byte[] data) => new(new MemoryStream(data));
    private static byte[] Write(IWritable packet) => Payload(packet.Write);
    private static byte[] Payload(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream)) { write(writer); writer.FlushBits(); }
        return stream.ToArray();
    }
}
