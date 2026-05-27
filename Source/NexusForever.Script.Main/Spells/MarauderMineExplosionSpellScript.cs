using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Spells
{
    [ScriptFilterOwnerId(26443u)]
    public class MarauderMineExplosionSpellScript : ISpellScript, IOwnedScript<ISpell>
    {
        private static readonly HashSet<uint> MineCreatureIds = new()
        {
            16718u,
            24251u
        };

        private ISpell owner;

        public void OnLoad(ISpell owner)
        {
            this.owner = owner;
        }

        public void OnFinish(ISpell spell, bool cancelled)
        {
            ISpell activeSpell = spell ?? owner;
            if (cancelled || activeSpell?.Caster is not ICreatureEntity mine || !MineCreatureIds.Contains(mine.CreatureId))
                return;

            mine.ModifyHealth(mine.MaxHealth, DamageType.Physical, null);
        }
    }
}
