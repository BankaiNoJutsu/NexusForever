using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Abstract.Prerequisite
{
    public interface IPrerequisiteParameters
    {
        /// <summary>
        /// Relationship target. MeetsUnit evaluates its explicit subject; only target-specific
        /// checks read this entity. Existing Meets(IPlayer, ...) target proxies remain compatible.
        /// </summary>
        public IUnitEntity Target { get; set; }

        /// <summary>
        /// Item under evaluation for item-scoped prerequisite types (client item-eval context).
        /// </summary>
        public IItem Item { get; set; }

        /// <summary>
        /// Taxi node supplied by a client rapid-transport cast request.
        /// </summary>
        public ushort TaxiNode { get; set; }

        /// <summary>
        /// Account item claim prerequisite context for accountitem.prerequisiteId rows.
        /// </summary>
        public bool AccountItemContext { get; set; }
    }
}
