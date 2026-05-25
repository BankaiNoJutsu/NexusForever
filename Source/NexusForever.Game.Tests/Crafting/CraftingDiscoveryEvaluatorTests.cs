using System.Numerics;
using NexusForever.Game.Crafting;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Crafting;

public class CraftingDiscoveryEvaluatorTests
{
    [Fact]
    public void IsCoordinateDiscoverySchematic_ReturnsTrueWhenDiscoverableFlagSet()
    {
        var schematic = new TradeskillSchematic2Entry { Flags = CraftingDiscoveryEvaluator.DiscoverableSchematicFlag };

        Assert.True(CraftingDiscoveryEvaluator.IsCoordinateDiscoverySchematic(schematic));
    }

    [Fact]
    public void Evaluate_UsesCritRadiusHotRadiusAndWarmRadiusBands()
    {
        var schematic = new TradeskillSchematic2Entry
        {
            VectorX            = 100f,
            VectorY            = 3f,
            Radius             = 3.25f,
            CritRadius         = 0.5f,
            DiscoverableRadius = 0.5f
        };

        Assert.Equal(CraftingDiscovery.Success, CraftingDiscoveryEvaluator.Evaluate(schematic, new Vector2(100f, 3f)).Result);
        Assert.Equal(CraftingDiscovery.Hot, CraftingDiscoveryEvaluator.Evaluate(schematic, new Vector2(102f, 3f)).Result);
        Assert.Equal(CraftingDiscovery.Warm, CraftingDiscoveryEvaluator.Evaluate(schematic, new Vector2(103.5f, 3f)).Result);
        Assert.Equal(CraftingDiscovery.Cold, CraftingDiscoveryEvaluator.Evaluate(schematic, new Vector2(110f, 3f)).Result);
    }

    [Fact]
    public void TryParseDiagnosticAttemptCoordinates_ReadsPackedCircuitCompleteCoordinates()
    {
        var craftStats = new CraftStats { CircuitComplete = 1098u | (3u << 16) };

        Assert.True(CraftingDiscoveryEvaluator.TryParseDiagnosticAttemptCoordinates(craftStats, 0u, out Vector2 attempt));
        Assert.Equal(new Vector2(1098f, 3f), attempt);
    }

    [Theory]
    [InlineData(22u, 0u, 0u, CraftingStationServiceKey.RunecraftingTradeSkill)]
    [InlineData(1u, 0u, 4u, CraftingStationServiceKey.TierZeroFlaggedSchematic)]
    [InlineData(1u, 1u, 4u, CraftingStationServiceKey.DefaultSchematic)]
    [InlineData(5u, 2u, 0u, CraftingStationServiceKey.DefaultSchematic)]
    public void GetForSchematic_MatchesNativeServiceKeyRules(uint tradeSkillId, uint tier, uint flags, uint expected)
    {
        Assert.Equal(expected, CraftingStationServiceKey.GetForSchematic(tradeSkillId, tier, flags));
    }
}
