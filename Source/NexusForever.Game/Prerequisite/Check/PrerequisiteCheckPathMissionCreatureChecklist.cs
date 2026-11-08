using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 107 (<c>FUN_14049fff0</c>, 124 retail slots; the enum name <c>QuestObjective7</c> is misleading): <c>ObjectId</c> is a PathMission id. A Player or Ghost
    /// subject with no target, or with a Player or Ghost target, gives Equal false / NotEqual true. Otherwise the unit is the target creature; a mission the player does not have, or a
    /// creature whose <c>QuestChecklistIdx</c> (<c>unit+0x35f8</c>, a signed byte: negative means none) is unset, gives word 0; else the word is the mission's progress word
    /// (<c>ProgressCount</c> of <c>ServerPathMissionUpdate</c>, the field behind the mission object's vtable <c>+0x40</c>) masked with <c>1 &lt;&lt; (idx &amp; 31)</c>. Equal returns the word as a
    /// flag and NotEqual <c>word == 0</c>; Value is not read. The client word is <c>ProgressCount</c>, not <c>ProgressData</c> (a 0/1 in-progress marker on the server).
    /// The server writes checklist bitmasks into <c>ProgressCount</c> for no mission type yet, so for mission types 5, 6, 13, 14 and 17 (97 of the 124 slots) the word is 0 until
    /// those producers exist. See Decomp/Analysis/PREREQUISITE_107_PATH_MISSION_WORD_20261108.md.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.QuestObjective7)]
    public class PrerequisiteCheckPathMissionCreatureChecklist : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            IWorldEntity target = parameters.Target as IWorldEntity;
            if (target == null || IsPlayerLike(target))
                return PrerequisiteCompare.CompareFlag(comparison, false);

            uint word = 0u;
            if ((sbyte)target.QuestChecklistIdx >= 0
                && player.PathManager != null
                && player.PathManager.TryGetMissionProgress(objectId, out uint progress))
                word = progress & (1u << (target.QuestChecklistIdx & 31));

            return comparison switch
            {
                PrerequisiteComparison.Equal    => word != 0u,
                PrerequisiteComparison.NotEqual => word == 0u,
                _                               => false
            };
        }

        private static bool IsPlayerLike(IWorldEntity entity)
            => entity.Type is EntityType.Player or EntityType.Ghost;
    }
}
