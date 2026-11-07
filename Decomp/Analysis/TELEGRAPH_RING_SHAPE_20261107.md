# Telegraph ring shape (2026-11-07)

Status: **Implemented, inferred** (Evidence Ladder in `CONTINUATION_GUIDE.md`). The reading comes from the shape's enum name and the retail parameter pattern; the client's own handling of
the shape was not read. Verification is Source/SQLite only. No retail parity claim.

## Gap

`Telegraph.InsideTelegraph` handled shapes 0 (Circle), 2 (Square), 4 (Cone), 5 (Pie), 7 (Rectangle) and 8 (LongCone). Any other shape logged "Unhandled telegraph shape" and reported no hit, and
`GridSearchSize` returned 0 for it, so the map search never even looked for targets. Shape 1 (`DamageShape.Ring`) is the fifth most common shape in `TelegraphDamage.tbl`:

| Shape | Rows in TelegraphDamage | Spell4Telegraph links | Spell4 ids | Server |
|---|---|---|---|---|
| 0 Circle | 5,587 | 10,151 | 6,999 | handled |
| 4 Cone | 2,781 | 4,748 | 2,422 | handled |
| 2 Square | 1,445 | 3,361 | 897 | handled |
| 7 Rectangle | 1,413 | 3,990 | 2,195 | handled |
| **1 Ring** | **469** | **525** | **315** | **no hit before this change** |
| 6 | 159 | 168 | 67 | still no hit (see below) |
| 3 | 104 | 145 | 95 | still no hit (see below) |
| 8 LongCone | 79 | 1,087 | 768 | handled |
| 5 Pie | 48 | 48 | 36 | handled |

## Reading

Ring rows carry `Param00 < Param01` in 468 of 469 rows (for example 10/20, 5/10, 5.5/10), `Param02` is 0 in 392 rows and an angle in the rest (120 in 17, 15 in 13), `Param03..05` are 0. That is the
(start radius, radius, angle) layout cones use. The ring is read as the band between an inner radius (`Param00`) and an outer radius (`Param01`) around the telegraph position (flat XZ distance, like Circle),
widened by half the target hit radius at both edges as Circle and Cone do. A non-zero `Param02` limits the band to an arc of that angle in front of the telegraph through the existing cone angle test.

What would raise the status: the client's telegraph builder for shape 1 (how it reads `Param00..02`) or an accepted retail capture of a ring-telegraph hit. The single row with `Param00 >= Param01` (7931, 5.5/5.5)
is a zero-width band under this reading.

## Change

`Telegraph.InsideTelegraph` has a `Ring` case and `GridSearchSize` returns `Param01` for it. `TelegraphRingTests` (12 cases: band edges and hole, hit radius widening, centre offset, arc) failed 7 of 12 before
the change (the positive cases) and pass now. 1,137 Spell/Telegraph/Combat Source/SQLite cases pass (separate scope).

## Not changed

- Shape 6 (159 rows, parameters like 10/5/5 and 20/10/10) and shape 3 (104 rows, up to six parameters such as 1/15/11/11/0/1) remain unhandled; their meaning is not established (`DamageShape` comments
  call them "Aura related" and "a Cross shape").
- The cone angle line `Param02 + Param00 / 2` in the existing Cone case adds half the start radius (in degrees) to the angle. It was left as is: nothing here shows it is wrong, but nothing supports it either.
