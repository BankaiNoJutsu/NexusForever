using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Abstract.Prerequisite
{
    public interface IPrerequisiteParameters
    {
        public IUnitEntity Target { get; set; }

        /// <summary>
        /// Item under evaluation for item-scoped prerequisite types (client item-eval context).
        /// </summary>
        public IItem Item { get; set; }

        /// <summary>
        /// Taxi node supplied by a client rapid-transport cast request.
        /// </summary>
        public ushort TaxiNode { get; set; }
    }
}
