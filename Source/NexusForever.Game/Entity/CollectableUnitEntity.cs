using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Network.World.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    public class CollectableUnitEntity : WorldEntity, ICollectableUnitEntity
    {
        public override EntityType Type => EntityType.CollectableUnit;

        #region Dependency Injection

        public CollectableUnitEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public override void Initialise(ICreatureInfo creatureInfo)
        {
            base.Initialise(creatureInfo);
            scriptCollection = GetScriptManager().InitialiseEntityScripts<ICollectableUnitEntity>(this);
        }

        public override void Initialise(EntityModel model)
        {
            base.Initialise(model);
            scriptCollection = GetScriptManager().InitialiseEntityScripts<ICollectableUnitEntity>(this);
        }

        public override void Initialise(ICreatureInfo creatureInfo, EntityModel model)
        {
            base.Initialise(creatureInfo, model);
            scriptCollection = GetScriptManager().InitialiseEntityScripts<ICollectableUnitEntity>(this);
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new CollectableUnitEntityModel
            {
                CreatureId = CreatureId,
                QuestChecklistIdx = 0
            };
        }
    }
}
