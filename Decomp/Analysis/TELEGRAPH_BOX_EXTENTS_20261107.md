# Telegraph box extents are full dimensions (2026-11-07)

Evidence tier: **Correlated / Implemented, inferred.** The native body (`FUN_14054d170`) is read; the box mesh size that would make the reading Verified was not. Source/SQLite only. No retail parity claim. This **changes existing
behaviour** for the Square (1,445 retail rows) and Rectangle (1,413 rows) shapes, so the reasoning is recorded in full and the change is easy to revert (one helper, `Telegraph.InsideBox`).

## Evidence
`FUN_14054d170` builds the box extent vector for shapes 2 (Square), 3 (Cross) and 7 (Rectangle) as `(Param00, Param01, Param02) * 0.5` on all three axes (the constant at `DAT_140b7ac50..5c` is 0.5 in every lane; the vector's
length is stored as the bounding radius). For shape 7 the builder additionally writes `-(z extent)` as an origin shift (`param_4[1] = z ^ sign mask`), so the box starts at the telegraph origin instead of being centred on it. Shapes 0 (Circle),
5 (Pie) and 6 (three-axis) store their parameters **unscaled**.
- A scale of 0.5 is only needed when the stored value is a full dimension and the geometry spans -1..1 (half extent = dimension / 2). If the parameters were already half extents the builder would store them unscaled, as it does for the circle radius.
- For the Rectangle the shift equals exactly the scaled z extent (Param02 / 2), which places the box from 0 to Param02. That agrees with the server's own Rectangle length (z from 0 to Param02), which the previous code already implemented as a full length.
  The same 0.5 applies to the width, so the width must be Param00 in total (+-Param00 / 2), not 2 * Param00.

## What the server did
`InsideBox` used `+-Param00` as the half width (a box twice as wide as drawn), `Param01` as the vertical tolerance (twice the half height) and, for a Square, `+-Param02` as the half length (twice as long); only the Rectangle length was right.
`GridSearchSize` for both shapes returned `Param01` (the height), so a long thin box never even searched the targets near its far end.

## Change
`Telegraph.InsideBox` now takes full dimensions: half width, half height and (Square, Cross) half length are the parameter times 0.5; a Rectangle runs from the origin to its full length with half width Param00 / 2. The grid search for Square, Rectangle and
Cross reaches the box's far corner (and is never smaller than the old value). The three-axis shape 6 and the other shapes are unchanged. `TelegraphBoxExtentTests` (21 cases) failed 14 of 72 in the Telegraph filter before the change (the
over-wide, over-tall and over-long hits and the too-small search radius); `TelegraphCrossAndEllipseTests` was updated to full dimensions (same coordinates, parameters doubled). 1,266 Telegraph/Spell/Combat Source/SQLite cases pass (separate scope).

## Risk and how to falsify
If the client mesh spans -0.5..0.5 instead of -1..1 the drawn box is half the size assumed here and the old width was closer to right than the new one for some axes. The missing input is the box mesh's vertex extents in the render submission that follows
`FUN_14054b1b0`, or a retail capture of a Square or Rectangle hit boundary. To revert, restore `halfWidth/halfHeight/halfLength = width/height/length` in `Telegraph.InsideBox` and the grid search expressions.
