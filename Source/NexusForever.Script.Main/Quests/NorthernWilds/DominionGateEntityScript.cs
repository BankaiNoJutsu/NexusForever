using NexusForever.Game.Abstract.Entity;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    [ScriptFilterCreatureId(12653u)]
    public class DominionGateEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private const uint IcefuryGateCreatureId = 16799u;
        private static readonly TimeSpan closeDelay = TimeSpan.FromSeconds(10d);

        private ICreatureEntity owner;
        private IDoorEntity pendingCloseDoor;
        private TimeSpan pendingCloseTime;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (owner == null)
                return;

            IDoorEntity door = owner.GetVisibleCreature<IDoorEntity>(IcefuryGateCreatureId).FirstOrDefault();
            if (door == null || door.IsOpen)
                return;

            door.OpenDoor();
            pendingCloseDoor = door;
            pendingCloseTime = closeDelay;
        }

        public void Update(double lastTick)
        {
            if (pendingCloseDoor == null)
                return;

            pendingCloseTime -= TimeSpan.FromSeconds(lastTick);
            if (pendingCloseTime > TimeSpan.Zero)
                return;

            if (pendingCloseDoor.IsOpen)
                pendingCloseDoor.CloseDoor();

            pendingCloseDoor = null;
            pendingCloseTime = TimeSpan.Zero;
        }
    }
}
