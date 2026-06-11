using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.Script;

namespace NexusForever.Game.Entity
{
    public class EntityFactory : IEntityFactory
    {
        #region Dependency Injection

        private readonly IServiceProvider serviceProvider;

        public EntityFactory(
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        #endregion

        /// <summary>
        /// Create a new entity of type <typeparamref name="T"/>.
        /// </summary>
        public T CreateEntity<T>() where T : IGridEntity
        {
            T entity = serviceProvider.GetRequiredService<T>();
            InitialiseRuntimeDependencies(entity);
            return entity;
        }

        /// <summary>
        /// Create a new <see cref="IWorldEntity"/> of supplied <see cref="EntityType"/>.
        /// </summary>
        /// <remarks>
        /// This should only be used for creating entities from the database, otherwise use <see cref="CreateEntity{T}"/>.
        /// </remarks>
        public IWorldEntity CreateWorldEntity(EntityType type)
        {
            IWorldEntity entity = type switch
            {
                EntityType.NonPlayer          => serviceProvider.GetRequiredService<INonPlayerEntity>(),
                EntityType.Chest              => serviceProvider.GetRequiredService<IChestEntity>(),
                EntityType.Destructible       => serviceProvider.GetRequiredService<IDestructibleEntity>(),
                EntityType.Vehicle            => serviceProvider.GetRequiredService<IVehicleEntity>(),
                EntityType.Door               => serviceProvider.GetRequiredService<IDoorEntity>(),
                EntityType.HarvestUnit        => serviceProvider.GetRequiredService<IHarvestUnitEntity>(),
                EntityType.CorpseUnit         => serviceProvider.GetRequiredService<ICorpseUnitEntity>(),
                EntityType.Mount              => serviceProvider.GetRequiredService<IMountEntity>(),
                EntityType.CollectableUnit    => serviceProvider.GetRequiredService<ICollectableUnitEntity>(),
                EntityType.Taxi               => serviceProvider.GetRequiredService<ITaxiEntity>(),
                EntityType.Simple             => serviceProvider.GetRequiredService<ISimpleEntity>(),
                EntityType.Platform           => serviceProvider.GetRequiredService<IPlatformEntity>(),
                EntityType.MailBox            => serviceProvider.GetRequiredService<IMailboxEntity>(),
                EntityType.AiTurret           => serviceProvider.GetRequiredService<IAiTurretEntity>(),
                EntityType.InstancePortal     => serviceProvider.GetRequiredService<IInstancePortalEntity>(),
                EntityType.Plug               => serviceProvider.GetRequiredService<IPlugEntity>(),
                EntityType.Residence          => serviceProvider.GetRequiredService<IResidenceEntity>(),
                EntityType.StructuredPlug     => serviceProvider.GetRequiredService<IStructuredPlugEntity>(),
                EntityType.PinataLoot         => serviceProvider.GetRequiredService<IPinataLootEntity>(),
                EntityType.BindPoint          => serviceProvider.GetRequiredService<IBindPointEntity>(),
                EntityType.Hidden             => serviceProvider.GetRequiredService<IHiddenEntity>(),
                EntityType.Trigger            => serviceProvider.GetRequiredService<ITriggerEntity>(),
                EntityType.Ghost              => serviceProvider.GetRequiredService<IGhostEntity>(),
                EntityType.Pet                => serviceProvider.GetRequiredService<IPetEntity>(),
                EntityType.EsperPet           => serviceProvider.GetRequiredService<IEsperPetEntity>(),
                EntityType.WorldUnit          => serviceProvider.GetRequiredService<IWorldUnitEntity>(),
                EntityType.ScannerUnit        => serviceProvider.GetRequiredService<IScannerUnitEntity>(),
                EntityType.Camera             => serviceProvider.GetRequiredService<ICameraEntity>(),
                EntityType.Trap               => serviceProvider.GetRequiredService<ITrapEntity>(),
                EntityType.DestructibleDoor   => serviceProvider.GetRequiredService<IDestructibleDoorEntity>(),
                EntityType.Pickup             => serviceProvider.GetRequiredService<IPickupEntity>(),
                EntityType.SimpleCollidable   => serviceProvider.GetRequiredService<ISimpleCollidableEntity>(),
                EntityType.HousingMannequin   => serviceProvider.GetRequiredService<IHousingMannequinEntity>(),
                EntityType.HousingHarvestPlug => serviceProvider.GetRequiredService<IHousingHarvestPlugEntity>(),
                EntityType.HousingPlant       => serviceProvider.GetRequiredService<IHousingPlantEntity>(),
                EntityType.Lockbox            => serviceProvider.GetRequiredService<ILockboxEntity>(),
                EntityType.Player             => throw new InvalidOperationException("Player entities are created from character state and cannot be spawned from static world data."),
                _                             => throw new InvalidOperationException($"Unsupported world entity type {type}.")
            };

            InitialiseRuntimeDependencies(entity);
            return entity;
        }

        private void InitialiseRuntimeDependencies(IGridEntity entity)
        {
            if (entity is GridEntity gridEntity)
                gridEntity.InitialiseScriptManager(() => serviceProvider.GetService<IScriptManager>());

            if (entity is not WorldEntity worldEntity)
                return;

            worldEntity.InitialiseRuntimeDependencies(
                () => serviceProvider.GetService<IEntitySummonFactory>(),
                () => serviceProvider.GetService<ICreatureInfoManager>(),
                () => serviceProvider.GetService<IFactionManager>(),
                () => serviceProvider.GetService<IGameTableManager>());

            if (entity is UnitEntity unitEntity)
                unitEntity.InitialiseRuntimeDependencies(
                    () => serviceProvider.GetService<IGlobalLootManager>(),
                    () => serviceProvider.GetService<ITradeManager>(),
                    () => serviceProvider.GetService<IDisableManager>(),
                    () => serviceProvider.GetService<IAssetManager>(),
                    () => serviceProvider.GetService<IPrerequisiteManager>(),
                    () => serviceProvider.GetService<IGlobalSpellManager>());
        }
    }
}
