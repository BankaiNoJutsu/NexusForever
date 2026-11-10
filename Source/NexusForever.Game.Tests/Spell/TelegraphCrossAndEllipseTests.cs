using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Spell;

// The client's telegraph geometry builder (FUN_14054d170, Decomp/Analysis/TELEGRAPH_SHAPES_3_AND_6_20261107.md) switches on the shape and reads Param00..02 through FUN_14054da10:
//  - shape 2 (Square) and shape 7 (Rectangle) build a box (x = Param00, y = Param01, z = Param02) from extents scaled by 0.5, i.e. the parameters are full dimensions;
//  - shape 3 builds the same box on its first pass and, on a second pass, the box with x and z swapped (x = Param02, y = Param01, z = Param00): a cross of two bars;
//  - shape 6 stores (Param00, Param01, Param02) unscaled as a three-axis extent vector with a full 2*pi sweep and no inner radius: a circle generalised to three radii.
// Retail rows: shape 3 has 104 rows (for example 6/8/12 and 3/5/26), shape 6 has 159 rows (10/5/5, 20/10/10, 60/25/25 ...). Before this change both fell into "Unhandled telegraph shape" and hit
// nobody. Status: Implemented, inferred: the axis assignment comes from the builder, the hit rule reuses the server's Square convention and the ellipse reading of shape 6 is not confirmed against a capture.
public class TelegraphCrossAndEllipseTests
{
    // Cross: Param00 = 2 (full bar width), Param01 = 10 (full height), Param02 = 20 (full bar length): the client scales every box extent by 0.5, so the parameters are full dimensions
    // (half width 1, half height 5, half length 10). Rotation 0 faces -Z, so the long bar runs along Z and the second bar along X.
    [Theory]
    [InlineData(0f, 0f, -8f, true)]       // along the long Z bar
    [InlineData(0f, 0f, 8f, true)]
    [InlineData(8f, 0f, 0f, true)]        // along the second bar (x and z swapped)
    [InlineData(-8f, 0f, 0f, true)]
    [InlineData(0.5f, 0f, -9f, true)]     // inside the bar width
    [InlineData(0f, 0f, 0f, true)]        // centre
    [InlineData(3f, 0f, 3f, false)]       // between the bars
    [InlineData(5f, 0f, 5f, false)]
    [InlineData(0f, 0f, 11f, false)]      // beyond the bar end
    [InlineData(11f, 0f, 0f, false)]
    [InlineData(1.5f, 0f, 8f, false)]     // outside the bar width
    public void ACrossCoversTwoPerpendicularBars(float x, float y, float z, bool expected)
    {
        Telegraph telegraph = Shape(DamageShape.Cross, 2f, 10f, 20f);

        Assert.Equal(expected, telegraph.InsideTelegraph(new Vector3(x, y, z), 0f));
    }

    [Theory]
    [InlineData(4f, true)]
    [InlineData(5.5f, false)]
    [InlineData(-4f, true)]
    [InlineData(-5.5f, false)]
    public void ACrossHasTheVerticalToleranceOfItsSecondParameter(float y, bool expected)
    {
        Telegraph telegraph = Shape(DamageShape.Cross, 2f, 10f, 20f);

        Assert.Equal(expected, telegraph.InsideTelegraph(new Vector3(0f, y, -5f), 0f));
        Assert.Equal(expected, telegraph.InsideTelegraph(new Vector3(5f, y, 0f), 0f));
    }

    [Fact]
    public void ACrossUsesAGridSearchLargeEnoughForItsBarCorners()
    {
        Telegraph telegraph = Shape(DamageShape.Cross, 2f, 10f, 20f);

        // a target at the end of a bar corner (1, 10) is sqrt(101) from the centre
        Assert.True(telegraph.InsideTelegraph(new Vector3(0.9f, 0f, -9.9f), 0f));
        Assert.True(telegraph.SearchRadius >= MathF.Sqrt(101f));
    }

