# Healing personal-modifier evidence boundary

## Caster-scoped health healing — 6 November 2026

Implemented the supported effect151 kind12/mode1 caster–target boundary.
Three baseline producer/ownership controls failed: a foreign healer received
120 instead of100, a second caster replaced the first caster's registration,
and health-only personal state leaked into shield healing. All26 new cases and
214 selected related cases now pass. Fifteen recovered catalog rows pass the
same two-caster, global-property, shield and isolated-removal controls using
their actual rows. This is Source/actual-catalog qualification, not native or
retail completion. The overall restoration Goal remains active.

### Recovered primary inputs

The existing owning GameTable/TextTable readers loaded Spell4Effects, Spell4,
Spell4Base, SpellEffectType and en-US.bin from the recovered asset cache. Each
input was checked against its retained asset manifest; hashes and extents are
in [the portable record](coverage/healing-personal-scope-20261106.json).
The Spell4Effects hash matches the earlier retained AD49CB47... input.

All145 effect151 rows are present. Nine distinct kinds occur; all15 kind12
rows use mode1, float DataBits02, zero DataBits03..09 and zero effect-group list.
SpellEffectType151 declares float DataType02 (0), independently corroborating
the bit cast. The relevant joins are:

| Family | Effect IDs | Spell IDs | Multiplier | Lifetime |
| --- | --- | --- | --- | --- |
| Fusion Probe |231375,231412–231415|88057–88061|1.2|duration0, CancelOnly flag2|
| Sigil of Recovery |231262–231266|88130–88134|1.1|3000ms, flags0|
| Sigil of Recovery |231315,231317,231319,231321,231323|88350–88354|1.1|3000ms, flags0|

English text767922 directly references effect231262 and describes increased
healing by the owner onto allies affected by Sigil of Recovery. Text768078
(and duplicate769875) directly references231375 and the owner's Fusion Probe.
Target icon texts768160–768164 each bind their corresponding Fusion Probe
effect and explicitly identify healing received from that caster. These direct
row/text joins close the previously missing independent caster-scope input;
they are shipped client text, not merely upstream's interpretation.
They do not establish server stacking arbitration or native execution.

Fresh target-free REA PE inspection binds
D:/WildStar/Client64/WildStar64.exe (13,927,576bytes) to SHA256
231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5,
Evidence ev_3a002aa3d25d9d846a80c84d9d2d23eb73db439235129acc4a5501029628a320.
PE inspection establishes identity only. The existing exact-hash Ghidra
validator fragment at0x140567af0 was reused and agrees with the bounded
instruction mapping below; no new native execution or provider-readiness
attempt occurred. No installation, launch or attach was needed.

