using System.Reflection;
using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
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

    [Fact]
    public void EntityFactoryCreateWorldEntity_InitialisesRuntimeDependencies()
    {
        const uint creatureId = 8765u;

        var creatureInfo = new TestCreatureInfo(creatureId);
        var creatureInfoManager = new TestCreatureInfoManager(creatureInfo);
        var summonFactory = new TestEntitySummonFactory();
        using ServiceProvider provider = BuildProvider(services =>
        {
            services.AddTransient<ISimpleEntity, TestSimpleEntity>();
            services.AddSingleton<ICreatureInfoManager>(creatureInfoManager);
            services.AddSingleton<IEntitySummonFactory>(summonFactory);
        });

        IEntityFactory entityFactory = provider.GetRequiredService<IEntityFactory>();
        IWorldEntity entity = entityFactory.CreateWorldEntity(EntityType.Simple);

        Assert.Same(summonFactory, entity.SummonFactory);
        Assert.Same(entity, summonFactory.Owner);

        entity.Initialise(creatureId);

        Assert.Same(creatureInfo, entity.CreatureInfo);
        Assert.Equal(creatureId, creatureInfoManager.LastCreatureId);
        Assert.Equal(1, creatureInfoManager.LookupCount);
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

    internal static ServiceProvider BuildProvider(Action<IServiceCollection> configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(RecordingDispatchProxy<IGameTableManager>.Create(out _));
        services.AddGameEntity();
        configure?.Invoke(services);
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

    private sealed class TestSimpleEntity : UnitEntity, ISimpleEntity
    {
        public override EntityType Type => EntityType.Simple;

        public TestSimpleEntity()
            : base(RecordingDispatchProxy<IMovementManager>.Create(out _))
        {
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new SimpleEntityModel();
        }

        public override void Initialise(ICreatureInfo creatureInfo)
        {
            CreatureInfo = creatureInfo;
        }

        protected override float CalculateDefaultProperty(Property property)
        {
            return 0f;
        }
    }

    private sealed class TestCreatureInfoManager(ICreatureInfo creatureInfo) : ICreatureInfoManager
    {
        public uint LastCreatureId { get; private set; }
        public int LookupCount { get; private set; }

        public void Initialise()
        {
        }

        public ICreatureInfo GetCreatureInfo<T>(T creatureId) where T : Enum
        {
            return GetCreatureInfo(Convert.ToUInt32(creatureId));
        }

        public ICreatureInfo GetCreatureInfo(uint creatureId)
        {
            LastCreatureId = creatureId;
            LookupCount++;
            return creatureInfo;
        }
    }

    private sealed class TestCreatureInfo(uint creatureId) : ICreatureInfo
    {
        public Creature2Entry Entry { get; } = new()
        {
            Id        = creatureId,
            FactionId = (uint)Faction.None
        };

        public Creature2DifficultyEntry DifficultyEntry => null;
        public Creature2ArcheTypeEntry ArcheTypeEntry => null;
        public Creature2TierEntry TierEntry => null;
        public Creature2ModelInfoEntry ModelEntry => null;
        public UnitVehicleEntry UnitVehicleEntry => null;
        public PrerequisiteEntry PrerequisiteVisibilityEntry => null;

        public void Initialise(Creature2Entry entry)
        {
            throw new NotSupportedException();
        }

        public void InitialiseOverrides(IEnumerable<CreatureInfoPropertyModel> properties, IEnumerable<CreatureInfoStatModel> stats)
        {
            throw new NotSupportedException();
        }

        public IEnumerable<ICreatureInfoProperty> GetPropertyOverrides()
        {
            return [];
        }

        public IEnumerable<ICreatureInfoStat> GetStatOverrides()
        {
            return [];
        }

        public uint GetLevel()
        {
            return 1u;
        }

        public Creature2DisplayInfoEntry GetDisplayInfoEntry()
        {
            return null;
        }

        public Creature2OutfitInfoEntry GetOutfitInfoEntry()
        {
            return null;
        }
    }

    private sealed class TestEntitySummonFactory : IEntitySummonFactory
    {
        public uint SummonCount => 0u;
        public IWorldEntity Owner { get; private set; }

        public void Initialise(IWorldEntity owner)
        {
            Owner = owner;
        }

        public T Summon<T>(ICreatureInfo creatureInfo, Vector3 position, Vector3 rotation) where T : IWorldEntity
        {
            throw new NotSupportedException();
        }

        public IWorldEntity Summon(ICreatureInfo creatureInfo, Vector3 position, Vector3 rotation)
        {
            throw new NotSupportedException();
        }

        public IWorldEntity Summon(ICreatureInfo creatureInfo, EntityType entityType, Vector3 position, Vector3 rotation)
        {
            throw new NotSupportedException();
        }

        public void TrackSummon(IWorldEntity entity)
        {
            throw new NotSupportedException();
        }

        public void UntrackSummon(IWorldEntity entity)
        {
            throw new NotSupportedException();
        }

        public void Unsummon(uint guid)
        {
            throw new NotSupportedException();
        }

        public void UnsummonCreature<T>(T creatureId) where T : Enum
        {
            throw new NotSupportedException();
        }

        public void UnsummonCreature(uint creatureId)
        {
            throw new NotSupportedException();
        }

        public void Unsummon()
        {
            throw new NotSupportedException();
        }
    }
}
