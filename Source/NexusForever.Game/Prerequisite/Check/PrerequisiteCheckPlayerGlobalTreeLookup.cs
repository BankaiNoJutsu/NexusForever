using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// NF proxy for handler table[177] <c>Prerequisite_CheckPlayerGlobalTreeLookup177</c>
    /// (<c>1404a1830</c>). Client enum label <c>TrueLevel</c> is unverified; live path walks
    /// <c>PlayerGlobal_LookupObjectIdInTree</c> at global <c>+0x6300</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.TrueLevel)]
    public class PrerequisiteCheckPlayerGlobalTreeLookup : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckPlayerGlobalTreeLookup(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (!PrerequisiteNpcTargetContext.RequiresNpcTarget(player, parameters))
                return false;

            uint presence = PlayerGlobalTreePrerequisiteHelper.GetTreeLookupPresence(player, gameTableManager, objectId);
            return PrerequisiteCompare.Compare(comparison, presence, value);
        }
    }
}
