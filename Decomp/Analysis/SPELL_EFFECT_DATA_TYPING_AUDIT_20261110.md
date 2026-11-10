# Spell effect data words: server reads versus the client typing table (2026-11-10)

Evidence tier: **raw table** (`SpellEffectType.tbl`, 150 rows: Id, Flags, DataType00..09) and **static source scan**. The typing says how a word is encoded, not what it means. No server behaviour was changed.
No retail parity claim.

## 1. What the table gives
`RESTORATION_GAP8_SPELL_EFFECT_NATIVE_READS_20261108.md` section 0 established that the client validator treats `DataType` 0 as float and 1 as integer. Across all effect types the codes are: 0 (float) 115
words, 1 (integer) 17 words, 3 on three words (UnitPropertyModifier 02 to 04, which the server reads as floats), and 2 on the remaining 1,365. Code 2 is not "unused": it also covers ids and flags (for example
UnitPropertyModifier word 00 is the property id), so it only says "not validated as a number".

`scripts/spell_effect_data_type_audit.py` joins that table with retail population counts (`Spell4Effects`, 131,010 rows) and with how the server reads each word in `SpellEffectInterpretation.cs` and the
`[SpellEffectHandler]` methods (bit-cast to float or used as an integer). Output: `coverage/spell_effect_data_type_audit.csv`.

Scanner limit: reads made elsewhere (`UnitEntity`, `WorldEntity`, `DamageCalculator`, rule classes) are not attributed to an effect type, so "server does not read" means "not read in the interpreter or the
handler method".

## 2. Results

### 2.1 Where the table confirms the server
The damage family agrees with the table on every word the server reads: Damage (8), DistanceDependentDamage (12), DistributedDamage (33) and DamageShields (138) word 00 float; Heal (10) and HealShields (118)
word 00 float; Absorption (29) and HealingAbsorption (144) words 00 and 01 float; Transference (5) words 02 and 05 float.

### 2.2 Encoding mismatches: 10, none populated
Kill (149) words 00 to 05 and PersonalDmgHealMod (151) words 06 to 09 are typed float and read as integers, but no retail row populates any of them. No effect.

### 2.3 Words the client types as a number, retail rows populate, and the server does not read

| Effect | Word | Table | Retail rows populated | Common values |
|---|---|---|---:|---|
| Proxy (26) | 04 | int | 16,928 of 16,929 | 1 (8,508), 0xFFFFFFFF (7,846), 10, 60, 3, 100, 12, 5 |
| Proxy (26) | 03 | float | 16,814 of 16,929 | 1.0 (15,873), 100.0 (604), 0.2, 0.5, 0.6, 0.7 |
| Damage (8) | 04 / 05 / 03 | float | 2,194 / 769 / 522 of 29,445 | 1.0, 0.75, 0.5, 0.25 |
| ProxyLinearAE (19) | 02 / 01 / 06 / 05 | float | 445 / 371 / 139 / 12 of 521 | not sampled |
| DistributedDamage (33) | 03 / 04 / 05 | float | 117 / 19 / 3 of 123 | not sampled |
| DistanceDependentDamage (12) | 03 / 04 | float | 7 / 2 of 10 | not sampled |
| CCStateSet (4) | 01 / 08 | float / int | 85 / 67 of 7,185 | not sampled |
| Disguise (31) | 01 | float | 26 of 544 | not sampled |
| Resurrect (16) | 00 / 01 | float | 19 / 8 of 20 | not sampled |
| DamageShields (138) | 04 | float | 5 of 5 | not sampled |
| ForcedAction (24), SpellCounter (2), FacilityModification (71), RestedXpDecorBonus (109), VacuumLoot (62), RewardBuffModifier (113) | | | | unhandled types, see `SPELL_EFFECT_REMAINING_20261108.md` |

**Proxy is the one with reach.** Proxy is how one spell casts another, and the server reads only word 00 (the Spell4 id). Almost every row also fills an integer word that is 1 or -1 in 97% of rows and a
float that is 1.0 in 94%.

Competing readings of Proxy word 04 (integer):
1. *Maximum number of times the proxy may fire per execution of the parent spell, -1 unlimited.* For: the value shape (1, -1, small counts); the upstream `spells-v2` handler implements exactly this
   (`if (count >= info.Entry.DataBits04) return;`) with the comment that an Esper builder may generate one resource per cast however many targets it hits. Against: that implementation says "seems to indicate";
   no native or retail witness.
2. *A stack or instance limit on the proxied spell.* For: the same value shape. Against: nothing specific.

Competing readings of Proxy word 03 (float): a trigger chance (1.0 = always) with a second scale present in the data (100.0 on 604 rows), or a multiplier passed to the child. Not separable from the table.

**Resolved the same day** from the tick-count equality and the Telekinetic Strike tooltip: word 04 is the maximum number of executions, and the child is cast by the caster of the parent. See
`SPELL_EFFECT_PROXY_20261110.md`; implemented as inferred. Word 03 is still unread.

The unread Damage floats (words 03 to 05) and the DistributedDamage and DistanceDependentDamage floats are consistent with the tracker's note that distance falloff and target-count splitting are not modelled;
which word is which is not known.

### 2.4 Words the server reads as float where the table says raw
RewardPropertyModifier (50) word 03, SapVital (30) word 05, Scale (18) word 04, DisguiseOutfit (112) words 04 and 05, SupportStuck (130) word 00. Code 2 does not contradict a float reading, so these are only
flagged for whoever revisits those handlers.

## 3. Reproduce
```
mysql ... wildstar_client -N -e "select * from spelleffecttype order by ID" > types.tsv
mysql ... wildstar_client -N -e "select effectType, count(*), sum(dataBits00<>0), sum(dataBits01<>0), sum(dataBits02<>0), sum(dataBits03<>0), sum(dataBits04<>0), sum(dataBits05<>0), sum(dataBits06<>0), sum(dataBits07<>0), sum(dataBits08<>0), sum(dataBits09<>0) from spell4effects group by effectType" > usage.tsv
python Decomp/Analysis/scripts/spell_effect_data_type_audit.py types.tsv usage.tsv
```
