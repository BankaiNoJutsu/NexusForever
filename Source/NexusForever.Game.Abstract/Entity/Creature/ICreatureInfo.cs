using NexusForever.Database.World.Model;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Abstract.Entity.Creature
{
    public interface ICreatureInfo
    {
        Creature2Entry Entry { get; }
        Creature2DifficultyEntry DifficultyEntry { get; }
        Creature2ArcheTypeEntry ArcheTypeEntry { get; }
        Creature2TierEntry TierEntry { get; }
        Creature2ModelInfoEntry ModelEntry { get; }
        UnitVehicleEntry UnitVehicleEntry { get; }
        PrerequisiteEntry PrerequisiteVisibilityEntry { get; }

        /// <summary>
        /// Initialise <see cref="ICreatureInfo"/> with supplied <see cref="Creature2Entry"/>.
        /// </summary>
        void Initialise(Creature2Entry entry);

        /// <summary>
        /// Initialise <see cref="ICreatureInfo"/> with supplied override information.
        /// </summary>
        void InitialiseOverrides(IEnumerable<CreatureInfoPropertyModel> properties, IEnumerable<CreatureInfoStatModel> stats);

        /// <summary>
        /// Return all properties that override the default values.
        /// </summary>
        IEnumerable<ICreatureInfoProperty> GetPropertyOverrides();

        /// <summary>
        /// Return all stats that override the default values.
        /// </summary>
        IEnumerable<ICreatureInfoStat> GetStatOverrides();

        /// <summary>
        /// Return a random level between the minimum and maximum level of the creature.
        /// </summary>
        uint GetLevel();

        /// <summary>
        /// Return a random <see cref="Creature2DisplayInfoEntry"/> from the display group.
        /// </summary>
        Creature2DisplayInfoEntry GetDisplayInfoEntry();

        /// <summary>
        /// Return a random <see cref="Creature2OutfitInfoEntry"/> from the outfit group.
        /// </summary>
        Creature2OutfitInfoEntry GetOutfitInfoEntry();
    }
}
