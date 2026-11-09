using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Script.Template.Event
{
    public class EntityYellEvent : IEntityYellEvent
    {
        private IWorldEntity owner;
        private string message;

        public void Initialise(IWorldEntity owner, string message)
        {
            this.owner   = owner;
            this.message = message;
        }

        public void Invoke()
        {
            owner.NpcYell(message);
        }
    }
}
