using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

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
        EntityType.Taxi,
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

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGameEntity();
        return services.BuildServiceProvider();
    }

    private static void SetWorldSocketId(IWorldEntity entity, ushort worldSocketId)
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
            TaxiEntityModel model        => model.OwnerId,
            TrapEntityModel model        => model.OwnerId,
            _                            => throw new Xunit.Sdk.XunitException($"Unexpected owner-aware model {entityModel.GetType().Name}.")
        };
    }
}
