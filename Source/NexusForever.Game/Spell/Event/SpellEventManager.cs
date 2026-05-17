using NexusForever.Game.Abstract.Spell.Event;

namespace NexusForever.Game.Spell.Event
{
    public class SpellEventManager : ISpellEventManager
    {
        public bool HasPendingEvent => events.Count != 0;

        private readonly List<ISpellEvent> events = new();

        public void Update(double lastTick)
        {
            foreach (ISpellEvent spellEvent in events.ToArray())
            {
                spellEvent.Update(lastTick);
                if (spellEvent.Delay <= 0d)
                {
                    events.Remove(spellEvent);
                    spellEvent.Callback.Invoke();
                }
            }
        }

        public void EnqueueEvent(ISpellEvent spellEvent)
        {
            events.Add(spellEvent);
        }

        public void CancelEvent(ISpellEvent spellEvent)
        {
            events.Remove(spellEvent);
        }

        public void CancelEvents()
        {
            events.Clear();
        }
    }
}
