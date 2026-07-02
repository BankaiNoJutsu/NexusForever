using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity.Movement.Spline.Template;
using NexusForever.Game.Entity.Movement.Spline;
using NexusForever.Game.Entity.Movement.Spline.Mode;
using NexusForever.Game.Entity.Movement.Spline.Template;
using NexusForever.Game.Entity.Movement.Spline.Type;
using NexusForever.Game.Static.Entity.Movement.Spline;

namespace NexusForever.Game.Tests.Entity;

public class SplineTests
{
    [Fact]
    public void GetPosition_DelayedLinearSplinePausesBeforeInterpolating()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddTransient<SplineTypeLinear>()
            .AddTransient<SplineModeOneShot>()
            .BuildServiceProvider();

        var spline = new Spline(
            new SplineTypeFactory(provider),
            new SplineModeFactory(provider));

        spline.Initialise(new TestSplineTemplate
        {
            Type = SplineType.Linear,
            Points =
            [
                new SplineTemplatePoint { Position = Vector3.Zero },
                new SplineTemplatePoint { Position = Vector3.Zero, Delay = 2f },
                new SplineTemplatePoint { Position = new Vector3(10f, 0f, 0f) },
                new SplineTemplatePoint { Position = new Vector3(10f, 0f, 0f) }
            ]
        }, SplineMode.OneShot, speed: 1f);

        Assert.Equal(0f, spline.GetPosition().X, 3);

        spline.Update(1d);
        Assert.Equal(0f, spline.GetPosition().X, 3);

        spline.Update(1d);
        Assert.Equal(0f, spline.GetPosition().X, 3);

        spline.Update(1d);
        Assert.Equal(1f, spline.GetPosition().X, 3);
    }

    [Fact]
    public void GetPosition_CyclicLinearSplineWrapsToStartAndContinuesForward()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddTransient<SplineTypeLinear>()
            .AddTransient<SplineModeCyclic>()
            .BuildServiceProvider();

        var spline = new Spline(
            new SplineTypeFactory(provider),
            new SplineModeFactory(provider));

        spline.Initialise(new TestSplineTemplate
        {
            Type = SplineType.Linear,
            Points =
            [
                new SplineTemplatePoint { Position = Vector3.Zero },
                new SplineTemplatePoint { Position = Vector3.Zero },
                new SplineTemplatePoint { Position = new Vector3(10f, 0f, 0f) },
                new SplineTemplatePoint { Position = new Vector3(10f, 0f, 0f) }
            ]
        }, SplineMode.Cyclic, speed: 10f);

        spline.Update(0.5d);
        Assert.Equal(5f, spline.GetPosition().X, 3);
        Assert.Equal(SplineDirection.Forward, spline.Direction);

        spline.Update(0.6d);

        Assert.False(spline.IsFinialised);
        Assert.Equal(1f, spline.GetPosition().X, 3);
        Assert.Equal(SplineDirection.Forward, spline.Direction);
    }

    private sealed class TestSplineTemplate : ISplineTemplate
    {
        public SplineType Type { get; init; }
        public List<ISplineTemplatePoint> Points { get; init; } = [];
    }
}
