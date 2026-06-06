using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;

namespace NexusForever.Game.Prerequisite
{
    public class PrerequisiteParameters : IPrerequisiteParameters
    {
        public IUnitEntity Target { get; set; }
        public IItem Item { get; set; }
        public ushort TaxiNode { get; set; }
        public bool AccountItemContext { get; set; }
    }
}
