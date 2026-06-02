using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 59: live <c>Prerequisite_CheckSpellTier</c> (<c>1404a4f60</c>),
    /// handler table <c>14049e820</c>. <c>objectId0</c> is Spell4 id; <c>value0</c> is tier rank.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellTier)]
    public class PrerequisiteCheckSpellTier : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpellTier(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            return SpellPrerequisiteHelper.MeetsSpellTier(player, comparison, value, objectId, gameTableManager);
        }
    }
}
