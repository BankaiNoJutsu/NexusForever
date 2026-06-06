# WildStar Wiki Archive Audit

- Archive: `I:\GIT\NexusForever\artifacts\wildstar-fandom-wiki-2026-05-18`
- Client SQL: `I:\GIT\NexusForever\wildstar_client_mysql`
- Domains: abilities

## Ability Wiki Page Coverage

- Ability infobox pages: 208
- Class combat ability types audited: Assault, Support, Utility

| Class | Assault pages | Support pages | Utility pages | Innate pages | Result |
| --- | --- | --- | --- | --- | --- |
| Warrior | 10 | 10 | 10 | 1 | PASS |
| Engineer | 10 | 10 | 10 | 2 | PASS |
| Esper | 10 | 10 | 10 | 1 | PASS |
| Medic | 10 | 10 | 10 | 1 | PASS |
| Spellslinger | 10 | 10 | 10 | 1 | PASS |
| Stalker | 10 | 10 | 10 | 3 | PASS |

## Client SpellLevel Unlock Rows

| Class | Client SpellLevel rows | Result |
| --- | --- | --- |
| Warrior | 48 | PASS |
| Engineer | 48 | PASS |
| Esper | 48 | PASS |
| Medic | 48 | PASS |
| Spellslinger | 48 | PASS |
| Stalker | 48 | PASS |

## Wiki Ability Unlocks Compared To Client SpellLevel

- Current wiki combat ability pages with unlock levels: 180
- Exact wiki/client unlock-level matches: 81
- Wiki pages matched to different client unlock levels: 98
- Wiki pages without current client SpellLevel match: 1

| Class | Exact matches | Client-level differences | No current client match | Result |
| --- | --- | --- | --- | --- |
| Warrior | 3 | 26 | 1 | INFO |
| Engineer | 28 | 2 | 0 | INFO |
| Esper | 11 | 19 | 0 | INFO |
| Medic | 30 | 0 | 0 | INFO |
| Spellslinger | 6 | 24 | 0 | INFO |
| Stalker | 3 | 27 | 0 | INFO |

### Unlock Exceptions

