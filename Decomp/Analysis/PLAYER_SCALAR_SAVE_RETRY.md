# Player scalar Character save retry (2026-11-06)

`Player.SaveCharacter` used to clear the Path, InputKeySet, Level, Innate, Sex, Race and
Options dirty bits while staging the row, before SQL flushed or committed. A rolled-back,
rejected, savepoint-reverted or lost-flush save therefore dropped those changes permanently.

`TrackScalarSave` (Player.ScalarSave.cs) now mirrors the Location/PvP acknowledgement: the bits
clear only after the flushed row matches the staged values (re-read before explicit commit, no
savepoint rollback) and the transaction commits. A bit whose live value changed after staging
stays dirty. Owner-managed columns (experience path/xp owners, Character keybinding manager)
keep their previous immediate clear because those owners settle them.

Evidence tier: Source/SQLite only. `ScalarCharacterFields*` 6 cases (5 failed before the fix:
rollback, dispose, reject, unflushed, savepoint) pass; 225 related Transport/PvP/Player
persistence cases pass. Not covered: Level/Path/Sex/Race columns individually (shared code
path, Innate/Options/SharedChallenge exercised), MySQL, socket, native, retail.
