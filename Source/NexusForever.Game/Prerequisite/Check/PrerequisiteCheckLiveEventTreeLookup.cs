using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.LiveEventTreeLookup)]
    public class PrerequisiteCheckLiveEventTreeLookup : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckLiveEventTreeLookup(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (!LiveEventPrerequisiteProgress.TryGetLiveEventEntry(gameTableManager, objectId, out _))
                return false;

            if (!LiveEventPrerequisiteProgress.RequiresNpcContext(player, parameters))
                return false;

            if (parameters.Target is not IUnitEntity npc)
                return false;

            uint presence = LiveEventPrerequisiteProgress.GetTreeLookupPresence(npc, objectId);
            return PrerequisiteCompare.Compare(comparison, presence, value);
        }
    }
}
