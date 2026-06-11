using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;

namespace NexusForever.Game.Entity
{
    public class NonPlayerEntity : CreatureEntity, INonPlayerEntity
    {
        public override EntityType Type => EntityType.NonPlayer;

        public IVendorInfo VendorInfo { get; private set; }

        #region Dependency Injection

        public NonPlayerEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public override void Initialise(EntityModel model)
        {
            base.Initialise(model);

            InitialiseVendor(model);
        }

        public override void Initialise(ICreatureInfo creatureInfo, EntityModel model)
        {
            base.Initialise(creatureInfo, model);

            InitialiseVendor(model);
        }

        private void InitialiseVendor(EntityModel model)
        {
            if (model.EntityVendor != null)
            {
                CreateFlags |= EntityCreateFlag.HasInteractionPrereq;
                VendorInfo = new VendorInfo(model);
            }
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new NonPlayerEntityModel
            {
                CreatureId = CreatureId,
                QuestChecklistIdx = 0
            };
        }

        /// <summary>
        /// Calculate default property value for supplied <see cref="Property"/>.
        /// </summary>
        /// <remarks>
        /// Default property values are not sent to the client, they are also calculated by the client and are replaced by any property updates.
        /// </remarks>
        protected override float CalculateDefaultProperty(Property property)
        {
            float value = base.CalculateDefaultProperty(property);

            Creature2ArcheTypeEntry archeTypeEntry = CreatureInfo?.ArcheTypeEntry
                ?? GetGameTableManager().Creature2ArcheType.GetEntry(CreatureEntry?.Creature2ArcheTypeId ?? 0u);
            if (archeTypeEntry != null)
                value *= archeTypeEntry.UnitPropertyMultiplier[(uint)property];

            Creature2DifficultyEntry difficultyEntry = CreatureInfo?.DifficultyEntry
                ?? GetGameTableManager().Creature2Difficulty.GetEntry(CreatureEntry?.Creature2DifficultyId ?? 0u);
            if (difficultyEntry != null)
                value *= difficultyEntry.UnitPropertyMultiplier[(uint)property];

            Creature2TierEntry tierEntry = CreatureInfo?.TierEntry
                ?? GetGameTableManager().Creature2Tier.GetEntry(CreatureEntry?.Creature2TierId ?? 0u);
            if (tierEntry != null)
                value *= tierEntry.UnitPropertyMultiplier[(uint)property];

            return value;
        }
    }
}
