using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Main.AI;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.ColdbloodCitadel.Script
{
    /// <summary>
    /// Build 16042 maps the Iceblood Coven objective to Darksister Golag, Katla,
    /// and Ulfrid. Credit the objective once all three Creature2 rows have died;
    /// ability choreography and resurrection timing remain blocked pending smoke.
    /// </summary>
    [ScriptFilterCreatureId(75472u, 75473u, 75474u)]
    public class IcebloodCovenEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private const uint DarksisterGolag = 75472u;
        private const uint DarksisterKatla = 75473u;
        private const uint DarksisterUlfrid = 75474u;

        private static readonly ConditionalWeakTable<IBaseMap, CovenKillState> covenKills = new();

        private ICreatureEntity entity;

        public void OnLoad(ICreatureEntity owner)
        {
            entity = owner;
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public void OnDeath()
        {
            if (!IsCovenCreature(entity.CreatureId))
                return;

            IBaseMap map = entity.Map;
            CovenKillState state = covenKills.GetOrCreateValue(map);
            if (!state.TryMarkDefeated(entity.CreatureId))
                return;

            covenKills.Remove(map);
            map.PublicEventManager.UpdateObjective(PublicEventObjective.DefeatTheIcebloodCoven, 1);
        }

        private static bool IsCovenCreature(uint creatureId)
        {
            return creatureId is DarksisterGolag or DarksisterKatla or DarksisterUlfrid;
        }

        private sealed class CovenKillState
        {
            private readonly HashSet<uint> defeatedCreatureIds = [];
            private bool objectiveCredited;

            public bool TryMarkDefeated(uint creatureId)
            {
                lock (defeatedCreatureIds)
                {
                    if (objectiveCredited)
                        return false;

                    defeatedCreatureIds.Add(creatureId);
                    if (!defeatedCreatureIds.Contains(DarksisterGolag) ||
                        !defeatedCreatureIds.Contains(DarksisterKatla) ||
                        !defeatedCreatureIds.Contains(DarksisterUlfrid))
                    {
                        return false;
                    }

                    objectiveCredited = true;
                    return true;
                }
            }
        }
    }

    /// <summary>
    /// Build 16042 Creature2 75459 is Harizog Coldblood, the final Coldblood
    /// Citadel boss tied to public-event objective 5315. Spell4 rows 87944 and
    /// 87945 are the mapped Harizog auto attacks.
    /// </summary>
    [ScriptFilterCreatureId(75459u)]
    public class HarizogColdbloodEntityScript : CombatAI
    {
        private bool defeated;

        public HarizogColdbloodEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager)
        {
        }

        /// <summary>
        /// Invoked when <see cref="IScript"/> is loaded.
        /// </summary>
        public override void OnLoad(ICreatureEntity owner)
        {
            base.OnLoad(owner);

            autoAttacks = [87944, 87945];
        }

        /// <summary>
        /// Invoked when <see cref="IUnitEntity"/> is killed.
        /// </summary>
        public override void OnDeath()
        {
            if (defeated)
                return;

            defeated = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.DefeatTheRisenHarizog, 1);
        }
    }
}
