using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using Microsoft.EntityFrameworkCore;
using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    public class PetFlair : IPetFlair
    {
        public ulong Owner { get; }
        public PetFlairEntry Entry { get; }

        private bool isDirty;
        private readonly object saveGate = new();
        private string database;

        /// <summary>
        /// Create a new <see cref="IPetFlair"/> from existing <see cref="CharacterPetFlairModel"/> database model.
        /// </summary>
        public PetFlair(CharacterPetFlairModel model, PetFlairEntry entry)
        {
            Owner = model.Id;
            Entry = entry;
        }

        /// <summary>
        /// Create a new <see cref="IPetFlair"/> from supplied <see cref="PetFlairEntry"/>.
        /// </summary>
        public PetFlair(ulong owner, PetFlairEntry entry)
        {
            Entry   = entry;
            Owner   = owner;

            isDirty = true;
        }

        public void Save(CharacterContext context)
        {
            lock (saveGate)
            {
                if (!isDirty) return;
                try
                {
                    database ??= CharacterDatabaseIdentity.Get(context);
                    CharacterDatabaseIdentity.Require(context, database);
                    uint id = Entry.Id;
                    if (!context.CharacterPetFlair.Local.Any(row => row.Id == Owner && row.PetFlairId == id)
                        && !context.CharacterPetFlair.AsNoTracking().Any(row => row.Id == Owner && row.PetFlairId == id))
                        context.Add(new CharacterPetFlairModel { Id = Owner, PetFlairId = id });
                    // An append-only row is acknowledged only after its commit,
                    // including observation of an earlier uncertain insertion.
                    context.OnSaveCompleted(committed =>
                    {
                        if (!committed) return;
                        bool stored = context.CharacterPetFlair.AsNoTracking().Any(row => row.Id == Owner && row.PetFlairId == id);
                        lock (saveGate) if (stored && Entry.Id == id) isDirty = false;
                    });
                }
                catch (Exception error) { context.RejectSave(error); throw; }
            }
        }
    }
}
