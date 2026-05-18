# Wiki Archive Audit

`audit_wildstar_wiki.py` is a reusable offline audit harness for comparing
durable facts from the local WildStar Fandom archive against extracted client
SQL and NexusForever implementation surfaces.

It intentionally works offline and uses only the Python standard library. The
default inputs are:

- wiki archive: `artifacts\wildstar-fandom-wiki-2026-05-18`
- client SQL dump directory: `wildstar_client_mysql`

## Implemented Domains

### `character-creation`

This domain covers:

- wiki class infobox race availability
- wiki class roles
- wiki-derived class item proficiencies compared to client `Class.tbl.sql`
- enabled `CharacterCreation` client rows
- supported server start-location coverage
- player path names exposed by the wiki and the server enum
- player path level curves and level reward coverage
- server path reward grant handlers for client reward kinds
- wiki path reward table completeness, with known wiki/client differences
- path ability unlock levels compared to client spell reward levels
- direct path ability spell effects compared to server spell-effect handlers
- one-hop proxied path ability spell effects compared to server spell-effect
  handlers
- rested-XP spell effects, separating direct rest-pool modifiers from blocked
  decor/aura bonus modifiers

Run from the repository root:

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain character-creation `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output artifacts\wiki-audit\character-creation-audit.md
```

If `--domain` is omitted, the script runs all implemented domains. If
`--output` is omitted, the report is printed to stdout.

List implemented and planned domains:

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py --list-domains
```

Use a non-default client SQL dump:

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --client-sql-dir D:\WildStar\client-sql `
  --output artifacts\wiki-audit\character-creation-audit.md
