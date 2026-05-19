using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.IO.Map;
using SharedItem = NexusForever.Network.World.Message.Model.Shared.Item;

namespace NexusForever.Game.Tests.Entity;

public class ProtocolRuntimeHardeningTests
{
    [Fact]
    public void ClientResurrectAccept_ReadRejectsUnsupportedResurrectionType()
    {
        byte[] packetData = BuildResurrectionAcceptPacket((ResurrectionType)32);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var message = new ClientResurrectAccept();

        InvalidPacketValueException exception = Assert.Throws<InvalidPacketValueException>(() => message.Read(reader));

        Assert.Contains("Unsupported resurrection accept type", exception.Message);
    }

    [Fact]
    public void ServerEntityCreate_WriteRejectsSpellInitData()
    {
        var packet = CreateEntityCreatePacket();
        packet.SpellInitData.Add(new ServerEntityCreate.SpellInit());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("spell initialisation data", exception.Message);
    }

    [Fact]
    public void ServerEntityCreate_WriteRejectsUnsupportedWorldPlacementType()
    {
        var packet = CreateEntityCreatePacket();
        packet.WorldPlacementData = new ServerEntityCreate.WorldPlacement
        {
            Type = 2
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(packet));

        Assert.Contains("WorldPlacement", exception.Message);
    }

    [Fact]
    public void SharedItem_WriteUsesUnknown88CountForUnknown88Payload()
    {
        var item = new SharedItem
        {
            LocationData = new ItemLocation(),
            Unknown58 =
            [
                new SharedItem.UnknownStructure(),
                new SharedItem.UnknownStructure()
            ]
        };
        item.Glyphs.Add(123u);
        item.Unknown88.Add(new SharedItem.UnknownStructure2());
        item.Unknown88.Add(new SharedItem.UnknownStructure2());

        byte[] packetData = WriteItemPacket(item);

        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);
        SkipItemFieldsBeforeVariablePayload(reader);

        Assert.Equal(0u, reader.ReadUInt(3u));
        Assert.Equal(1u, reader.ReadUInt(4u));
        Assert.Equal(123u, reader.ReadUInt());
        Assert.Equal(2u, reader.ReadUInt(6u));
    }

    [Theory]
    [InlineData(MapDefines.WorldGridOrigin * MapDefines.GridSize, 0f)]
    [InlineData(0f, MapDefines.WorldGridOrigin * MapDefines.GridSize)]
    public void MapGrid_GetGridCoordRejectsUpperWorldBoundary(float x, float z)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapGrid.GetGridCoord(new Vector3(x, 0f, z)));
    }

    [Theory]
    [InlineData(MapDefines.WorldGridOrigin * MapDefines.GridSize, 0f)]
    [InlineData(0f, MapDefines.WorldGridOrigin * MapDefines.GridSize)]
    public void MapCell_GetCellCoordRejectsUpperWorldBoundary(float x, float z)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapCell.GetCellCoord(new Vector3(x, 0f, z)));
    }

    [Fact]
    public void AppearanceManager_UpdateBonesCanRemoveAllExistingBones()
    {
        var manager = (AppearanceManager)RuntimeHelpers.GetUninitializedObject(typeof(AppearanceManager));
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 123u);

        IBone bone = RecordingDispatchProxy<IBone>.Create(out _);
        var characterBones = new Dictionary<byte, IBone>
        {
            [0] = bone
        };
        var deletedBones = new List<IBone>();

        SetPrivateField(manager, "owner", player);
        SetPrivateField(manager, "characterBones", characterBones);
        SetPrivateField(manager, "deletedCharacterBones", deletedBones);

        MethodInfo updateBones = typeof(AppearanceManager).GetMethod("UpdateBones", BindingFlags.Instance | BindingFlags.NonPublic)!;
        updateBones.Invoke(manager, [Array.Empty<float>()]);

        Assert.Empty(characterBones);
        Assert.Same(bone, Assert.Single(deletedBones));
        RecordingDispatchProxy<IPlayer>.Invocation invocation = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        var update = Assert.IsType<ServerEntityBoneUpdate>(invocation.Arguments[0]);
        Assert.Empty(update.Bones);
    }

    private static ServerEntityCreate CreateEntityCreatePacket()
    {
        return new ServerEntityCreate
        {
            Guid = 99u,
            Type = EntityType.Taxi,
            EntityModel = new TaxiEntityModel()
        };
    }

    private static byte[] BuildResurrectionAcceptPacket(ResurrectionType type)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(42u);
            writer.Write(type, 32u);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static byte[] WriteItemPacket(SharedItem item)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            item.Write(writer);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static void SkipItemFieldsBeforeVariablePayload(GamePacketReader reader)
    {
        reader.ReadULong();
        reader.ReadULong();
        reader.ReadUInt(18u);
        new ItemLocation().Read(reader);
        reader.ReadUInt();
        reader.ReadUInt();
        reader.ReadULong();
        reader.ReadUInt();
        reader.ReadULong();
        reader.ReadSingle();
        reader.ReadUInt();
        reader.ReadByte();
        reader.ReadUInt();
        reader.ReadUInt();
        reader.ReadUInt();

        for (int i = 0; i < 2; i++)
        {
            reader.ReadByte(3u);
            reader.ReadUInt();
            reader.ReadUInt();
        }

        reader.ReadUInt(18u);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static void WritePacket(ServerEntityCreate packet)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        packet.Write(writer);
        writer.FlushBits();
    }
}
