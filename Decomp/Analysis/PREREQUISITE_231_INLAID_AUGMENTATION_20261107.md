# Prerequisite 231 is "inlaid augmentation not yet unlocked"; its list, producer and the missing persistence (2026-11-07)

Evidence tier: **native writer and reader read** (`FUN_1403b5f80`, `FUN_14049d7b0`) plus a **data correlation** (235 of 240 ids). The effect-to-list link is Correlated, not Verified. No code change: the producer needs persistence that does not exist. Source/SQLite only. No retail parity claim.

## What the list is
[PREREQUISITE_TYPE_COVERAGE_20261107.md](PREREQUISITE_TYPE_COVERAGE_20261107.md) recorded that type 231 scans a player-held array of 16-bit ids at `player + 0x6df4` (count at `+0x6df0`) and that the writer was unidentified. The writer is the **player-create handler**
`FUN_1403b5f80` (`ServerPlayerCreate`): it stores the count byte (`param_2[0x39]`) at `+0x6df0` and `memcpy`s `count * 2` bytes from the packet into `+0x6df4`. That is the packet's `UnlockedInlaidAugmentationIds` field (a 6-bit count and 16-bit ids; the older model name for it was
`KnownDyes`). The server fills this list nowhere (`ServerPlayerCreate.UnlockedInlaidAugmentationIds` is never populated), so every client believes the character has unlocked none.

## Type 231
All 241 retail slots are `NotEqual` with `Value` 0 and an `ObjectId` between 13 and 936 (240 distinct). The body returns false unless the unit is the local player, so for NotEqual-only rows the content is available exactly when the id is **not** in the list.
Because the server never reports a populated list, a correct check would be true for every row today, while the unhandled type evaluates false and keeps the gated content unavailable.

## Producer
The `UnlockInlaidAugment` spell effect (type 127, 281 retail rows, currently unhandled): `DataBits01..09` are always 0 and `DataBits00` has 244 distinct values, **235 of which are exactly the type 231 `ObjectId`s** (235 of the 240 prerequisite ids appear as an effect value).
So the effect adds `DataBits00` to the character's unlocked list and prerequisite 231 stops the same grant from being offered twice. `ServerLasAuxPackets` already carries a message with the same `UnlockedInlaidAugmentationIds` field (the incremental update).

## Why nothing was implemented
A faithful implementation needs (1) a per-character persisted set of unlocked ids (a new Character table plus MySQL and SQLite migrations), (2) the `UnlockInlaidAugment` effect handler, (3) the list in `ServerPlayerCreate` and the aux update packet, and (4) the type 231 check (also pinned by
`AmpLifecycleTests.RegisteredPrerequisiteDispatchKeepsEnabledAndGlobalAmpListsSeparate`, which requires it to be a list separate from the enabled-AMP list used by 227). Step 1 is a schema/DDL decision and needs the migration projects to be built, which this lane does not do; an in-memory-only version
would silently forget unlocks at relog and was not added. This also moves the 281 `UnlockInlaidAugment` rows from "no witness" to "data-correlated producer, needs persistence".

## Remaining gate
A persistence design (table and migrations) approved for the Character database, and confirmation of the effect's observable result from a capture (that the aux update packet is sent when the effect fires).

## Sizing of the persistence step (same day)
Tooling is not the obstacle: `dotnet-ef` 10.0.8 is installed and both providers have design-time contexts (`SqliteContextFactories`, `NexusForever.WorldServer/Design/CharacterContextFactory`), so the two migrations (and their snapshots) could be generated.
The cost is the integration. A persisted Character entity here must follow the repo's atomic save idiom: `CharacterSaveGraph.Seal` runs `ValidateCharacterOwner` on every tracked row, so a new row type needs an ownership rule; titles show the full pattern
(`TitleManager` + `TitlePersistence` snapshots, `Player.NormalRewardSave` capture, `Player.TitlePublication`, load through `CharacterDatabase` includes, plus the durability tests). Keybindings are the simplest comparable. The set also has a client cap of 54 ids
(`ServerActionSetUInt16List.MaxUnlockedInlaidAugmentations`; packet `0x019C` replaces the whole list and raises `CharacterUnlockedInlaidEldanAugmentation`). This is an estimated multi-file feature (model, mapping, 2 migrations, manager, save/load/normal-save integration, packet list in `ServerPlayerCreate`
and the `0x019C` update, the effect handler, the 231 check, tests), judged larger than a safe single slice without a way to run the Character persistence qualification here; it is declined as an effort and risk gate, not a missing witness. The effect's observable result (the `0x019C` update and the event) is already
named by the packet comment, so the only external input still wanted is confirmation from a capture.