[#595](https://github.com/NexusForever/NexusForever/pull/595) was refreshed:
still open/draft at ba30ae14513461ab6bd83d691543b6a155846e1c, base
3588ca5f578387928e6593e3808bf15f6192b094. Its kind12/mode1 proposal agrees
with the recovered rows/text. The implementation uses this fork's existing
Heal producer and tracked state rather than importing its architecture.
Bounded online searches found no usable final-build primary witness for
stacking/Focus; they supplied no policy used here.

### Owning behavior

UnitEntity stores personal registrations in its existing tracked property-state
collection, with the original caster object as scope. They are excluded from
whole-unit property calculation and read only by CalculateHealing for that
caster. A recycled GUID or different target cannot inherit the bonus.
A second caster's instance coexists; reapplication replaces only the same
caster/spell/effect tuple. Existing multiplicative aggregation is retained for
applicable registrations, without claiming retail arbitration.

The effect info retains the concrete ISpellPropertyInstance. Lifetime removal
uses reference identity; an old or rejected effect cannot remove a replacement
with a reused numeric ID. Existing tracked dispel/spell removal sees the same
registration. Kind12 cannot fall back to the global property mapper.
Unknown modes, nonzero extra operands/group-list, missing caster, and invalid
multipliers are rejected. Other effect151 kinds retain their separate legacy
behavior and unresolved evidence boundaries.

Global incoming/outgoing UnitPropertyModifier behavior, health cap, healing
absorption, overheal and combat logs remain in the established producer.
Personal health-healing state is not promoted into shield healing without a
shield-specific witness. Existing global shield modifiers still apply.
No Focus seed, AMP auto-application, migration or runtime authoring query changed.

### Verification and review

- Baseline:3 failures on publication parent2c8e0603, retained under
  artifacts/healing-personal-scope/baseline.
- Final selected Game.Tests graph:214 passed,0 failed/0 skipped (2s execution),
  including26 new cases. Covers real UnitEntity/Heal calculator, live-health
  Heal handler, two casters, replacement/removal, rejection, global modifiers,
  absorption and related AMP, conversion, proc-lifetime and channel controls.
  Channel tests qualify existing dispatcher cadence, not native healing ticks.
- Actual-catalog runner:15/15 kind12 rows pass; actual modifier rows with a
  controlled100-point heal, not a complete real-spell coefficient/targeting cast.
- Incremental owning Game build:0 warnings/errors,3.89s. The selected test graph
  emits the existing ASPIRE010 warning; no full solution or full suite was run.
- Primary-agent consolidated spec review: supported row/text meaning matches
  caster scope; stale removal and ordinary property semantics are preserved.
  Standards review: bounded owning changes, no authoring database access, no
  unrelated files or credentials. No remaining actionable findings.

Source and owning/test/runner DLL hashes are in the portable record. Raw REA,
upstream patches, row/text audit, failed controls, TRX and runner stay under
artifacts/healing-personal-scope and are not transferred by Git.

### Remaining witness

Native/UI execution, real spell targeting/tier stacking, full cast/tick lifetime,
death/map-transition lifecycle, shield-specific personal applicability, other
kind/mode semantics and Focus rates remain open. One kind13 row is a named test
spell, with extra DataBits04=1; its tooltip does not justify changing all kind13
behavior. Do not infer Focus1000 or recovery0.005/0.02 from this result.

Next integrated work should select another available gameplay producer from the
canonical remaining-work ledger, or close one of these questions with a new
specific primary witness. Do not repeat the recovered-input search, migrate
Focus seeds, or treat this milestone as completed spell/full restoration.
Computer Use/native client remain stopped.

## Historical evidence checkpoints

## Build 16042 effect 151 row validator

4 November 2026. The retained WildStar64 executable has SHA-256
`231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5`.
REA PE evidence
`ev_da27f0ff8de8665b152bc739b7654db7aa4f78845a3d9775591172e68e5ccf3d`
binds the image. The REA objdump provider could not start its configured
executable on Windows. Installed LLVM and direct PE reads supplied the
instruction and dispatch evidence without launching the target.

`Spell4Manager_BuildRuntimeData` calls the validator at `0x140560227`.
The zero return continues to `0x140560260`. A nonzero return follows
the diagnostic path and skips that insertion path. The validator starts
at `0x140567af0`. Its switch reads the effect type at row offset
`0x0c` and subtracts one before indexing the byte table.

| Effect | Byte entry | Slot | RVA entry | Target |
| --- | --- | --- | --- | --- |
| 11, UnitPropertyModifier | `0x1405681d2` | 6 | `0x140568190` | `0x140567c85` |
| 151, PersonalDmgHealMod | `0x14056825e` | 18 | `0x1405681c0` | `0x140568133` |

The image's Spell4Effects field descriptors bind `effectType`, `durationTime`,
`flags`, `dataBits00` and `dataBits01` to row offsets `0x0c`, `0x1c`,
`0x20`, `0x24` and `0x28` respectively. For effect 151, flag bit `0x2`
skips the unsigned 200 ms minimum. Otherwise a lower duration returns 13.
The surviving path jumps to `0x140567cb7`, where zero `dataBits01` returns
15 and a nonzero value succeeds in this branch. Effect 11 goes through the
`dataBits00` UnitProperty2 lookup at `0x1402479c0` before reaching that
shared code. The effect 151 branch bypasses the lookup.

These bytes corroborate #595's validation claim. This is mapped row validation.
The validator does not establish kind 12 caster-target ownership, actual row
values, mode meaning, stacking, effect application or Focus rates. The next
evidence remains the actual rows and tooltip bindings plus an independently
corroborated application or healing path. Server behavior awaits that evidence.
Local evidence is in `artifacts/continuation-20261104/healing-validator/`,
including the verifier, dispatch addresses, named field descriptors and
four bounded LLVM extracts.

## Original source comparison

1 November 2026 read-only continuation while auction-posting full validation runs.
This is a source comparison and evidence-gap record, not a new healing runtime
implementation or Verified retail semantics. Source/Tools and test binaries were
not changed. Computer Use/native client launch remain stopped.

## Reconciled upstream identity

[#595](https://github.com/NexusForever/NexusForever/pull/595) remains open/draft at
head ba30ae14513461ab6bd83d691543b6a155846e1c, matching the assessment's saved
pull-requests.json. Its current base is expedition-evil-from-the-ether at
3588ca5f578387928e6593e3808bf15f6192b094. This is not a missed new head. All30
changed-file records and both discussion comments are retained in
artifacts/continuation-20261031/healing-personal-mod/upstream595.json. One large
migration-designer patch is null in the response; that is not proof of absence.

Upstream proposes Focus pool1000 and recovery0.005/0.02, explicitly qualified as
video-derived and requiring packet verification. Its migration also distinguishes
the UnitProperty2 default0.011 from those proposed server rates. None of these
statements supplies independent server/runtime corroboration. Do not import
these numbers or regenerate the fork's seed/migration as a completeness claim.

## Concrete source comparison

The fork already has registered Heal/HealShields producers in
Source/NexusForever.Game/Spell/SpellEffectHandler.Vitals.cs; they call separate
health/shield calculators. Do not reimplement that existing integration merely
because #595 supplies different architecture.

The remaining ownership question is effect151 PersonalDmgHealMod:

- Upstream's PersonalDmgHealModKind/handler describes kind12,mode1 as a modifier
  for healing from this caster onto the affected unit. It asserts145 shipped rows,
  three supporting tooltips, float DataBits02 and a nonzero-mode/duration validator.
  These are primary-source claims to corroborate, not fresh native/table findings.
- The fork's SpellEffectInterpretation bit-casts DataBits02 and carries DataBits01
  as Priority. SpellEffectHandler.HandleEffectPersonalDmgHealMod maps kind12 to
  HealingMultiplierIncoming and kind13 to HealingMultiplierOutgoing, then adds a
  whole-unit SpellPropertyModifier. DamageCalculator.CalculateHealing multiplies
  the caster's outgoing and target's incoming properties without caster-pair scope.
- Therefore, if the upstream kind12 interpretation is verified, the fork's current
  unit property would also affect other casters healing that target. The conditional
  mismatch is source-grounded; the retail ownership conclusion remains unverified.

## Shipped artifact and missing corroboration

Fresh REA inspect_pe_imports Evidence
ev_6bbb62def40231982221ac00f91d138b55e4693d7186915c933f7dac27870f90 binds
C:/GIT/_archive/cluster-3d/NexusForever/Decomp/Client64/WildStar64.exe,13,927,576bytes,
SHA256231bb2bb3fc6c37f3e8a43a0ba965cc3645287bbc6ccad83d073a495c396b3e5.
Complete inline Evidence is retained in healing-personal-mod/rea-pe-imports.json.
Import inspection proves input identity and import-table observations only; it
does not establish healing ownership, validator branches or execution.

REA6.8.0 reports no active target and Ghidra not_configured (GHIDRA_INSTALL_DIR
unset). No unsupported deep-analysis attempt, setup/configuration change, target
launch or attach occurred. Existing LLVM/static caches remain available, but no
new effect151 validator/producer address was established in this pass.

A bounded, case-insensitive filename search of the archived NexusForever root
found no Spell4Effects/Spell4/UnitProperty2/GameText .tbl/.tbl.sql inputs or named
wildstar MySQL dumps. This is that root/search's limit, not global data absence.
The previously recorded Spell4Effects hash is
ad49cb47af98a4b9b85e5f715af85545a8b286fb3ca1f26ad8cf8ea09097b3ed.
No unchanged RawCommunicator lookup was repeated.

Next evidence: recover those effect151 rows and the three exact tooltip bindings,
bind the validator body to this client, and corroborate caster scope/mode/stacking/
lifetime independently. A future implementation must exercise the real Heal
producer with two casters, exact source-effect ownership and replacement/removal
controls; preserve global UnitPropertyModifier behavior, healing absorption and
channel handling. Broader AMP auto-application and Focus seed changes remain
separate. Missing native/UI/actual-table/provider evidence is not replaced by
synthetic source tests. The overall assessment Goal remains active.
