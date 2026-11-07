namespace NexusForever.Game.Static.Spell
{
    public enum DamageShape
    {
        Circle        = 0,
        Ring          = 1,
        Square        = 2,
        /// <summary>
        /// Two perpendicular bars: a box (Param00 width, Param01 height, Param02 length) plus the same box with width and length swapped.
        /// Inferred from the client geometry builder, see Decomp/Analysis/TELEGRAPH_SHAPES_3_AND_6_20261107.md.
        /// </summary>
        Cross         = 3,
        Cone          = 4,
        /// <summary>
        /// This is a pie shape, where the radius value is the size of the missing slice.
        /// </summary>
        Pie           = 5,
        /// <summary>
        /// Three radii (Param00 x, Param01 vertical, Param02 z) with a full sweep and no inner radius; read as an ellipse. Inferred from the client geometry builder.
        /// </summary>
        ThreeAxis     = 6,
        Rectangle     = 7,
        LongCone      = 8
    }
}
