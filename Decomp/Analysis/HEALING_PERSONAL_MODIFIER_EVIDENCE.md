# Healing personal-modifier evidence boundary

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
