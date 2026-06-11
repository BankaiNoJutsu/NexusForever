using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Creature;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script;
using NexusForever.Script.Template;

namespace NexusForever.Game.Entity
{
    public class DoorEntity : WorldEntity, IDoorEntity
    {
        public override EntityType Type => EntityType.Door;

        public bool IsOpen => GetStatEnum<StandState>(Stat.StandState) == StandState.State1;

        #region Dependency Injection

        public DoorEntity(IMovementManager movementManager)
            : base(movementManager)
        {
        }

        #endregion

        public override void Initialise(EntityModel model)
        {
            base.Initialise(model);

            scriptCollection = GetScriptManager().InitialiseEntityScripts<IDoorEntity>(this);
            InitialiseDoorState();
        }

        public override void Initialise(ICreatureInfo creatureInfo, EntityModel model)
        {
            base.Initialise(creatureInfo, model);

            scriptCollection = GetScriptManager().InitialiseEntityScripts<IDoorEntity>(this);
            InitialiseDoorState();
        }

        private void InitialiseDoorState()
        {
            SetStat(Stat.StandState, StandState.State0); // Closed on spawn
            SetBaseProperty(Property.BaseHealth, 101f); // Sniffs showed all doors had 101hp for me.
        }

        protected override IEntityModel BuildEntityModel()
        {
            return new DoorEntityModel
            {
                CreatureId = CreatureId
            };
        }

        /// <summary>
        /// Used to open this <see cref="IDoorEntity"/>.
        /// </summary>
        public void OpenDoor()
        {
            SetStat(Stat.StandState, StandState.State1);
            EnqueueToVisible(new ServerEmote
            {
                Guid       = Guid,
                StandState = StandState.State1
            });

            scriptCollection?.Invoke<IDoorEntityScript>(s => s.OnOpenDoor());
        }

        /// <summary>
        /// Used to close this <see cref="IDoorEntity"/>.
        /// </summary>
        public void CloseDoor()
        {
            SetStat(Stat.StandState, StandState.State0);
            EnqueueToVisible(new ServerEmote
            {
                Guid       = Guid,
                StandState = StandState.State0
            });

            scriptCollection?.Invoke<IDoorEntityScript>(s => s.OnDoorClose());
        }
    }
}
