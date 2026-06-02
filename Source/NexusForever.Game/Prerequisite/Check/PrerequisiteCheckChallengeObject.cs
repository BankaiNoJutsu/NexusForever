using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ChallengeObject)]
    public class PrerequisiteCheckChallengeObject : IPrerequisiteCheck
    {
        private readonly IGameTableManager gameTableManager;

        public PrerequisiteCheckChallengeObject(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (gameTableManager.Challenge.GetEntry(objectId) == null)
                return false;

            uint presence = player.ChallengeManager.IsChallengeActivated((ushort)objectId) ? 1u : 0u;
            return PrerequisiteCompare.Compare(comparison, presence, value);
        }
    }
}
