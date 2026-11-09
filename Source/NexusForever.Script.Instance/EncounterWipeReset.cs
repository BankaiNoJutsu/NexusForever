using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Script.Instance
{
    /// <summary>
    /// Wipe reset facility for encounter owners (Phase A item "spawn, door and platform restore on a wipe").
    /// A boss script registers what its fight changed (adds it spawned, invulnerable states it applied, a door it opened,
    /// a platform it moved); when the boss leaves combat while still alive, <see cref="Run"/> undoes all of it so the next
    /// pull starts from the opening state. A boss that dies is not reset, so doors it opened stay open.
    /// The helper holds no timing or condition and knows nothing about specific doors: the registering script supplies
    /// the restore action. It is not retail behaviour evidence, only the plumbing a script needs.
    /// </summary>
    public static class EncounterWipeReset
    {
        private sealed class State
        {
            public readonly List<IGridEntity> Adds = [];
            public readonly Dictionary<string, Action> Restores = [];
        }

        private static readonly ConditionalWeakTable<IUnitEntity, State> states = new();

        /// <summary>
        /// Remember an entity the encounter spawned; it is removed from the map on a wipe.
        /// </summary>
        public static void TrackAdd(IUnitEntity owner, IGridEntity add)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(add);

            State state = states.GetOrCreateValue(owner);
            if (!state.Adds.Contains(add))
                state.Adds.Add(add);
        }

        /// <summary>
        /// Register an action that restores something the fight changed (close a door, return a platform).
        /// Registering the same key again replaces the earlier action, so a repeated open does not queue two closes.
        /// </summary>
        public static void RegisterRestore(IUnitEntity owner, string key, Action restore)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNull(restore);

            states.GetOrCreateValue(owner).Restores[key] = restore;
        }

        /// <summary>
        /// Undo the encounter state of an owner that has just left combat. Does nothing for a dead owner (a kill is not a wipe)
        /// or one that registered nothing. Releases every invulnerable state held through <see cref="EncounterInvulnerability"/>,
        /// removes tracked adds and runs the registered restores, then forgets all of them. A failing restore does not stop the others.
        /// Returns the number of things undone.
        /// </summary>
        public static int Run(IUnitEntity owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            if (!owner.IsAlive)
                return 0;

            int undone = EncounterInvulnerability.ReleaseAll(owner);

            if (!states.TryGetValue(owner, out State state))
                return undone;

            IGridEntity[] adds = [.. state.Adds];
            Action[] restores = [.. state.Restores.Values];
            state.Adds.Clear();
            state.Restores.Clear();

            foreach (IGridEntity add in adds)
            {
                add.RemoveFromMap();
                undone++;
            }

            foreach (Action restore in restores)
            {
                try
                {
                    restore();
                }
                catch (Exception)
                {
                    // One broken restore must not leave the rest of the encounter half reset.
                }

                undone++;
            }

            return undone;
        }
    }
}