| Ability | Class | Type | Wiki unlock | Client levels | Client spell ids | Result |
| --- | --- | --- | --- | --- | --- | --- |
| Electrocute | Engineer | Assault | 2 | 1 | 41276 | INFO |
| Zap | Engineer | Utility | 3 | 1 | 41438 | INFO |
| Mind Burst | Esper | Assault | 2 | 1 | 32809 | INFO |
| Bolster | Esper | Support | 9 | 7 | 32821 | INFO |
| Mending Banner | Esper | Support | 15 | 20 | 33345 | INFO |
| Mental Boon | Esper | Support | 24 | 18 | 33257 | INFO |
| Mirage | Esper | Support | 18 | 16 | 33366 | INFO |
| Phantasmal Armor | Esper | Support | 11 | 6 | 32823 | INFO |
| Pyrokinetic Flame | Esper | Support | 21 | 9 | 36821 | INFO |
| Reverie | Esper | Support | 9 | 6 | 33181 | INFO |
| Soothe | Esper | Support | 13 | 12 | 33140 | INFO |
| Warden | Esper | Support | 27 | 24 | 33392 | INFO |
| Catharsis | Esper | Utility | 15 | 20 | 33270 | INFO |
| Fade Out | Esper | Utility | 15 | 12 | 33081 | INFO |
| Fixation | Esper | Utility | 18 | 16 | 33272 | INFO |
| Geist | Esper | Utility | 6 | 7 | 36155 | INFO |
| Incapacitate | Esper | Utility | 21 | 10 | 33359 | INFO |
| Meditate | Esper | Utility | 18 | 24 | 32818 | INFO |
| Projected Spirit | Esper | Utility | 31 | 9 | 36233 | INFO |
| Restraint | Esper | Utility | 13 | 14 | 32815 | INFO |
| Shockwave | Esper | Utility | 24 | 22 | 32819 | INFO |
| Arcane Missiles | Spellslinger | Assault | 31 | 24 | 43570 | INFO |
| Assassinate | Spellslinger | Assault | 18 | 16 | 38905 | INFO |
| Charged Shot | Spellslinger | Assault | 2 | 1 | 34718 | INFO |
| Flame Burst | Spellslinger | Assault | 15 | 5 | 46690 | INFO |
| Rapid Fire | Spellslinger | Assault | 11 | 12 | 35356 | INFO |
| True Shot | Spellslinger | Assault | 21 | 20 | 36052 | INFO |
| Wild Barrage | Spellslinger | Assault | 6 | 8 | 34772 | INFO |
| Astral Infusion | Spellslinger | Support | 11 | 9 | 35870 | INFO |
| Dual Fire | Spellslinger | Support | 15 | 7 | 39068 | INFO |
| Healing Salve | Spellslinger | Support | 13 | 10 | 39121 | INFO |
| Healing Torrent | Spellslinger | Support | 18 | 16 | 39116 | INFO |
| Regenerative Pulse | Spellslinger | Support | 24 | 22 | 39646 | INFO |
| Sustain | Spellslinger | Support | 27 | 18 | 43326 | INFO |
| Vitality Burst | Spellslinger | Support | 9 | 6 | 39132 | INFO |
| Voidspring | Spellslinger | Support | 21 | 14 | 39134 | INFO |
| Affinity | Spellslinger | Utility | 31 | 22 | 39063 | INFO |
| Arcane Shock | Spellslinger | Utility | 15 | 10 | 46160 | INFO |
| Flash Freeze | Spellslinger | Utility | 9 | 7 | 34353 | INFO |
| Gate | Spellslinger | Utility | 3 | 1 | 34355 | INFO |
| Gather Focus | Spellslinger | Utility | 18 | 14 | 39330 | INFO |
| Phase Shift | Spellslinger | Utility | 27 | 24 | 35170 | INFO |
| Spatial Shift | Spellslinger | Utility | 24 | 20 | 30006 | INFO |
| Void Pact | Spellslinger | Utility | 18 | 16 | 52233 | INFO |
| Void Slip | Spellslinger | Utility | 13 | 12 | 58428 | INFO |
| Clone | Stalker | Assault | 18 | 16 | 38973 | INFO |
| Concussive Kicks | Stalker | Assault | 24 | 22 | 39671 | INFO |
| Cripple | Stalker | Assault | 11 | 14 | 38825 | INFO |
| Impale | Stalker | Assault | 2 | 1 | 38779 | INFO |
| Neutralize | Stalker | Assault | 13 | 8 | 38838 | INFO |
| Punish | Stalker | Assault | 15 | 5 | 48584 | INFO |
| Ruin | Stalker | Assault | 6 | 9 | 38841 | INFO |
| Amplification Spike | Stalker | Support | 18 | 16 | 39642 | INFO |
| Decimate | Stalker | Support | 18 | 9 | 48033 | INFO |
| Frenzy | Stalker | Support | 21 | 6 | 39366 | INFO |
| Nano Dart | Stalker | Support | 31 | 24 | 39581 | INFO |
| Nano Field | Stalker | Support | 6 | 18 | 39180 | INFO |
| Nano Virus | Stalker | Support | 15 | 10 | 39361 | INFO |
| Razor Disk | Stalker | Support | 11 | 12 | 39268 | INFO |
| Razor Storm | Stalker | Support | 27 | 6 | 39561 | INFO |
| Steadfast | Stalker | Support | 27 | 7 | 48117 | INFO |
| Whiplash | Stalker | Support | 9 | 6 | 39157 | INFO |
| Bloodthirst | Stalker | Utility | 18 | 16 | 39497 | INFO |
| Collapse | Stalker | Utility | 24 | 20 | 39372 | INFO |
| False Retreat | Stalker | Utility | 6 | 10 | 39246 | INFO |
| Pounce | Stalker | Utility | 9 | 7 | 42676 | INFO |
| Preparation (ability) | Stalker | Utility | 21 | 24 | 50184 | INFO |
| Reaver | Stalker | Utility | 9 | 22 | 39371 | INFO |
| Stagger | Stalker | Utility | 3 | 1 | 38791 | INFO |
| Stim Drone | Stalker | Utility | 31 | 20 | 39565 | INFO |
| Tactical Retreat | Stalker | Utility | 15 | 14 | 38912 | INFO |
| Tether Mine | Stalker | Utility | 13 | 12 | 38860 | INFO |
| Augmented Blade | Warrior | Assault | 11 | 24 | 46935 | INFO |
| Breaching Strikes | Warrior | Assault | 21 | 9 | 32353 | INFO |
| Power Strike | Warrior | Assault | 2 | none | none | INFO |
| Ripsaw | Warrior | Assault | 6 | 22 | 70016 | INFO |
| Savage Strike | Warrior | Assault | 6 | 9 | 57248 | INFO |
| Smackdown | Warrior | Assault | 15 | 20 | 57978 | INFO |
| Tremor | Warrior | Assault | 18 | 16 | 36227 | INFO |
| Whirlwind | Warrior | Assault | 9 | 5 | 33802 | INFO |
| Atomic Spear | Warrior | Support | 15 | 12 | 32129 | INFO |
| Atomic Surge | Warrior | Support | 27 | 6 | 32351 | INFO |
| Bolstering Strike | Warrior | Support | 18 | 16 | 32345 | INFO |
| Bum Rush | Warrior | Support | 27 | 20 | 59080 | INFO |
| Expulsion | Warrior | Support | 24 | 18 | 47214 | INFO |
| Jolt | Warrior | Support | 9 | 7 | 54342 | INFO |
| Plasma Wall | Warrior | Support | 13 | 10 | 38787 | INFO |
| Polarity Field | Warrior | Support | 18 | 8 | 38964 | INFO |
| Shield Burst | Warrior | Support | 31 | 6 | 57694 | INFO |
| Defense Grid | Warrior | Utility | 21 | 10 | 54475 | INFO |
| Emergency Reserves | Warrior | Utility | 15 | 12 | 32358 | INFO |
| Flash Bang | Warrior | Utility | 11 | 14 | 32320 | INFO |
| Grapple | Warrior | Utility | 13 | 7 | 32132 | INFO |
| Kick | Warrior | Utility | 3 | 1 | 58591 | INFO |
| Plasma Blast | Warrior | Utility | 9 | 14 | 32342 | INFO |
| Power Link | Warrior | Utility | 18 | 16 | 53599 | INFO |
| Sentinel | Warrior | Utility | 18 | 24 | 54395 | INFO |
| Tether Bolt | Warrior | Utility | 31 | 22 | 49557 | INFO |
| Unstoppable Force | Warrior | Utility | 24 | 18 | 30568 | INFO |

## Server Ability Grant Surface

| Implementation surface | Present | Result |
| --- | --- | --- |
| Uses client SpellLevel rows | yes | PASS |
| Filters by player class and level | yes | PASS |
| Respects SpellLevel prerequisites | yes | PASS |
| Grants Spell4 base ids | yes | PASS |
| Grants class innate and primary attacks | yes | PASS |
| Creates ability-book spell items | yes | PASS |

## Summary

Result: PASS

Warnings:
- 1 wiki combat ability page(s) have no current client SpellLevel match: Power Strike
- 98 wiki combat ability unlock level(s) differ from client SpellLevel data; client SpellLevel rows remain the implementation authority.
