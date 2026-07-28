using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Housing
{
    [ScriptFilterCreatureId(65852u, 70052u, 75398u)]
    public class HousingDoorEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const StandState DoorClosed = StandState.State0;
        private const StandState DoorOpen   = StandState.State1;

        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
            owner.SetStandState(DoorClosed);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator == null || owner == null)
                return;

            StandState nextState = owner.StandState == DoorOpen
                ? DoorClosed
                : DoorOpen;

            owner.SetStandState(nextState);
        }
    }
}
