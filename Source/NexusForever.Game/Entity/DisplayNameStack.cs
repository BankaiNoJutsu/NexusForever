using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Entity;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// One <c>ChangeDisplayName</c> spell effect applied to a unit.
    /// </summary>
    public readonly record struct DisplayNameState(uint EffectId, uint Spell4Id, uint CastingId, string Name, bool Temporary);

    /// <summary>
    /// The display names spell effects have given a unit, in the order they were applied. The most recent one is shown; removing it reveals the previous one, and removing the last restores the
    /// unit's own name.
    /// </summary>
    public sealed class DisplayNameStack
    {
        private readonly List<DisplayNameState> states = [];

        /// <summary>
        /// The name currently shown, or null when no spell effect has renamed the unit.
        /// </summary>
        public string Current => states.Count == 0 ? null : states[^1].Name;

        /// <summary>
        /// Names that must be undone when their spell ends or is removed; permanent names (rows without a duration) are never listed.
        /// </summary>
        public IReadOnlyList<DisplayNameState> TemporaryStates => states.Where(s => s.Temporary).ToArray();

        public void Add(uint effectId, uint spell4Id, uint castingId, string name, bool temporary)
        {
            states.RemoveAll(s => s.EffectId == effectId);
            states.Add(new DisplayNameState(effectId, spell4Id, castingId, name, temporary));
        }

        public bool Remove(uint effectId)
            => states.RemoveAll(s => s.EffectId == effectId) != 0;
    }

    /// <summary>
    /// Builds the client message that makes a unit show a spell-given name, or its own name again.
    /// </summary>
    /// <remarks>
    /// A name is sent as the wide-string name change the client already applies to scanner units. A restored creature is sent as <c>ServerUnitNameAndTitleChange</c> with its Creature2 id so the client
    /// looks the name up itself; a restored player is sent the character name. Which packet retail used for this effect is not witnessed.
    /// </remarks>
    public static class DisplayNameSync
    {
        public static IWritable Build(IUnitEntity unit, string current)
        {
            if (current != null)
            {
                return new ServerUnitNameChange
                {
                    UnitId = unit.Guid,
                    Name   = current
                };
            }

            if (unit is IPlayer player)
            {
                return new ServerUnitNameChange
                {
                    UnitId = unit.Guid,
                    Name   = player.Name ?? string.Empty
                };
            }

            return new ServerUnitNameAndTitleChange
            {
                UnitId      = unit.Guid,
                TitleId     = 0u,
                Creature2Id = unit.CreatureId
            };
        }
    }
}
