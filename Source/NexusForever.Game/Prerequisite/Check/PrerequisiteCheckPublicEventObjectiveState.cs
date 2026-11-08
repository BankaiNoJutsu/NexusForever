using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 122 (<c>FUN_1404a08f0</c>): true only when the objective's event is active and the objective id is present in a per-unit map (<c>unit+0x3780</c>)
    /// that the client creates empty and never fills (no writer in <c>.text</c>), so the result is constant: Equal false, NotEqual true, every other comparison false.
    /// One retail slot (prerequisite 11645, objective 748). See Decomp/Analysis/PUBLIC_EVENT_NATIVE_READ_20261108.md; an inserter reaching the map through a sub-object
    /// base was not excluded, so this is the faithful dead behaviour until a witness shows otherwise.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PublicEventObjective122)]
    public class PrerequisiteCheckPublicEventObjective122 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, false);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, false);
    }

    /// <summary>
    /// Prerequisite type 200 (<c>FUN_1404a0bf0</c>, 11 retail slots): <c>ObjectId</c> is a public event objective id. When the objective exists and its event is active,
    /// the word is <c>(objective Busy or event Busy ? 1 : 0) &amp; Value</c>, else 0; Equal returns the word as a flag and NotEqual <c>word == 0</c>, any other comparison is false.
    /// Nine of the eleven slots have Value 0, so for them Equal is always false and NotEqual always true.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PublicEventObjective200)]
    public class PrerequisiteCheckPublicEventObjective200 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => Evaluate(player.Map, player.CharacterId, comparison, value, objectId);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => subject is IPlayer player
                ? Meets(player, comparison, value, objectId, parameters)
                : Evaluate(subject?.Map, 0ul, comparison, value, objectId);

        private static bool Evaluate(Abstract.Map.IBaseMap map, ulong characterId, PrerequisiteComparison comparison, uint value, uint objectId)
        {
            uint word = 0u;
            var found = PublicEventPrerequisiteHelper.FindActiveObjective(map, characterId, objectId);
            if (found is { } hit)
                word = (hit.Objective.IsBusy || hit.Event.IsBusy ? 1u : 0u) & value;

            return comparison switch
            {
                PrerequisiteComparison.Equal    => word != 0u,
                PrerequisiteComparison.NotEqual => word == 0u,
                _                               => false
            };
        }
    }

    /// <summary>
    /// Prerequisite type 119 (<c>FUN_1404a0650</c>, 122 retail slots): <c>ObjectId</c> is a public event objective id. A Player or Ghost subject with no target, or with a
    /// Player or Ghost target, gives Equal false / NotEqual true. Otherwise the unit is the target when the subject is a player, else the subject; the parent event must be
    /// active and the unit's <c>QuestChecklistIdx</c> (<c>unit+0x35f8</c>, a signed byte, negative meaning none) must be set; the word is the objective's ObjectiveData
    /// checklist bitfield masked with <c>1 &lt;&lt; (idx &amp; 31)</c>. Equal returns the word as a flag and NotEqual <c>word == 0</c>; Value is unused.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.PublicEventObjectiveObject)]
    public class PrerequisiteCheckPublicEventObjectiveObject119 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => Evaluate(player, parameters.Target as IWorldEntity, player.Map, player.CharacterId, comparison, objectId);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => subject is IPlayer player
                ? Meets(player, comparison, value, objectId, parameters)
                : Evaluate(subject, parameters.Target as IWorldEntity, subject?.Map, 0ul, comparison, objectId);

        private static bool IsPlayerLike(IWorldEntity entity)
            => entity != null && entity.Type is EntityType.Player or EntityType.Ghost;

        private static bool Evaluate(IWorldEntity subject, IWorldEntity target, Abstract.Map.IBaseMap map, ulong characterId,
            PrerequisiteComparison comparison, uint objectId)
        {
            if (subject == null)
                return false;

            bool subjectIsPlayer = IsPlayerLike(subject);
            if (subjectIsPlayer && (target == null || IsPlayerLike(target)))
                return PrerequisiteCompare.CompareFlag(comparison, false);

            IWorldEntity unit = subjectIsPlayer ? target : subject;
            uint word = 0u;
            var found = PublicEventPrerequisiteHelper.FindActiveObjective(map, characterId, objectId);
            if (found is { } hit && unit != null && (sbyte)unit.QuestChecklistIdx >= 0)
                word = hit.Objective.ObjectiveData & (1u << (unit.QuestChecklistIdx & 31));

            return comparison switch
            {
                PrerequisiteComparison.Equal    => word != 0u,
                PrerequisiteComparison.NotEqual => word == 0u,
                _                               => false
            };
        }
    }
}