```

Exit code `0` means the report has no hard failures. Exit code `1` means the
audit completed and found one or more hard failures. Exit code `2` means the
tool could not run because an input path or domain was invalid.

Current path ability notes:

- The audit treats wiki/client unlock-level differences as `INFO` unless the
  client spell reward rows cannot be matched at all.
- `SettlerCampfire` has a narrow runtime handler for the client-table-backed
  Back in Action buff cast. Campfire world-object behavior and proxied
  `RestedXpDecorBonus` semantics remain blocked until the rest-XP accrual,
  timing, and stacking rules are mapped independently.
- `ModifyRestedXP` is handled as a direct, cap-clamped rested-XP pool modifier
  from the client spell table rows. It is intentionally not used as evidence
  for `RestedXpDecorBonus`.

### `abilities`

This domain covers:

- wiki class ability infobox pages by class and combat type
- client `SpellLevel.tbl.sql` class unlock rows
- wiki combat ability unlock levels compared to matched client `SpellLevel`
  and `Spell4` rows
- current server ability-grant surfaces that read client `SpellLevel`, check
  prerequisites, grant base spell ids, and grant class innate/primary attacks

Run from the repository root:

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain abilities `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output artifacts\wiki-audit\abilities-audit.md
```

Current ability notes:

- Wiki/client unlock-level differences are `INFO`. `SpellLevel.tbl.sql` is the
  implementation authority, and the server grant path is expected to follow it.
- Wiki combat ability pages without a current client `SpellLevel` match are
  `INFO`, because the wiki archive can retain stale or deprecated ability pages.

### `amps`

This domain covers:

- wiki AMP infobox pages by branch and class category
- client `EldanAugmentation.tbl.sql` and `EldanAugmentationCategory.tbl.sql`
  coverage
- client AMP id ranges compared to network and database storage width
- server AMP id persistence through `ushort` request/list/model surfaces
- server AMP validation for ids, power cost, player class, and required AMP
  series dependencies

Run from the repository root:

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain amps `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output artifacts\wiki-audit\amps-audit.md
```

Current AMP notes:

- Client AMP ids exceed byte range, so persistence must remain `ushort` end to
  end.
- Inlaid AMP item unlocks and category-tier locks are recorded as `INFO`
  blockers until learned inlaid state and category-tier spending rules are
  independently mapped.

### `housing`

This domain covers:

- wiki housing page facts for unlock level, rested XP, decor bonus categories,
  sockets/plugs, visitor rules, expeditions, and communities
- client housing table presence for residences, properties, maps, plots, plugs,
  decor, decor limits, wallpaper, and neighborhoods
- client decor/rested-XP rows, including `RestedXpDecorBonus` spell effects
- server residence, plot, plug, decor, wallpaper, and rested-XP implementation
  surfaces

Run from the repository root:

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain housing `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output artifacts\wiki-audit\housing-audit.md
```

Current housing notes:

- Residence, plot, plug, decor, and direct rested-XP packet/persistence surfaces
  are table-backed and checked by the audit.
- Plug contribution/prerequisite/runtime effects and interior wallpaper updates
  remain explicitly bounded by current server handlers.
- `RestedXpDecorBonus` is deliberately not implemented as a mutating spell
  effect. The decompile pass maps rested-XP state copies, UI reads, Lua reads,
  and experience consumption, but not decor accrual timing, stacking, or
  persistence.

### `quests`

This domain covers:

- wiki quest infobox pages, objective sections, rewards, episode links, and
  previous/next chain fields
- client `Quest2`, `QuestObjective`, `Quest2Reward`, episode, category, hub,
  and periodic quest table coverage
- client objective/reward type ids compared to server enums
- server quest lifecycle surfaces for accept, abandon, retry, track, share,
  objective updates, completion, repeat resets, and rewards

Run from the repository root:

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain quests `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output artifacts\wiki-audit\quests-audit.md
```

Current quest notes:

- Item, money/currency, reputation, XP, cash, tradeskill XP, tradeskill unlock,
  and account currency rewards are covered by runtime grant paths.
- The current client `Quest2Reward` dump has no account-item or generic-unlock
  quest reward rows. `RotationEssence` rows remain an `INFO` blocker because
  the client expands them through a server-provided active reward-rotation
  schedule keyed by `WorldZone.RewardRotationContentId` and `Quest2.Id`, plus an
  active multiplier.
- Item prerequisites and quest exclusion prerequisites are covered by quest
  accept validation.
- Faction-level gates are covered by quest accept validation. Alternate
  receiver fields are mapped as client-side dialog/map guidance: the completion
  packet does not carry a receiver/location selector, and the client data rows
  with alternate receiver prerequisites still have Creature2 receive entries.
- Virtual pushed items are mapped as a client-derived UI/inventory projection
  from quest state; the server-owned mutation remains the existing
  `VirtualCollect` objective/loot path.

### `achievements`

This domain covers:

- wiki achievement infobox pages, descriptions, criteria, title rewards, and
  series sections
- client achievement, checklist, category, group, subgroup, title, and challenge
  table coverage
- client achievement type ids compared to server enum/runtime trigger coverage
- server achievement persistence, packets, checklist/value completion,
  prerequisites, title grants, realm-first broadcasts, and mapped trigger paths

Run from the repository root:

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain achievements `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output artifacts\wiki-audit\achievements-audit.md
```

Current achievement notes:

- Persistence, initial/update packets, checklist and scalar completion, title
  grants, realm-firsts, quest completion, creature kills, crafted items,
  tradeskill tiers, reputation levels, map completion, character levels, titles
  earned, path levels, currency gains, item consume, duel participation/wins,
  guild/circle joins, group joins, friend additions, housing plug placement,
  housing decor purchases, account-currency grants from quest/loot paths, and
  `AchievementAdvance` spell grants are covered.
- Remaining client achievement type ids stay data-only until each trigger
  family is mapped to a runtime event.
- Wiki title matching is informational because the archive and client
  localisation differ heavily for PvP/ranked achievement naming.

### `tradeskills`

This domain covers:

- wiki tradeskill, crafting, profession, runecrafting, salvaging, and schematic
  list coverage
- client tradeskill, tier, talent, schematic, material, harvesting, catalyst,
  additive, achievement, material, and rune table coverage
- client tradeskill ids compared to server `TradeskillType` values
- client schematic output/material/discovery data compared to server profession
  and fixed-recipe crafting surfaces
- server profession persistence, learn/drop/talent/reset handlers, material
  debits, additive/catalyst item validation and consumption, supply-satchel
  updates, crafting result packets, give-schematic packets, and rune request
  handlers

Run from the repository root:

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain tradeskills `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output artifacts\wiki-audit\tradeskills-audit.md
```

Current tradeskill notes:

- Profession persistence, learned/discovered schematic persistence,
  learn/drop/talent/reset, active-profession validation for schematic crafting,
  direct item outputs, material debits, additive/catalyst item validation and
  consumption, satchel updates, craft XP, quest tradeskill XP, and
  crafting/rune packet surfaces are covered.
- Relearn cost/cooldown semantics, hot/cold discovery, fail/crit outputs,
  additive/catalyst output math, harvesting behavior, and durable rune state
  remain explicit `INFO` blockers.
- Client data and wiki text disagree on naming for tradeskill id 16:
  server code currently names it `Augmentor`, while the wiki-facing profession
  is Technologist.

### `lore`

This domain covers:

- wiki lore, Codex, Datacube, Galactic Archive, Zone Lore, datacube entry, and
  archive-topic coverage
- client datacube, datacube volume, Scientist datacube-discovery, Galactic
  Archive article/entry/unlock/link/category, and story panel table coverage
- server character datacube persistence, activation updates, initial/update
  packets, story panel display, Galactic Archive packet models, and archive
  article chat formatting

Run from the repository root:

```powershell
python Tools\WikiArchiveAudit\audit_wildstar_wiki.py `
  --domain lore `
  --archive-dir artifacts\wildstar-fandom-wiki-2026-05-18 `
  --output artifacts\wiki-audit\lore-audit.md
```

Current lore notes:

- Datacube/journal persistence, activation, progress packets, story-panel
  display, Galactic Archive unlock/view persistence, basic faction and
  quest/achievement archive rules, archive title rewards, and archive packet
  models are covered.
- ArchiveEntryUnlockRule type 1, Scientist datacube-discovery mission
  completion, datacube unlock-count and quest-direction semantics, and
  StoryPanel prerequisite enforcement remain explicit `INFO` blockers. The
  mapped client `0x00F9` path progress report carries the PathMission
  ObjectId/PowerMap id, not trusted mission-completion state.

## Next Audit Domains

Extend this tool one domain at a time, preferring checks that can be backed by
both wiki structure and client/game-table data:

All initially planned domains are implemented. Add new domains only when a new
wiki/client/runtime surface needs a focused audit.

When adding a domain, keep it additive:

1. Add parser helpers for the wiki pages and client SQL tables needed by the
   domain.
2. Add one `build_<domain>_report(...)` function that returns report lines,
   hard failures, and warnings.
3. Register the domain in `IMPLEMENTED_AUDIT_DOMAINS`.
4. Run the audit with `--domain <name>` and save the report under
   `artifacts\wiki-audit\`.
