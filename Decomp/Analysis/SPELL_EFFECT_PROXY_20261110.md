# Proxy (effect type 26): execution limit and who casts the child (2026-11-10)

Evidence tier: **raw tables** (`Spell4Effects`, `SpellEffectType`), **game tooltip text** (Jabbithole `spells.flavor`, an independent rendering of the client tooltip) and table statistics. The client does not
execute spell effects, so there is no native consumer; the video corpus (`I:\tools\video_index\corpus.db`, 8,448 videos) was searched for a spoken statement and has none. Status: **Implemented, inferred.**
No retail parity claim.

## 1. DataBits04 is the maximum number of executions

Three independent observations:

1. **Client typing.** `SpellEffectType.tbl` types Proxy word 04 as an integer (code 1); only 17 words in the whole table carry that code.
2. **Tick count equality.** Among Proxy rows that tick (`tickTime != 0`), have a duration and a finite word 04, the value equals `durationTime / tickTime` in 52 of 76 rows: 12 with 6000 ms / 500 ms (44 rows),
   5 with 5000 / 1000, 6 with 30000 / 5000, 10 with 10000 / 1000, 20 with 10000 / 500. Ticking proxies are otherwise almost always `0xFFFFFFFF` (755 of 791 without a duration, 664 of 751 with one), while
   non-ticking proxies split 8,357 with 1 against 6,285 with `0xFFFFFFFF`.
3. **Tooltip witness.** Telekinetic Strike, Spell4 `32893`:

   | Effect row | Type | TargetFlags | Data |
   |---|---|---|---|
   | 62159 | Damage | 4 (area targets) | |
   | 62204 | Proxy | 4 (area targets) | DataBits00 = 30900, DataBits03 = 1.0, DataBits04 = 1, DataBits05 = 1000 |

   Spell `30900` is `VitalModifier` on target flag 1 (its caster): vital 6, amount 1. The tooltip reads "Deal 33.6%AP +18*lvl magic damage to 5 foes and generate a Psi Point". A per-target proxy without a limit
   would grant one point per foe hit.

A fourth, weaker check: the server already special-cased Warrior Relentless Strikes (row `177057`, target flag 4, child `53865` = two caster-flag VitalModifiers). That row has word 04 = 1, so the general rule
reproduces the hand-written case.

Rejected reading: a stack limit on the child spell. Spell `30900` is an instant vital change with nothing to stack, and the tick equality has no stack interpretation.

### Open
* **Scope with several targets on a ticking proxy.** The count is kept per effect row for the life of the spell instance. For a ticking proxy that hits several targets per tick with a finite limit this may
  exhaust early; no witness row was examined.
* **Word 04 = 0** (about 115 rows) is treated as unlimited; nothing shows what it means.
* **DataBits03** (float, 1.0 on 15,873 rows, 100.0 on 604, fractions on others) and **DataBits05** (60 on 10,099 rows, 1 on 2,444, 120, 30, 900, 1000) are still unread. A trigger chance and an internal
  cooldown or range are candidates; the two scales in word 03 are unexplained.
* Upstream `spells-v2` names word 01 `PeriodicSpellId` and word 06 `PrerequisiteId`; the server reads neither for plain Proxy. 791 + 751 ticking rows have word 01 set.

## 2. The parent's caster casts the child

Previously a unit-target Proxy made the **target** cast the child on itself. The other proxy variants (ProxyLinearAE, ProxyChannel, ProxyRandomExclusive) and the world-object path already cast from the parent's
caster.

* Of the 4,748 distinct child spells of proxies whose target flags include an explicit or area target, 4,562 have effects on non-caster target flags. A spell cast by a unit on itself whose effects address
  an explicit target is degenerate; a spell cast by the original caster at the proxy target is not.
* Of the 186 caster-only children, 48 contain `VitalModifier` (the resource builders, which must reach the original caster for the tooltip to hold).
* Under the old rule a Damage child was cast by the victim on itself, where the handler's `target.CanAttack(spell.Caster)` test compares the victim with itself.

Against: 39 caster-only children contain `ForcedMove`, 6 `Damage` and 4 `CCStateSet`. If any of those are meant to act on the proxy target they now act on the original caster. They were not examined row by
row.

## 3. Server change
`SpellHandler.HandleEffectProxy` now consumes one execution against DataBits04 before anything else (including the existing Relentless Strikes and Artillerybot Barrage special cases) and casts the child from
`spell.Caster` with the proxy target as primary target. Tests: `SpellEffectCombatRegressionTests.HandleEffectProxy_WithExecutionLimitOne_...` (the Telekinetic Strike rows),
`..._CountsExecutionsAgainstTheLimitForTheLifeOfTheSpell`, and the existing position-forwarding test, which now expects the caster to cast. One existing test encoded the target-casts rule.

Not verified: the change was not run in a live session. A child spell now goes through primary-target validation (range, facing, valid-target mask) as a cast by the original caster at the proxy target,
where a self-cast passed trivially; children that fail that validation would stop firing. Watch `primary-target-validation` diagnostics for proxied spells in the first live pass.

The Artillerybot Barrage rows (`117258`, `159172`) have word 04 = `0xFFFFFFFF`, yet the server's special case fires them once per cast. That special case came from server-side play testing, not from retail
evidence; it was left as it is.
