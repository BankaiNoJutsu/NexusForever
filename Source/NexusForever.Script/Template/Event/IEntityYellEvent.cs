using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Script.Template.Event
{
    public interface IEntityYellEvent : IScriptEvent
    {
        void Initialise(IWorldEntity owner, string message);
    }
}
