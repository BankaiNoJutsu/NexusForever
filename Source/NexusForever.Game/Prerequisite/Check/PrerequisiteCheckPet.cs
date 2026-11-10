using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 88: evaluator case <c>0x58</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) calls manager vtable <c>+0x158</c> (<c>14049d470</c>) with the
    /// primary unit, the comparison and the second unit. With no second unit only NotEqual is met. Otherwise the flag is "the id of the primary unit is in the list at second unit
    /// <c>+0x2b8</c> / <c>+0x2c0</c>"; Equal returns the flag and NotEqual its negation. ObjectId and Value are not read.
    /// </summary>
    /// <remarks>
    /// The list was identified from its only writer, <c>140458ec0</c>, called when a unit is created: an entity of type <c>0x19</c> resolves its owner (<c>+0x2b0</c>, the id
    /// <c>Lua_Unit_GetUnitOwner</c> reads) and appends its own id to the owner's list. The flag therefore reads "the primary unit is an entity of type 0x19 owned by the second unit".
    /// Type 0x19 is <see cref="EntityType.EsperPet"/> in the server enum, whose create model sends the summoner as its owner.
    /// The earlier check compared "a vanity pet is visible" with Value, which read the legacy handler table entry (<c>14049f370</c>) rather than the live case.
    /// The one retail row (15631) is not referenced by any table. Evidence tier: native bodies. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.Pet)]
    public class PrerequisiteCheckPet : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => Evaluate(player, parameters?.Target, comparison);

        public bool MeetsUnit(IUnitEntity subject, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => Evaluate(subject, parameters?.Target, comparison);

        internal static bool Evaluate(IWorldEntity primary, IWorldEntity second, PrerequisiteComparison comparison)
        {
            if (second == null)
                return comparison == PrerequisiteComparison.NotEqual;

            bool owned = primary != null
                && primary.Type == EntityType.EsperPet
                && primary.SummonerGuid == second.Guid;

            return PrerequisiteCompare.CompareFlag(comparison, owned);
        }
    }
}
