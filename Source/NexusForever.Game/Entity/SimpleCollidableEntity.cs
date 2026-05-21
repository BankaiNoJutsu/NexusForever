using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Script;

namespace NexusForever.Game.Entity
{
    internal class SimpleCollidableEntity : WorldEntity, ISimpleCollidableEntity
    {
        public override EntityType Type => EntityType.SimpleCollidable;

        #region Dependency Injection

        public SimpleCollidableEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public override void Initialise(EntityModel model)
        {
            base.Initialise(model);

            scriptCollection = ScriptManager.Instance.InitialiseEntityScripts<ISimpleCollidableEntity>(this);
        }

        public override void Initialise(ICreatureInfo creatureInfo)
        {
            base.Initialise(creatureInfo);

            scriptCollection = ScriptManager.Instance.InitialiseEntityScripts<ISimpleCollidableEntity>(this);
        }

        public override void Initialise(ICreatureInfo creatureInfo, EntityModel model)
        {
            base.Initialise(creatureInfo, model);

            scriptCollection = ScriptManager.Instance.InitialiseEntityScripts<ISimpleCollidableEntity>(this);
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new SimpleCollidableEntityModel
            {
                CreatureId        = CreatureId,
                QuestChecklistIdx = QuestChecklistIdx
            };
        }
    }
}
