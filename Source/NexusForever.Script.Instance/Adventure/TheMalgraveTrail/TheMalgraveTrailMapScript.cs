using System.Collections.Generic;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Instance.Adventure.TheMalgraveTrail
{
    /// <summary>
    /// Build 16042 MatchingGameMap 9 and 50 map The Malgrave Trail to world
    /// 1181. PE 53 is the adventure parent event and PE 56 is the Exodus
    /// setup route.
    /// </summary>
    [ScriptFilterOwnerId(1181)]
    public class TheMalgraveTrailMapScript : EventBaseContentMapScript
    {
        public override uint PublicEventId => 53u;

        protected override IEnumerable<uint> AdditionalPublicEventIds => [56u];
    }
}
