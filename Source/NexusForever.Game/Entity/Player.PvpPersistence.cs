using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Entity;

public partial class Player
{
    private long pvpCooldownVersion;

    private void SetPvPCooldownExpiry(DateTime? expiry)
    {
        lock (mapAdmission.SyncRoot)
        {
            pvpFlagDisableUntilUtc = expiry;
            pvpCooldownVersion++;
            MarkPlayerSave(PlayerSaveMask.PvP);
        }
    }

    private void StagePvPCooldownSave(CharacterContext context, EntityEntry<CharacterModel> entity)
    {
        DateTime? expiry;
        long version;
        ulong character = CharacterId;
        // Share location's dirty-mask gate, without waiting on map callbacks
        // from a Character transaction. The next save can stage a skipped expiry.
        if (!Monitor.TryEnter(mapAdmission.SyncRoot)) return;
        try { expiry = pvpFlagDisableUntilUtc; version = pvpCooldownVersion; }
        finally { Monitor.Exit(mapAdmission.SyncRoot); }
        entity.Entity.PvpFlagDisableUntilUtc = expiry;
        var property = entity.Property(row => row.PvpFlagDisableUntilUtc);
        property.IsModified = true;
        bool writing = false, flushed = false;
        bool SameWrite() => entity.Entity.Id == character && property.CurrentValue == expiry
            && entity.State is not (EntityState.Deleted or EntityState.Detached);
        context.OnSaveValidation(phase =>
        {
            if (phase == CharacterSaveValidationPhase.BeforeFlush)
                writing = SameWrite() && property.IsModified;
            if (!SameWrite()) flushed = false;
        });
        EventHandler<SavedChangesEventArgs> onFlushed = (_, _) =>
        {
            // A successful unrelated flush must not acknowledge a suppressed
            // cooldown write. Do not impose extra DateTime precision on providers.
            if (writing) flushed = SameWrite() && entity.State == EntityState.Unchanged;
        };
        context.SavedChanges += onFlushed;
        context.OnSaveSettled(committed =>
        {
            context.SavedChanges -= onFlushed;
            if (!committed || !flushed || !Monitor.TryEnter(mapAdmission.SyncRoot)) return;
            try
            {
                if (CharacterId == character && pvpCooldownVersion == version && pvpFlagDisableUntilUtc == expiry)
                    ClearPlayerSave(PlayerSaveMask.PvP);
            }
            finally { Monitor.Exit(mapAdmission.SyncRoot); }
        });
    }
}
