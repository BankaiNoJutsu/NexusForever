using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.CrimsonIsle
{
    [ScriptFilterCreatureId(24215u)]
    public class Q5584TrappedAssistantEntityScript : IWorldEntityScript, IOwnedScript<ICreatureEntity>
    {
        private ICreatureEntity owner;

        public void OnLoad(ICreatureEntity owner)
        {
            this.owner = owner;
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            // WIP/GUESSED: Questing-and-more has no quest-state guard here; exact retail activation gating is not live-smoked.
            owner.StandState = StandState.State2;
            owner.ModifyHealth(owner.MaxHealth, DamageType.Physical, null);
        }
    }
}
