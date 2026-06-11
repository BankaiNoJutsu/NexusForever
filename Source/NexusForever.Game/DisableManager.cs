using System.Collections.Immutable;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Static;

namespace NexusForever.Game
{
    public sealed class DisableManager : IDisableManager
    {
        private static ulong Hash(DisableType type, uint objectId)
        {
            // hash where type takes up the top 32 bits and objectId takes up the lower 32 bits
            return ((ulong)type << 32) | objectId;
        }

        private readonly IDatabaseManager databaseManager;

        private ImmutableDictionary<ulong, Disable> disables = ImmutableDictionary<ulong, Disable>.Empty;

        public DisableManager(
            IDatabaseManager databaseManager = null)
        {
            this.databaseManager = databaseManager;
        }

        public void Initialise()
        {
            if (databaseManager == null)
                throw new InvalidOperationException("DisableManager requires an IDatabaseManager.");

            var builder = ImmutableDictionary.CreateBuilder<ulong, Disable>();
            foreach (DisableModel model in databaseManager.GetDatabase<WorldDatabase>().GetDisables())
            {
                DisableType type = (DisableType)model.Type;
                builder.Add(Hash(type, model.ObjectId), new Disable(type, model.ObjectId, model.Note));
            }

            disables = builder.ToImmutable();
        }

        /// <summary>
        /// Returns if <see cref="DisableType"/> and objectId are disabled.
        /// </summary>
        public bool IsDisabled(DisableType type, uint objectId)
        {
            ulong hash = Hash(type, objectId);
            return disables.ContainsKey(hash);
        }

        /// <summary>
        /// Return the disable reason for <see cref="DisableType"/> and objectId.
        /// </summary>
        public string GetDisableNote(DisableType type, uint objectId)
        {
            ulong hash = Hash(type, objectId);
            return disables.TryGetValue(hash, out Disable disable) ? disable.Note : null;
        }
    }
}
