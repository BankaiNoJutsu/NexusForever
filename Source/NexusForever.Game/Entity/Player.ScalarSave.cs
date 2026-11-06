using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Entity;

public partial class Player
{
    private const PlayerSaveMask ScalarSaveMasks = PlayerSaveMask.Path | PlayerSaveMask.InputKeySet
        | PlayerSaveMask.Level | PlayerSaveMask.Innate | PlayerSaveMask.Sex | PlayerSaveMask.Race | PlayerSaveMask.Options;

    /// <summary>
    /// Acknowledge staged scalar Character columns only after the flushed row matches the staged
    /// values and the transaction commits. Failed or rolled-back saves keep the dirty bits for retry;
    /// later changes to a staged value are left dirty because the live value no longer matches.
    /// </summary>
    private void TrackScalarSave(CharacterContext context, CharacterModel staged, PlayerSaveMask stagedMask)
    {
        stagedMask &= ScalarSaveMasks;
        if (stagedMask == PlayerSaveMask.None)
            return;

        bool flushed = false;
        bool MatchesSql() => !context.HasSavepointRollback && context.Character.AsNoTracking().Any(row =>
            row.Id == staged.Id
            && ((stagedMask & PlayerSaveMask.Path) == 0 || (row.ActivePath == staged.ActivePath && row.PathActivatedTimestamp == staged.PathActivatedTimestamp))
            && ((stagedMask & PlayerSaveMask.InputKeySet) == 0 || row.InputKeySet == staged.InputKeySet)
            && ((stagedMask & PlayerSaveMask.Level) == 0 || row.Level == staged.Level)
            && ((stagedMask & PlayerSaveMask.Innate) == 0 || row.InnateIndex == staged.InnateIndex)
            && ((stagedMask & PlayerSaveMask.Sex) == 0 || row.Sex == staged.Sex)
            && ((stagedMask & PlayerSaveMask.Race) == 0 || row.Race == staged.Race)
            && ((stagedMask & PlayerSaveMask.Options) == 0 || (row.CastingOptions == staged.CastingOptions
                && row.SharedChallengeEnabled == staged.SharedChallengeEnabled
                && row.DisableOtherPlayersCombatLogs == staged.DisableOtherPlayersCombatLogs
                && row.CombatLogDisableFlags == staged.CombatLogDisableFlags)));

        EventHandler<SavedChangesEventArgs> onFlushed = (_, _) => flushed = MatchesSql();
        context.SavedChanges += onFlushed;
        context.OnSaveValidation(phase =>
        {
            if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit && flushed)
                flushed = MatchesSql();
        });
        context.OnSaveSettled(committed =>
        {
            context.SavedChanges -= onFlushed;
            if (!committed || !flushed)
                return;

            PlayerSaveMask acknowledge = PlayerSaveMask.None;
            if ((stagedMask & PlayerSaveMask.Path) != 0 && (byte)Path == staged.ActivePath && PathActivatedTime == staged.PathActivatedTimestamp) acknowledge |= PlayerSaveMask.Path;
            if ((stagedMask & PlayerSaveMask.InputKeySet) != 0 && (sbyte)InputKeySet == staged.InputKeySet) acknowledge |= PlayerSaveMask.InputKeySet;
            if ((stagedMask & PlayerSaveMask.Level) != 0 && Level == staged.Level) acknowledge |= PlayerSaveMask.Level;
            if ((stagedMask & PlayerSaveMask.Innate) != 0 && InnateIndex == staged.InnateIndex) acknowledge |= PlayerSaveMask.Innate;
            if ((stagedMask & PlayerSaveMask.Sex) != 0 && (byte)Sex == staged.Sex) acknowledge |= PlayerSaveMask.Sex;
            if ((stagedMask & PlayerSaveMask.Race) != 0 && (byte)Race == staged.Race) acknowledge |= PlayerSaveMask.Race;
            if ((stagedMask & PlayerSaveMask.Options) != 0 && (byte)CastingOptions == staged.CastingOptions
                && SharedChallengeEnabled == staged.SharedChallengeEnabled
                && DisableOtherPlayersCombatLogs == staged.DisableOtherPlayersCombatLogs
                && (ushort)CombatLogDisableFlags == staged.CombatLogDisableFlags)
                acknowledge |= PlayerSaveMask.Options;

            if (acknowledge != PlayerSaveMask.None)
                ClearPlayerSave(acknowledge);
        });
    }
}
