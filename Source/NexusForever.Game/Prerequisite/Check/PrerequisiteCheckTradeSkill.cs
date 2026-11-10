using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 175: evaluator case <c>0xaf</c> of <c>PrerequisiteManager_EvaluateTypeSlot</c> (<c>1404a2100</c>) calls manager vtable <c>+0x3e0</c> (<c>14049f190</c>) with the comparison and
    /// <b>ObjectId</b>. It walks the local player's tradeskill list (global <c>+0x1698</c> / <c>+0x16a0</c>, the list <c>Lua_Crafting_GetKnownTradeskills</c> reads) and, for every entry whose
    /// field <c>+0xc</c> is non-zero, searches the ten ids at <c>+0x18</c> for ObjectId. Equal returns "found", NotEqual its negation, any other comparison false. Value is never read.
    /// </summary>
    /// <remarks>
    /// The ten ids are the talent picked in each talent tier: the entry layout (id, active flag, ten ids) is the server's own tradeskill state, and the one retail slot has ObjectId 153, a
    /// TradeskillBonus row that is one of the three choices (152, 153, 154) of TradeskillTalentTier 42. Read as "the player has picked tradeskill talent ObjectId".
    /// The previous implementation read ObjectId as a tradeskill id and compared its tier rank with Value (0). The retail row (29117) has no referencing row.
    /// Status: Implemented from the native body; the meaning of the ten ids is inferred from the layout and the table match. No retail parity claim.
    /// </remarks>
    [PrerequisiteCheck(PrerequisiteType.TradeSkill)]
    public class PrerequisiteCheckTradeSkill : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
            => PrerequisiteCompare.CompareFlag(comparison, player != null && player.HasTradeskillTalent(objectId));
    }
}
