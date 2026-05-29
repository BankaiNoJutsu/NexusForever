using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Static.Entity.Movement.Command;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Command;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.IO.Map;
using NexusForever.Shared;
using SharedItem = NexusForever.Network.World.Message.Model.Shared.Item;

namespace NexusForever.Game.Tests.Entity;

[Collection(LegacyServiceProviderCollection.Name)]
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

    [Fact]
    public void GamePacketWriter_WriteRejectsValueOutsideBitWidth()
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => writer.Write(8u, 3u));

        Assert.Contains("3-bit wire limit", exception.Message);
    }

    [Fact]
    public void GamePacketReader_ReadBitRejectsTruncatedStream()
    {
        using var stream = new MemoryStream();
        using var reader = new GamePacketReader(stream);

        Assert.Throws<EndOfStreamException>(() => reader.ReadBit());
    }

    [Fact]
    public void SetPositionKeysCommand_WriteRejectsMismatchedTimesAndPositions()
    {
        var command = new SetPositionKeysCommand
        {
            Times = [1u],
            Positions = []
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(command));

        Assert.Contains("same count", exception.Message);
    }

    [Fact]
    public void ClientEntityCommand_ReadRejectsUnsupportedCommandType()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var entityCommandManager = new EntityCommandManager();
        entityCommandManager.Initialise();

        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(entityCommandManager)
            .BuildServiceProvider();

        try
        {
            byte[] packetData;
            using var stream = new MemoryStream();
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write(123u);
                writer.Write(1u);
                writer.Write(31u, 5u);
                writer.FlushBits();
                packetData = stream.ToArray();
            }

            using var reader = new GamePacketReader(new MemoryStream(packetData));

            InvalidPacketValueException exception = Assert.Throws<InvalidPacketValueException>(() => new ClientEntityCommand().Read(reader));

            Assert.Contains("Unsupported entity command", exception.Message);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ClientEntityCommand_ReadsTimeCountAndSetTimePayload()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        var entityCommandManager = new EntityCommandManager();
        entityCommandManager.Initialise();

        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(entityCommandManager)
            .BuildServiceProvider();

        try
        {
            byte[] packetData = WritePacket(writer =>
            {
                writer.Write(0x11223344u);
                writer.Write(1u);
                writer.Write((uint)EntityCommand.SetTime, 5u);
                writer.Write(0x55667788u);
            });

            ClientEntityCommand packet = ReadPacket<ClientEntityCommand>(packetData);

            Assert.Equal(0x11223344u, packet.Time);
            NetworkEntityCommand command = Assert.IsType<NetworkEntityCommand>(Assert.Single(packet.Commands));
            Assert.Equal(EntityCommand.SetTime, command.Command);

            var setTime = Assert.IsType<SetTimeCommand>(command.Model);
            Assert.Equal(0x55667788u, setTime.Time);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ServerEntityCommand_WritesGuidTimeFlagsAndSetTimePayload()
    {
        byte[] packetData = WritePacket(new ServerEntityCommand
        {
            Guid             = 0x01020304u,
            Time             = 0x11223344u,
            TimeReset        = true,
            ServerControlled = false,
            Commands =
            [
                new NetworkEntityCommand
                {
                    Command = EntityCommand.SetTime,
                    Model = new SetTimeCommand
                    {
                        Time = 0x55667788u
                    }
                }
            ]
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));

        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.Equal(1u, reader.ReadByte(5u));
        Assert.Equal(EntityCommand.SetTime, reader.ReadEnum<EntityCommand>(5));
        Assert.Equal(0x55667788u, reader.ReadUInt());
    }

    [Fact]
    public void SetPositionMultiSplineCommand_ReadRoundTripsPackedSpeedAndFloatFields()
    {
        var command = new SetPositionMultiSplineCommand
        {
            SplineIds = [1u, 2u],
            Speed = 2f,
            Position = 3.25f,
            TakeoffLocationHeight = 4.5f,
            LandingLocationHeight = 5.75f
        };

        SetPositionMultiSplineCommand roundTripped = ReadPacket<SetPositionMultiSplineCommand>(WritePacket(command));

        Assert.Equal(command.SplineIds, roundTripped.SplineIds);
        Assert.Equal(command.Speed, roundTripped.Speed);
        Assert.Equal(command.Position, roundTripped.Position);
        Assert.Equal(command.TakeoffLocationHeight, roundTripped.TakeoffLocationHeight);
        Assert.Equal(command.LandingLocationHeight, roundTripped.LandingLocationHeight);
    }

    [Fact]
    public void SetRotationSplineCommand_ReadRoundTripsFloatPosition()
    {
        var command = new SetRotationSplineCommand
        {
            SplineId = 3u,
            Speed = 4,
            Position = 7.5f
        };

        SetRotationSplineCommand roundTripped = ReadPacket<SetRotationSplineCommand>(WritePacket(command));

        Assert.Equal(command.Position, roundTripped.Position);
    }

    [Fact]
    public void SetPositionProjectileCommand_ReadRoundTripsFloatGravity()
    {
        var command = new SetPositionProjectileCommand
        {
            Gravity = 9.75f
        };

        SetPositionProjectileCommand roundTripped = ReadPacket<SetPositionProjectileCommand>(WritePacket(command));

        Assert.Equal(command.Gravity, roundTripped.Gravity);
    }

    [Fact]
    public void ServerCharacterListCharacter_WriteRejectsMismatchedCustomisationLabelsAndValues()
    {
        var character = new ServerCharacterList.Character
        {
            Id = 1ul,
            Name = "Tester"
        };
        character.Labels.Add(1u);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WritePacket(character));

        Assert.Contains("Labels", exception.Message);
    }

    [Fact]
    public void ServerCharacterListEntry_WriteUsesCharacterRowPayload()
    {
        var character = new ServerCharacterList.Character
        {
            Id = 1ul,
            Name = "Tester"
        };

        byte[] entryPayload = WritePacket(new ServerCharacterListEntry
        {
            Character = character
        });

        Assert.Equal(WritePacket(character), entryPayload);
    }

    [Fact]
    public void ClientStatisticsConnection_ReadsPackedUnitCountAndFlag()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(10u);
            writer.Write(20u);
            writer.Write(30u);
            writer.Write((40u << 1) | 1u);
        });

        ClientStatisticsConnection packet = ReadPacket<ClientStatisticsConnection>(packetData);

        Assert.Equal(10u, packet.AverageRoundTripInMs);
        Assert.Equal(20u, packet.BytesReceivedPerSecond);
        Assert.Equal(30u, packet.BytesSentPerSecond);
        Assert.Equal(40u, packet.UnitHashTableEntryCount);
        Assert.True(packet.Unknown);
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

    private static byte[] WritePacket(IWritable packet)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            packet.Write(writer);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            write(writer);
            writer.FlushBits();
        }

        return stream.ToArray();
    }

    private static T ReadPacket<T>(byte[] packetData)
        where T : IReadable, new()
    {
        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var packet = new T();
        packet.Read(reader);

        return packet;
    }
}
