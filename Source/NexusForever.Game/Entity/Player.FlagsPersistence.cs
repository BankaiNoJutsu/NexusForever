using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Entity;

public partial class Player
{
    private long characterFlagsVersion;

    private void StageCharacterFlagsSave(CharacterContext context, EntityEntry<CharacterModel> entity)
    {
        uint value; long version; ulong character = CharacterId;
        if (!Monitor.TryEnter(mapAdmission.SyncRoot)) return;
        try { value = (uint)flags; version = characterFlagsVersion; }
        finally { Monitor.Exit(mapAdmission.SyncRoot); }
        entity.Entity.Flags = value;
        var property = entity.Property(row => row.Flags);
        property.IsModified = true;
        bool writing = false, flushed = false;
        bool SameWrite() => entity.Entity.Id == character && property.CurrentValue == value
            && entity.State is not (EntityState.Deleted or EntityState.Detached);
        bool MatchesSql() => !context.HasSavepointRollback && context.Character.AsNoTracking()
            .Any(row => row.Id == character && row.Flags == value);
        context.OnSaveValidation(phase =>
        {
            if (phase == CharacterSaveValidationPhase.BeforeFlush) writing = SameWrite() && property.IsModified;
            if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit && flushed) flushed = MatchesSql();
            if (!SameWrite()) flushed = false;
        });
        EventHandler<SavedChangesEventArgs> onFlushed = (_, _) =>
        { if (writing) flushed = SameWrite() && entity.State == EntityState.Unchanged && MatchesSql(); };
        context.SavedChanges += onFlushed;
        context.OnSaveSettled(committed =>
        {
            context.SavedChanges -= onFlushed;
            if (!committed || !flushed || !Monitor.TryEnter(mapAdmission.SyncRoot)) return;
            try
            {
                if (CharacterId == character && characterFlagsVersion == version && (uint)flags == value)
                    ClearPlayerSave(PlayerSaveMask.Flags);
            }
            finally { Monitor.Exit(mapAdmission.SyncRoot); }
        });
    }
}
