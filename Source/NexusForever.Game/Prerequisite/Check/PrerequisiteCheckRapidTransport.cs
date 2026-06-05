using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 269: live handler table entry <c>0x10d</c> compares the rapid-transport node id
    /// from client cast context with <c>objectId0</c>.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.RapidTransport)]
    public class PrerequisiteCheckRapidTransport : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            uint taxiNode = parameters?.TaxiNode ?? 0u;
            return PrerequisiteCompare.Compare(comparison, taxiNode, objectId);
        }
    }
}
