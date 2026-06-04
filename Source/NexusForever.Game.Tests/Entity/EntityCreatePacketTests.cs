using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.Network;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Entity;

public class EntityCreatePacketTests
{
    public static TheoryData<EntityType, Type> CreatePacketEntities => new()
    {
        { EntityType.AiTurret, typeof(AiTurretEntityModel) },
        { EntityType.BindPoint, typeof(BindpointEntityModel) },
        { EntityType.Camera, typeof(CameraEntityModel) },
        { EntityType.Chest, typeof(ChestEntityModel) },
        { EntityType.CollectableUnit, typeof(CollectableUnitEntityModel) },
        { EntityType.CorpseUnit, typeof(CorpseEntityModel) },
        { EntityType.Destructible, typeof(DestructibleEntityModel) },
        { EntityType.DestructibleDoor, typeof(DestructibleDoorEntityModel) },
        { EntityType.Door, typeof(DoorEntityModel) },
        { EntityType.EsperPet, typeof(EsperPetEntityModel) },
        { EntityType.HarvestUnit, typeof(HarvestUnitEntityModel) },
        { EntityType.Hidden, typeof(HiddenEntityModel) },
        { EntityType.HousingHarvestPlug, typeof(HousingHarvestPlugEntityModel) },
        { EntityType.HousingMannequin, typeof(HousingMannequinEntityModel) },
        { EntityType.HousingPlant, typeof(HousingPlantEntityModel) },
        { EntityType.InstancePortal, typeof(InstancePortalEntityModel) },
        { EntityType.Lockbox, typeof(LockboxEntityModel) },
        { EntityType.MailBox, typeof(MailboxEntityModel) },
        { EntityType.NonPlayer, typeof(NonPlayerEntityModel) },
        { EntityType.Pickup, typeof(PickupEntityModel) },
        { EntityType.PinataLoot, typeof(PinataLootEntityModel) },
        { EntityType.Platform, typeof(PlatformEntityModel) },
        { EntityType.Residence, typeof(ResidenceEntityModel) },
        { EntityType.ScannerUnit, typeof(ScannerUnitEntityModel) },
        { EntityType.Simple, typeof(SimpleEntityModel) },
        { EntityType.SimpleCollidable, typeof(SimpleCollidableEntityModel) },
        { EntityType.StructuredPlug, typeof(StructuredPlugEntityModel) },
        { EntityType.Taxi, typeof(TaxiEntityModel) },
        { EntityType.Trap, typeof(TrapEntityModel) },
        { EntityType.Trigger, typeof(TriggerEntityModel) },
        { EntityType.WorldUnit, typeof(WorldUnitEntityModel) }
    };

    public static TheoryData<EntityType> SocketAwareEntities => new()
    {
        EntityType.StructuredPlug,
        EntityType.HousingHarvestPlug,
        EntityType.HousingPlant
    };

    public static TheoryData<EntityType> SummonerOwnedEntities => new()
    {
        EntityType.EsperPet,
        EntityType.Pickup,
        EntityType.ScannerUnit,
        EntityType.Trap
    };

    [Theory]
    [MemberData(nameof(CreatePacketEntities))]
    public void EntityFactoryCreateWorldEntity_UsesExpectedEntityTypeAndModel(EntityType entityType, Type expectedModelType)
    {
        using ServiceProvider provider = BuildProvider();
        IEntityFactory entityFactory = provider.GetRequiredService<IEntityFactory>();
        IWorldEntity entity = entityFactory.CreateWorldEntity(entityType);

        var packet = entity.BuildCreatePacket(false);

        Assert.Equal(entityType, packet.Type);
        Assert.IsType(expectedModelType, packet.EntityModel);
    }

    [Fact]
    public void EntityFactoryCreateWorldEntity_RejectsStaticPlayerEntity()
    {
        using ServiceProvider provider = BuildProvider();
        IEntityFactory entityFactory = provider.GetRequiredService<IEntityFactory>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => entityFactory.CreateWorldEntity(EntityType.Player));