## Implemented (same day, after the sizing above proved too pessimistic)
The ownership validation in `CharacterSaveGraph.ValidateCharacterOwner` is generic: it follows mapped foreign keys to the character id, so a new row type with a foreign key to `character` needs no special rule. That removed the main obstacle and the
feature was built:
- `CharacterInlaidAugmentationModel` (`character_inlaid_augmentation`: `id` bigint unsigned, `augmentationId` smallint unsigned, primary key both, foreign key to `character` with cascade), the context mapping, `CharacterModel.InlaidAugmentation` and the load include in `CharacterDatabase`.
- Migrations generated with `dotnet ef` for both providers: `20261107211527_CharacterInlaidAugmentation` (MySQL, in the Character project) and `20261107211556_CharacterInlaidAugmentation` (SQLite), each a single `CreateTable`, with snapshot additions only.
- `IInlaidAugmentationManager` / `InlaidAugmentationManager` (ascending ids, zero and duplicates rejected, 54-id client cap, `Save` stages only unpersisted ids and marks them persisted after a committed save via `OnSaveSettled`), exposed as `IPlayer.InlaidAugmentationManager`, loaded in the player constructor, saved with the character, and written into `ServerPlayerCreate.UnlockedInlaidAugmentationIds`.
- The `UnlockInlaidAugment` effect handler (`DataBits00` = id, then packet `0x019C` with the full list) and `PrerequisiteCheckEldanAugmentation231` (Equal present, NotEqual absent, other comparisons false).

Tests: `InlaidAugmentationTests` (20 cases: load order, cap and duplicates, persistence including a save that never commits, the prerequisite and the effect) passed after fixing my test's spell proxy; the code was written before the tests, so no red run was observed. Two existing tests encoded assumptions this broke and were updated, not weakened:
`CraftingAccountMigrationTests.BeforeLast` asserted that `CraftingAccountAwards` is the *last* migration (it now finds that migration by name and migrates to its predecessor), and `Player` null-tolerance for the new manager was needed because several tests build a partial `Player`. The 3,882-case
Persistence/Database/Prerequisite/Player/Title/Amp/Migration/Spell Source/SQLite filter passes (3,880 passed, 2 skipped as before; separate scope). The unhandled spell-effect rows drop from 2,478 to 2,197 (the 281 `UnlockInlaidAugment` rows are handled).

Not verified: the MySQL migration was generated but not applied to a fresh MySQL schema in this pass; the effect-to-list link remains data-correlated (235 of 240 ids), and the client's reaction to packet `0x019C` needs a capture.

## MySQL migration applied (same day, later)
`dotnet ef database update` (project `NexusForever.Database.Character`, startup `NexusForever.WorldServer`, a temporary design-time config) was run against a fresh disposable schema on local MySQL 8.0.46: all **50** Character migrations applied, the last two being
`20261105080127_CraftingAccountAwards` and `20261107211527_CharacterInlaidAugmentation`, and `SHOW CREATE TABLE character_inlaid_augmentation` shows `id bigint unsigned`, `augmentationId smallint unsigned`, `PRIMARY KEY (id, augmentationId)` and the foreign key
`FK__character_inlaid_augmentation_id__character_id` to `character(id)` with `ON DELETE CASCADE`. The schema was dropped afterwards. This closes the "generated but not applied" caveat for the MySQL provider (forward migration only: no insert, cascade or load was exercised on MySQL; the SQLite
tests cover persistence). The effect-to-list link (data correlation) and the client's reaction to packet `0x019C` remain unverified.
