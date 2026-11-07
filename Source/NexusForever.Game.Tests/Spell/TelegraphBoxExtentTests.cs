using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Spell;

// The client's geometry builder (FUN_14054d170, Decomp/Analysis/TELEGRAPH_BOX_EXTENTS_20261107.md) multiplies the Square (2), Cross (3) and Rectangle (7) extents (Param00 width, Param01 height,
// Param02 length) by the constant 0.5 on all three axes and, for the Rectangle, shifts the box by exactly that half length so it starts at the telegraph origin. A 0.5 is only needed when the
// parameters are FULL dimensions drawn with a mesh that spans -1..1, and the Rectangle shift only makes the box span 0..Param02 when Param02 is the full length. The server used the width and
// height as half extents (a Square or Rectangle twice as wide as drawn, with twice the vertical tolerance) and a Square length of -Param02..+Param02. Status: Implemented, inferred (the box mesh
// size was not read).
public class TelegraphBoxExtentTests
{
    // Square: width 4 (x within +-2), height 6 (y within +-3), length 10 (z within +-5).
    [Theory]
    [InlineData(1.9f, 0f, 0f, true)]
    [InlineData(-1.9f, 0f, 0f, true)]
    [InlineData(2.1f, 0f, 0f, false)]      // the server used to reach 4
    [InlineData(0f, 0f, 4.9f, true)]
    [InlineData(0f, 0f, -4.9f, true)]
    [InlineData(0f, 0f, 5.1f, false)]      // the server used to reach 10
    [InlineData(1.5f, 0f, 4.5f, true)]
    [InlineData(2.5f, 0f, 4.5f, false)]
    [InlineData(0f, 2.9f, 0f, true)]
    [InlineData(0f, 3.1f, 0f, false)]      // the server used to tolerate 6
    [InlineData(0f, -3.1f, 0f, false)]
    public void ASquareSpansItsFullDimensionsCentredOnTheTelegraph(float x, float y, float z, bool expected)
    {
        Telegraph telegraph = Box(DamageShape.Square, 4f, 6f, 10f);

        Assert.Equal(expected, telegraph.InsideTelegraph(new Vector3(x, y, z), 0f));
    }

    // Rectangle: width 4 (x within +-2), height 6, length 10 measured from the origin.
    [Theory]
    [InlineData(1.9f, 0f, true)]
    [InlineData(-1.9f, 0f, true)]
    [InlineData(2.1f, 0f, false)]          // the server used to reach 4
    [InlineData(0f, 3.1f, false)]
    [InlineData(0f, 2.9f, true)]
    public void ARectangleSpansItsFullWidthAndHeight(float x, float y, bool expected)
    {
        Telegraph telegraph = Box(DamageShape.Rectangle, 4f, 6f, 10f);

        // the rectangle's origin is at its base, so test at mid length (the sign of its forward axis is the existing server convention)
        bool forwardPositive = telegraph.InsideTelegraph(new Vector3(0f, 0f, 5f), 0f);
        float z = forwardPositive ? 5f : -5f;
        Assert.Equal(expected, telegraph.InsideTelegraph(new Vector3(x, y, z), 0f));
    }

    [Fact]
    public void ARectangleLengthStaysFullLengthFromTheOrigin()
    {
        Telegraph telegraph = Box(DamageShape.Rectangle, 4f, 6f, 10f);

        bool forwardPositive = telegraph.InsideTelegraph(new Vector3(0f, 0f, 9.5f), 0f);
        float sign = forwardPositive ? 1f : -1f;
        Assert.True(telegraph.InsideTelegraph(new Vector3(0f, 0f, 9.5f * sign), 0f));
        Assert.False(telegraph.InsideTelegraph(new Vector3(0f, 0f, 10.5f * sign), 0f));
        Assert.False(telegraph.InsideTelegraph(new Vector3(0f, 0f, -1f * sign), 0f));     // behind the origin
    }

    [Theory]
    [InlineData(DamageShape.Square)]
    [InlineData(DamageShape.Rectangle)]
    public void TheGridSearchReachesTheFarCornerOfTheBox(DamageShape shape)
    {
        // height 1 is smaller than the box: the search radius must not be limited by it
        Telegraph telegraph = Box(shape, 4f, 1f, 30f);
        float farCorner = shape == DamageShape.Square ? MathF.Sqrt(2f * 2f + 15f * 15f) : MathF.Sqrt(2f * 2f + 30f * 30f);

        Assert.True(telegraph.SearchRadius >= farCorner);
    }

    private static Telegraph Box(DamageShape shape, float width, float height, float length)
    {
        var entry = new TelegraphDamageEntry
        {
            Id              = 7,
            DamageShapeEnum = (uint)shape,
            Param00         = width,
            Param01         = height,
            Param02         = length
        };

        IUnitEntity caster = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.HitRadius), 0f);

        return new Telegraph(entry, caster, Vector3.Zero, Vector3.Zero);
    }
}
