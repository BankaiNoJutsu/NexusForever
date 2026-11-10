# Damage (effect type 8): words 03, 04 and 05 (2026-11-10)

Evidence tier: **raw tables** (`Spell4Effects`, `Spell4`) and **game tooltip text** (Jabbithole `spells.flavor`, an independent rendering of the client tooltip). The client does not execute spell
effects, so there is no native consumer. No retail parity claim.

Of 29,445 Damage rows, word 04 is set on 2,194, word 05 on 769 and word 03 on 522. `SpellEffectType.tbl` types all three as floats. Before this pass the server read words 00 (multiplier) and
01 (flat amount) only. Words 04 and 05 are now read; word 03 needs chained damage, which the server does not have.

## 1. Word 04 is the fraction of armor the row ignores. Status: Implemented, inferred.

Tooltips that state an armor pierce percentage, against word 04 of the Damage rows of the same ability:

| Ability | Damage rows | Word 04 | Tooltip |
|---|---|---:|---|
| Spellslinger True Shot | "True Shot - Tap 1" (`36052` and tiers, 28 rows) | 0.35 | "Deal ... physical damage to 5 foes and pierce 35% armor" |
| Spellslinger True Shot, Spell Surge | "True Shot - Surge - Tap 2" (`36054` and tiers, 28 rows) | 0.5 | "Spell Surge: Armor Pierce increased to 50%" |
| Spellslinger Charged Shot, Spell Surge | "Charged Shot - Surge C2" (`34765` and tiers, 27 rows) | 0.5 | "Spell Surge: ... C2+: Pierce 50% armor" |
| Spellslinger Wild Barrage, Spell Surge | "Wild Barrage - Surge Damage" (`34787` and tiers, 9 rows) | 0.5 | "Spell Surge: Deal ... physical damage and pierce 50% Armor" |
| Warrior Breaching Strikes | "Breaching Strikes - 2nd Hit" (`42371` and tiers, 16 rows) | 0.2 | "Pierces 20% Armor" |

`scripts/damage_armor_pierce_tooltip_check.py` runs the comparison over every tooltip: 41 tooltips state a percentage; for 19 the stated value is found in word 04 of a Damage row reachable from
the tooltip spell through its proxies (True Shot 0.35, Breaching Strikes 0.2 and 0.8, Bellow 0.5); 13 reach no Damage row at all (charge and threshold abilities whose stages are not proxies);
9 reach only rows with 0. Those 9 are the nine tiers of Wild Barrage, whose pierce belongs to the Spell Surge variant, a separate spell (`spell4IdMechanicAlternateSpell`) that does carry 0.5.
No tooltip states a percentage that contradicts the word.

Not explained by a tooltip: 0.38 on Stalker Shred, 0.2 on Stalker Whiplash, 0.3 on Stalker Impale, 0.6 on Warrior Savage Strikes, 0.25 on Esper Blade Dance, 0.08 on Esper Telekinetic Storm,
whose tooltips do not mention armor. 0.75 (511 rows) is almost entirely boss and instance abilities, and 1.0 (1,404 rows) is hazards, housing explosions and scripted damage.

Implemented: `SpellEffectDamageSemantics.ArmorPierce` (Damage rows only, clamped to 0..1) and `DamageCalculator.ApplyArmorMitigation`, which multiplies the victim's armor by `1 - pierce`
before the mitigation formula. **Assumed, not evidenced:** that the fraction scales the armor term and leaves the flat mitigation offsets alone (the tooltips only say "armor"), and that it does
not combine with an attacker armor pierce stat, which the server does not model at all. The other damage effect types (DistanceDependentDamage, DistributedDamage, DamageShields) populate
word 04 too and were not sampled; they do not read it.

## 2. Word 03 is a per-jump damage scalar on chained damage. Status: Read from the spell names; not implemented.

The designers named it in the spell descriptions: "[DEPRECATED] Medic Chain Shock - 2.4x initial, .66 scalar" carries word 03 = 0.66; "[DEPRECATED] Spellslinger Plasma Barrage Ricochet" 0.66;
"Spellslinger Barrage 2 Jump" 0.5; "Elementalist Forked Lightning 1 Jump" 1.0. Nearly every row with a value other than 1.0 is a deprecated or test spell; the live rows (creature telegraphs
such as Gorganoth Ground Pound and Stone Rain) carry 1.0, which is no reduction. The server has no chained damage, so nothing reads the word. Evidence tier: designer naming on four spell
families; no tooltip, no native consumer.

## 3. Word 05 is the fraction shields cannot absorb. Status: Implemented, inferred (added later the same day).

| Witness | Row | Word 05 | Text |
|---|---|---:|---|
| Stalker AMP Cutthroat | Damage row `181556` of "Stalker - Support - C2 - T2 - Damage" `70965`, reached from the AMP spell `59338` through its proc | 1.0 (word 04 is 0) | tooltip: "at 10 stacks the mark is consumed and deals 21 technology damage. This damage ignores Shields." |
| "Generic Instakill" `29207` | flat 999,999 damage | 0.99, with 0.99 in word 04 as well | a designer kill spell that sets both mitigation words |
| Consumable and hazard damage | `52147` (244 flat), `63534` (1000 flat), `27936` (16% of maximum health) | 1.0 | tooltips state the exact amount: "Deals 244 damage every second", "taking 1000 damage every 3s", "16% of your max health damage every 3s" |

1.0 on 706 rows, fractions on 63 (Spellslinger Charged Shot C3 0.75 and C4 0.5, landmines and scripted explosions 0.75, Engineer Bolt Caster tier 9 0.5).

**The one tooltip that seemed to disagree is stale text.** Spell4 `57506` says "This damage has 100% Shield Pierce" for Medic Chemical Burn, while its damage row (`147124` of
`60765`) carries 0. `57506` is only the item-unlock spell of the AMP (one UnlockInlaidAugment effect, AMP 745). The AMP itself (`59012`) is described in Jabbithole's AMP table as
"Landing a Multi-Hit deals an additional 27 technology damage every 1s for 4s", with no shield clause. The 0 matches the current description.

Implemented: `SpellEffectDamageSemantics.ShieldPierce` (Damage rows only, clamped to 0..1) and `DamageCalculator.CalculateShieldAmount`, which scales the share of the damage a shield may
absorb by `1 - pierce`. **Assumed:** that scaling form for fractions; 1.0 and 0.99 are not sensitive to it. The earlier second reading (no random spread) is dropped: it explained none of
the fractions. No retail parity claim.

## 4. Reproduce
```
python Decomp/Analysis/scripts/damage_armor_pierce_tooltip_check.py
```
Needs the `wildstar_client` and `jabbithole` reference databases (`-EnableDataMappingAuthoring`).
