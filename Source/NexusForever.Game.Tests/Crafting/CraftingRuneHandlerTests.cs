using NexusForever.Game.Static.Crafting;
using NexusForever.Network;
using NexusForever.WorldServer.Network.Message.Handler.Crafting;

namespace NexusForever.Game.Tests.Crafting;

public class CraftingRuneHandlerTests
{
    [Theory]
    [InlineData(7u, RuneType.Air)]
    [InlineData(12u, RuneType.Life)]
    public void NormalizeRuneType_DefaultPrefersFullRuneTypeIds(uint rawValue, RuneType expected)
    {
        RuneType result = CraftingRuneRequestHelper.NormalizeRuneType((RuneType)rawValue);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1u, RuneType.Air)]
    [InlineData(6u, RuneType.Life)]
    [InlineData(7u, RuneType.Fusion)]
    public void NormalizeRuneType_CompactFirstPrefersCompactSocketIds(uint rawValue, RuneType expected)
    {
        RuneType result = CraftingRuneRequestHelper.NormalizeRuneType((RuneType)rawValue, compactFirst: true);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(14u)]
    [InlineData(31u)]
    public void NormalizeRuneType_RejectsUnknownValues(uint rawValue)
    {
        Assert.Throws<InvalidPacketValueException>(() => CraftingRuneRequestHelper.NormalizeRuneType((RuneType)rawValue));
    }
}
