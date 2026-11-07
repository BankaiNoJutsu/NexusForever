# Telegraph shapes 3 and 6 from the client geometry builder (2026-11-07)

Evidence tier: **native body read** for the axis assignment (`FUN_14054d170`); **Implemented, inferred** for the server hit rule (it reuses the server's Square convention and reads shape 6 as an ellipse). Source/SQLite only.
No retail parity claim.

## Found by
[TELEGRAPH_RING_SHAPE_20261107.md](TELEGRAPH_RING_SHAPE_20261107.md) traced the cast path to `FUN_1406007c0`/`FUN_14061a840` (no shape branch). A scan of every client function with a computed jump and a 0 to 8 `switch`
(`ShapeSwitch.java`, 558 functions scanned, 6 candidates) found two shape-keyed functions:
- `FUN_1406177c0`: a `switch` on the row field at `+4` that selects index/vertex data by *draw class*: {0, 2}, {1, 3}, {4}, {5, 6}, {7, 8}. This pairs shapes by mesh topology only and says nothing about parameter meaning.
- `FUN_14054d170`: the geometry builder. It `switch`es on the shape (row `+8`) and reads `Param00..` through `FUN_14054da10(this, index, default)`.

## What the builder does
| Shape | Builder |
|---|---|
| 0 Circle | radius = Param00, full sweep (`0x40c90fdb` = 2*pi at `+0x260`) |
| 1 Ring | Param00 (clamped at 0) and Param01 into the inner/outer slots |
| 2 Square | extent vector (x, y, z) = (Param00, Param01, Param02) times the constant `0.5` (`DAT_140b7ac50..5c` = 0.5 each); bounding radius = `sqrt(x*x + y*y + z*z)` |
| **3** | if the second-pass flag (`param_2`) is 0: the Square box (x, y, z) = (Param00, Param01, Param02); otherwise (x, y, z) = **(Param02, Param01, Param00)**. Same 0.5 scale. A **cross of two bars** |
| 5 Pie | Param0 inner, Param1 radius, Param2 angle |
| **6** | extent vector (Param00, Param01, Param02) stored **unscaled** at both `+0x220` and `+0x230`, sweep 2*pi, inner part zeroed: a Circle generalised to three radii |
| 7 Rectangle | as Square (0.5 scale) plus an origin shift of the z extent, i.e. the box starts at the origin |

## Server change
`DamageShape` gains `Cross = 3` and `ThreeAxis = 6`. `Telegraph.InsideTelegraph` now handles them: **Cross** is the union of two centred boxes, (width Param00, height Param01, length Param02) and the same with width and length swapped;
**ThreeAxis** is an ellipse with radius Param00 along local X, Param02 along local Z and a vertical tolerance of Param01, widened by half the hit radius and rotated with the telegraph. The Square/Rectangle code moved unchanged into
an `InsideBox` helper (no behaviour change). `GridSearchSize` returns the bar-corner distance for Cross and the larger of Param00 and Param02 for ThreeAxis (it returned 0, so no target was even searched).
Retail: shape 3 has 104 rows (for example 6/8/12 and 3/5/26) and shape 6 has 159 (10/5/5, 20/10/10, 30/15/15, 60/25/25; Param01 equals Param02 in all but a few).

## Open questions (recorded, not changed)
- **Scale of boxes.** The client multiplies the Square/Rectangle/Cross extents by 0.5 and the server's Square uses +-Param00 and +-Param02 as the polygon corners (full width 2 * Param00). If the client's 0.5 turns the parameters into half extents of a unit
  box, a Square's full width is Param00 and the server's hit width is twice the drawn width (the Rectangle's length is already 0 to Param02 in both). The renderer's mesh size was not read, so which is right is unresolved and the Square code was left as it was.
  Cross reuses the same convention so it stays consistent with Square whichever is right.
- **Shape 6 axis meaning.** Param01 equals Param02 in the common rows, which would also fit (vertical, radius X?) orders; the builder stores the vector (x, y, z) = (Param00, Param01, Param02), which the server reads as above. A retail capture of a shape 6 hit is the missing witness.
- Shape 3's extra parameters (Param03 to Param05 in 17 rows) are not read by this builder case.

## Tests
`TelegraphCrossAndEllipseTests` (32 cases): the red run was a compile failure (the shapes and `SearchRadius` did not exist). 1,247 Telegraph/Spell/Combat Source/SQLite cases pass (separate scope).