    // Shape 6: Param00 = 10 (x radius), Param01 = 5 (vertical), Param02 = 5 (z radius).
    [Theory]
    [InlineData(9f, 0f, 0f, true)]        // along the long X axis
    [InlineData(-9f, 0f, 0f, true)]
    [InlineData(11f, 0f, 0f, false)]
    [InlineData(0f, 0f, 4.9f, true)]      // along the short Z axis
    [InlineData(0f, 0f, 6f, false)]
    [InlineData(7f, 0f, 3.5f, true)]      // (0.7)^2 + (0.7)^2 = 0.98
    [InlineData(8f, 0f, 4f, false)]       // 0.64 + 0.64 = 1.28: inside the bounding box, outside the ellipse
    [InlineData(0f, 4f, 0f, true)]
    [InlineData(0f, 6f, 0f, false)]       // above the vertical extent
    [InlineData(0f, -6f, 0f, false)]
    public void AThreeAxisShapeCoversAnEllipseWithItsThreeRadii(float x, float y, float z, bool expected)
    {
        Telegraph telegraph = Shape(DamageShape.ThreeAxis, 10f, 5f, 5f);

        Assert.Equal(expected, telegraph.InsideTelegraph(new Vector3(x, y, z), 0f));
    }

    [Fact]
    public void AThreeAxisShapeRotatesWithTheTelegraph()
    {
        // A quarter turn swaps which axis the long radius lies on.
        Telegraph telegraph = Shape(DamageShape.ThreeAxis, 10f, 5f, 5f, rotation: new Vector3(MathF.PI / 2f, 0f, 0f));

        Assert.True(telegraph.InsideTelegraph(new Vector3(0f, 0f, 9f), 0f));
        Assert.False(telegraph.InsideTelegraph(new Vector3(9f, 0f, 0f), 0f));
    }

    [Fact]
    public void AThreeAxisShapeIsCentredOnTheTelegraphPosition()
    {
        Telegraph telegraph = Shape(DamageShape.ThreeAxis, 10f, 5f, 5f, position: new Vector3(100f, 5f, 200f));

        Assert.True(telegraph.InsideTelegraph(new Vector3(109f, 5f, 200f), 0f));
        Assert.False(telegraph.InsideTelegraph(new Vector3(9f, 0f, 0f), 0f));
    }

    [Fact]
    public void AThreeAxisShapeWidensByHalfTheTargetHitRadius()
    {
        Telegraph telegraph = Shape(DamageShape.ThreeAxis, 10f, 5f, 5f);

        Assert.False(telegraph.InsideTelegraph(new Vector3(10.6f, 0f, 0f), 0f));
        Assert.True(telegraph.InsideTelegraph(new Vector3(10.6f, 0f, 0f), 2f));     // hit radius 2 counts as 1
    }

    [Fact]
    public void AThreeAxisShapeUsesAGridSearchOfItsLargestRadius()
    {
        Assert.True(Shape(DamageShape.ThreeAxis, 10f, 5f, 5f).SearchRadius >= 10f);
        Assert.True(Shape(DamageShape.ThreeAxis, 5f, 3f, 12f).SearchRadius >= 12f);
    }

    // Pie: the hit rule (upstream NexusForever, not re-derived here) accepts targets out to Param01 that lie outside the Param02 wedge. The grid search radius was 0 for this shape, so only
    // the grid cell of the telegraph origin was searched and farther targets the hit rule accepts were never tested. Retail rows: 48, Param01 from 3 to 100.
    // Status: internal consistency fix only; no retail parity claim for the Pie hit rule itself.
    [Theory]
    [InlineData(3f)]
    [InlineData(35f)]
    [InlineData(100f)]
    public void APieUsesAGridSearchThatCoversTheRadiusItsHitRuleAccepts(float radius)
    {
        Telegraph telegraph = Shape(DamageShape.Pie, 0f, radius, 90f);

        // behind the telegraph (outside the wedge) and just inside the radius
        Assert.True(telegraph.InsideTelegraph(new Vector3(0f, 0f, radius - 0.5f), 0f));
        Assert.True(telegraph.SearchRadius >= radius);
    }

    private static Telegraph Shape(DamageShape shape, float param00, float param01, float param02, Vector3? position = null, Vector3? rotation = null)
    {
        var entry = new TelegraphDamageEntry
        {
            Id              = 6,
            DamageShapeEnum = (uint)shape,
            Param00         = param00,
            Param01         = param01,
            Param02         = param02
        };

        IUnitEntity caster = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.HitRadius), 0f);

        return new Telegraph(entry, caster, position ?? Vector3.Zero, rotation ?? Vector3.Zero);
    }
}