        Assert.Contains("Player entities are created from character state", exception.Message);
    }

    [Theory]
    [MemberData(nameof(SocketAwareEntities))]
    public void BuildCreatePacket_UsesWorldSocketIdForSocketAwareModels(EntityType entityType)
    {
        const ushort worldSocketId = 321;

        using ServiceProvider provider = BuildProvider();
        IEntityFactory entityFactory = provider.GetRequiredService<IEntityFactory>();
        IWorldEntity entity = entityFactory.CreateWorldEntity(entityType);

        SetWorldSocketId(entity, worldSocketId);

        var packet = entity.BuildCreatePacket(false);

        Assert.Equal(1, packet.WorldPlacementData.Type);
        Assert.Equal(worldSocketId, packet.WorldPlacementData.SocketId);
        Assert.Equal(worldSocketId, GetSocketId(packet.EntityModel));
    }

    [Theory]
    [MemberData(nameof(SummonerOwnedEntities))]
    public void BuildCreatePacket_UsesSummonerGuidForOwnerAwareModels(EntityType entityType)
    {
        const uint ownerId = 789u;

        using ServiceProvider provider = BuildProvider();
        IEntityFactory entityFactory = provider.GetRequiredService<IEntityFactory>();
        IWorldEntity entity = entityFactory.CreateWorldEntity(entityType);

        entity.SummonerGuid = ownerId;

        var packet = entity.BuildCreatePacket(false);

        Assert.Equal(ownerId, GetOwnerId(packet.EntityModel));
    }

    [Fact]
    public void EsperPetEntityModel_WriteMatchesNativeBranchWithoutTrailingName()
    {
        var model = new EsperPetEntityModel
        {
            CreatureId = 0x12345u,
            OwnerId = 0xAABBCCDDu,
            OwnerDisplayItemId = 0x3456
        };

        byte[] packetData = WriteModel(model);

        Assert.Equal(9, packetData.Length);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x12345u, reader.ReadUInt(18u));
        Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
        Assert.Equal((ushort)0x3456, reader.ReadUShort(15u));
    }

    [Fact]
    public void TaxiEntityModel_WriteOmitsOwnerBeforePassengerCount()
    {
        var model = new TaxiEntityModel
        {
            CreatureId = 0x12345u,
            UnitVehicleId = 0x2345,
            Passengers =
            {
                new TaxiEntityModel.Passenger
                {
                    SeatType = 2,
                    SeatPosition = 5,
                    UnitId = 0xCAFEBABEu
                }
            }
        };

        byte[] packetData = WriteModel(model);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x12345u, reader.ReadUInt(18u));
        Assert.Equal((ushort)0x2345, reader.ReadUShort(14u));
        Assert.Equal((byte)1, reader.ReadByte(3u));
        Assert.Equal((byte)2, reader.ReadByte(2u));
        Assert.Equal((byte)5, reader.ReadByte(3u));
        Assert.Equal(0xCAFEBABEu, reader.ReadUInt());
    }

    [Fact]
    public void SetQuestChecklistIndex_UsesIndexInSimpleCollidableCreateModel()
    {
        using ServiceProvider provider = BuildProvider();
        IEntityFactory entityFactory = provider.GetRequiredService<IEntityFactory>();
        IWorldEntity entity = entityFactory.CreateWorldEntity(EntityType.SimpleCollidable);

        entity.SetQuestChecklistIndex(7);

        var packet = entity.BuildCreatePacket(false);
        var model = Assert.IsType<SimpleCollidableEntityModel>(packet.EntityModel);
        Assert.Equal(7, model.QuestChecklistIdx);
    }

    [Fact]
    public void ServerEntityCreate_WriteSerializesMappedWorldPlacementAndTailFields()
    {
        var packet = new ServerEntityCreate
        {
            Guid = 0x10203040u,
            Type = EntityType.Simple,
            EntityModel = new SimpleEntityModel
            {
                CreatureId = 0x12345u,
                QuestChecklistIdx = 0x56
            },
            CreateFlags = EntityCreateFlag.Immediate | EntityCreateFlag.NoDeathDelay,
            Time = 0xAABBCCDDu,
            CurrentSpellUniqueId = 0x01020304u,
            Faction1 = (Faction)0x1111,
            Faction2 = (Faction)0x2222,
            UnitTagOwner = 0x33445566u,
            GroupTagOwner = 0x1122334455667788ul,
            WorldPlacementData = new ServerEntityCreate.WorldPlacement
            {
                Type = 1,
                ActivePropId = 0x8877665544332211ul,
                SocketId = 0x1234
            },
            MiniMapMarker = 0x2345,
            DisplayInfo = 0x12345,
            OutfitInfo = 0x3456
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x10203040u, reader.ReadUInt());
        Assert.Equal(EntityType.Simple, reader.ReadEnum<EntityType>(6u));
        Assert.Equal(0x12345u, reader.ReadUInt(18u));
        Assert.Equal((byte)0x56, reader.ReadByte());
        Assert.Equal(EntityCreateFlag.Immediate | EntityCreateFlag.NoDeathDelay, reader.ReadEnum<EntityCreateFlag>(8u));
        Assert.Equal((byte)0, reader.ReadByte(5u));
        Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
        Assert.Equal((byte)0, reader.ReadByte(5u));
        Assert.Equal((byte)0, reader.ReadByte());
        Assert.Equal((byte)0, reader.ReadByte(7u));
        Assert.Equal((short)0, reader.ReadShort(9u));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal((Faction)0x1111, reader.ReadEnum<Faction>(14u));
        Assert.Equal((Faction)0x2222, reader.ReadEnum<Faction>(14u));
        Assert.Equal(0x33445566u, reader.ReadUInt());
        Assert.Equal(0x1122334455667788ul, reader.ReadULong());
        Assert.Equal((byte)0, reader.ReadByte(2u));
        Assert.False(reader.ReadBit());
        Assert.Equal((byte)1, reader.ReadByte(2u));
        Assert.Equal(0x8877665544332211ul, reader.ReadULong());
        Assert.Equal((ushort)0x1234, reader.ReadUShort(14u));
        Assert.Equal((byte)0, reader.ReadByte(2u));
        Assert.False(reader.ReadBit());
        Assert.Equal((ushort)0x2345, reader.ReadUShort(14u));
        Assert.Equal(0x12345u, reader.ReadUInt(17u));
        Assert.Equal((ushort)0x3456, reader.ReadUShort(15u));
    }

    internal static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameEntity();
        return services.BuildServiceProvider();
    }

    internal static void SetWorldSocketId(IWorldEntity entity, ushort worldSocketId)
    {
        PropertyInfo propertyInfo = typeof(WorldEntity).GetProperty(nameof(IWorldEntity.WorldSocketId), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new Xunit.Sdk.XunitException($"Unable to find {nameof(IWorldEntity.WorldSocketId)} on {nameof(WorldEntity)}.");
        propertyInfo.SetValue(entity, worldSocketId);
    }

    private static ushort GetSocketId(IEntityModel entityModel)
    {
        return entityModel switch
        {
            StructuredPlugEntityModel model       => model.SocketId,
            HousingHarvestPlugEntityModel model   => model.SocketId,
            HousingPlantEntityModel model         => model.SocketId,
            _                                     => throw new Xunit.Sdk.XunitException($"Unexpected socket-aware model {entityModel.GetType().Name}.")
        };
    }

    private static uint GetOwnerId(IEntityModel entityModel)
    {
        return entityModel switch
        {
            EsperPetEntityModel model    => model.OwnerId,
            PickupEntityModel model      => model.OwnerId,
            ScannerUnitEntityModel model => model.OwnerId,
            TrapEntityModel model        => model.OwnerId,
            _                            => throw new Xunit.Sdk.XunitException($"Unexpected owner-aware model {entityModel.GetType().Name}.")
        };
    }

    private static byte[] WritePacket(ServerEntityCreate message)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        message.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }

    private static byte[] WriteModel(IEntityModel model)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        model.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
