using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Spell;

// Retail TelegraphDamage rows with DamageShapeEnum 1 ("Ring", 469 rows) carry an inner radius in Param00 and an outer radius in Param01 (inner < outer in 468 of 469 rows, for example
// 5/10 and 10/20) and Param02 is 0 (392 rows) or an arc angle (120 in 17, 15 in 13, ...), the same layout cones use for (start radius, radius, angle). The server returned "unhandled
// telegraph shape" and no hit for every one of them, so spells with a ring telegraph damaged nobody. Status: Implemented, inferred (the shape reading comes from the enum name and the
// parameter pattern; the client's own shape handling was not read). See Decomp/Analysis/TELEGRAPH_RING_SHAPE_20261107.md.
public class TelegraphRingTests
{
    [Theory]
    [InlineData(7f, 0f, true)]      // inside the ring
    [InlineData(5f, 0f, true)]      // on the inner edge
    [InlineData(10f, 0f, true)]     // on the outer edge
    [InlineData(3f, 0f, false)]     // in the hole
    [InlineData(12f, 0f, false)]    // beyond the outer radius
    [InlineData(0f, 0f, false)]     // at the centre
    [InlineData(11f, 2f, true)]     // hit radius 2 counts as 1 and reaches back to 10
    [InlineData(12f, 2f, false)]
    [InlineData(4f, 2f, true)]      // reaches forward to 5
    [InlineData(3f, 2f, false)]
    public void ARingCoversTheBandBetweenItsInnerAndOuterRadius(float distance, float hitRadius, bool expected)
    {
        Telegraph telegraph = Ring(inner: 5f, outer: 10f, angle: 0f);

        Assert.Equal(expected, telegraph.InsideTelegraph(new Vector3(distance, 0f, 0f), hitRadius));
        Assert.Equal(expected, telegraph.InsideTelegraph(new Vector3(0f, 0f, -distance), hitRadius));
        Assert.Equal(expected, telegraph.InsideTelegraph(new Vector3(0f, 0f, distance), hitRadius));
    }

    [Fact]
    public void ARingIsCentredOnTheTelegraphPosition()
    {
        Telegraph telegraph = Ring(inner: 5f, outer: 10f, angle: 0f, position: new Vector3(100f, 5f, 200f));

        Assert.True(telegraph.InsideTelegraph(new Vector3(107f, 5f, 200f), 0f));
        Assert.False(telegraph.InsideTelegraph(new Vector3(103f, 5f, 200f), 0f));
        Assert.False(telegraph.InsideTelegraph(new Vector3(7f, 0f, 0f), 0f));
    }

    [Fact]
    public void ARingWithAnArcOnlyCoversThatArcInFrontOfTheCaster()
    {
        // Rotation 0 faces -Z; a 90 degree arc covers 45 degrees to each side.
        Telegraph telegraph = Ring(inner: 5f, outer: 10f, angle: 90f);

        Assert.True(telegraph.InsideTelegraph(new Vector3(0f, 0f, -7f), 0f));     // straight ahead, in the band
        Assert.True(telegraph.InsideTelegraph(new Vector3(3f, 0f, -7f), 0f));     // about 23 degrees to the side
        Assert.False(telegraph.InsideTelegraph(new Vector3(0f, 0f, 7f), 0f));     // behind
        Assert.False(telegraph.InsideTelegraph(new Vector3(7f, 0f, 0f), 0f));     // 90 degrees to the side
        Assert.False(telegraph.InsideTelegraph(new Vector3(0f, 0f, -3f), 0f));    // ahead but in the hole
        Assert.False(telegraph.InsideTelegraph(new Vector3(0f, 0f, -12f), 0f));   // ahead but too far
    }

    private static Telegraph Ring(float inner, float outer, float angle, Vector3? position = null)
    {
        var entry = new TelegraphDamageEntry
        {
            Id              = 5,
            DamageShapeEnum = (uint)DamageShape.Ring,
            Param00         = inner,
            Param01         = outer,
            Param02         = angle
        };

        IUnitEntity caster = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.HitRadius), 0f);

        return new Telegraph(entry, caster, position ?? Vector3.Zero, Vector3.Zero);
    }
}
