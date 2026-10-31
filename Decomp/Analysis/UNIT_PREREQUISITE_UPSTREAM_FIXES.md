# Unit and target prerequisite reconciliation (#568 / #554)

Status: implementation and focused regression verification; final combined gate
and live NPC spell validation are recorded separately in the assessment report.

## Evidence

Build-16042 WildStar64.exe fingerprint
`sha256:231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`:

- `PrerequisiteManager_EvaluateTypeSlot` (`1404a2100`) receives an evaluation
  context whose first entity is the subject and second entity is the relationship
  target. Type 1 reads the subject's level; type 28 reads its combat state;
  type 250 dispatches the subject to the base-faction handler. Type 73 obtains the
  subject's requested vital through `140466520`.
- Nested prerequisite type 11 invokes `1404a1ca0` with the same context pointer.
  It does not reconstruct an empty context or discard item/relationship data.
- Subject-only state/effect/movement cases pass the first entity, while explicit
  relationship checks such as distance (95), target spell checks (50), and
  positional checks retain access to both entities. This supports separate
  subject/target APIs rather than reversing every existing target-aware check.
- Frozen upstream PR #568 supplies the unit and spell gate integration intent.
  #554's broad handler rewrite is not imported: the fork already has richer
  handlers and verified comparison identifiers. Existing proxy calculations,
  item context, grant policy, and unmodified handler APIs remain intact.

Cached exact-address fragments were read before a focused recovered-project
inspection. The inspection applied durable labels and refreshed all five helper
bodies plus the live dispatcher. Four new mapped labels are retained in
`function_labels.csv`: `Prerequisite_CheckVitalPercent` (`1404a4e30`),
`Prerequisite_CheckShieldCapacityPercent` (`1404a4cf0`),
`Unit_GetCurrentVitalForPrerequisite` (`140466520`), and
`Unit_GetVitalCapacityForPrerequisite` (`1404667c0`). The existing float comparison
label (`1404a2010`) is preserved. These helpers underpin the separately implemented
type 71/215 percentage checks; no native source is copied into runtime code.

## Compatible implementation

`IPrerequisiteManager.MeetsUnit` and `IPrerequisiteCheck.MeetsUnit` add explicit
unit-subject evaluation. Every old `Meets(IPlayer, ...)` signature remains valid,
without introducing ambiguous null overloads. Default interface adapters keep custom
player-only implementations source compatible. Unsupported non-player checks fail
closed, rather than treating an unrelated player target as the subject.

The manager retains AND/OR evaluation, resolves existing keyed handlers, and
preserves the complete parameters object. Nested checks now forward that same
object through both old and new APIs, retaining target, item, taxi, and account-item
contexts. Supported unit scalar, health, faction, vital, tracked-spell, movement,
phase, creature-state, effect, and positional/distance checks reuse existing
calculation logic. Old player/target proxy behavior remains available through the
old entry points. The new entry point reads the explicit subject for subject-only
checks and the separate target only for relationship checks.

Spell caster and explicit-target cast gates now evaluate their respective units;
no explicit target uses the caster as the existing upstream self context. Missing
explicit targets fail a configured target gate. Runners override only caster
gates. Apply checks retain the fork's difficulty-only fast path and evaluate both
caster and effect target for other unit rows. Persistence retains per-effect
termination, lifetime cleanup, cooldown removal, and suppression of terminated
effects, with caster and target gates evaluated against their own units.

No SpellInfo property renames, general proc reconstruction, comparison enum
rewrites, entitlement changes, or broad replacement of player-only handlers are
included.

## Verification and limits

`UnitPrerequisiteContextTests` covers NPC subjects distinct from player targets,
old target API behavior, existing custom implementation compatibility, unsupported
player-only NPC checks, AND/OR semantics, full nested context, relationship checks,
missing inputs, cast/runner/target gates, apply and persistence subjects, and
existing unit-state proxy calculations. The assessment's
`remaining-goal/UNIT_PREREQUISITES.md` and `unit-prerequisite-tests.log` record the
focused commands, alongside native export summary and label hashes. The parent
integrated checkpoint passed the full solution build with zero warnings/errors
and all 6,450 tests, including both final nested-item cases; evidence is
`integrated-build.log`, `integrated-tests.log`, and
`integrated-tests/integrated-suite.trx` in the assessment report. The native client
reached world 51 after correcting the isolated complete-asset path, and the user
confirmed that the world loaded and responded normally. That native session ran
the earlier copied checkpoint; the separate later 6511 World synthetic gate
passed 28/28 mixed Eastern/NPE sessions. These entry/responsiveness observations
do not establish the specific NPC prerequisite gameplay checks below.

Several existing checks remain explicit server proxies (health-scaled fallback,
entity type, movement state, phase visibility, active-effect groups, and creature
difficulty). This change preserves those calculations; explicit-subject support
does not promote them to newly verified retail parity. Unsupported NPC handlers
fail closed. Live NPC cast/apply/persistence traffic remains an additional runtime
gate; the earlier login/world smoke checkpoint is not evidence for these new
gameplay paths.
