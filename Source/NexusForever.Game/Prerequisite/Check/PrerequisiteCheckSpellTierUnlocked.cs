using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 233: live handler <c>14049d820</c> resolves <c>objectId0</c>
    /// as a Spell4 row and checks whether the current ability-book tier reaches that row's tier index.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.SpellTierUnlocked)]
    public class PrerequisiteCheckSpellTierUnlocked : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckSpellTierUnlocked(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            Spell4Entry entry = gameTableManager.Spell4.GetEntry(objectId);
            if (entry == null)
                return false;

            uint tier = SpellPrerequisiteHelper.GetSpellTierRank(player, objectId, gameTableManager);
            bool unlocked = tier >= entry.TierIndex;
            return comparison switch
            {
                PrerequisiteComparison.Equal    => unlocked,
                PrerequisiteComparison.NotEqual => !unlocked,
                _                               => false
            };
        }
    }
}
