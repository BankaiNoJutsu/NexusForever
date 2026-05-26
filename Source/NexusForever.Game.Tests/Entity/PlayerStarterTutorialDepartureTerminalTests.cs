using System.Runtime.CompilerServices;
using NexusForever.Game.Entity;
using static NexusForever.Game.Static.Tutorial.StarterTutorialDefinition;

namespace NexusForever.Game.Tests.Entity;

public class PlayerStarterTutorialDepartureTerminalTests
{
    [Theory]
    [InlineData(ExileEverstarGroveDepartureTerminalCreatureId)]
    [InlineData(ExileNorthernWildsDepartureTerminalCreatureId)]
    [InlineData(DominionCrimsonIsleDepartureTerminalCreatureId)]
    [InlineData(DominionLevianBayDepartureTerminalCreatureId)]
    public void RecordStarterTutorialDepartureTerminalStoresKnownDepartureTerminal(uint terminalCreatureId)
    {
        Player player = CreatePlayer();

        player.RecordStarterTutorialDepartureTerminal(terminalCreatureId);

        Assert.Equal(terminalCreatureId, player.StarterTutorialDepartureTerminalCreatureId);
    }

    [Fact]
    public void RecordStarterTutorialDepartureTerminalIgnoresNonDepartureTerminal()
    {
        Player player = CreatePlayer();
        player.RecordStarterTutorialDepartureTerminal(ExileEverstarGroveDepartureTerminalCreatureId);

        player.RecordStarterTutorialDepartureTerminal(73419u);

        Assert.Equal(ExileEverstarGroveDepartureTerminalCreatureId, player.StarterTutorialDepartureTerminalCreatureId);
    }

    [Fact]
    public void RecordStarterTutorialDepartureTerminalIsStoredPerPlayer()
    {
        Player exilePlayer = CreatePlayer();
        Player dominionPlayer = CreatePlayer();

        exilePlayer.RecordStarterTutorialDepartureTerminal(ExileNorthernWildsDepartureTerminalCreatureId);
        dominionPlayer.RecordStarterTutorialDepartureTerminal(DominionLevianBayDepartureTerminalCreatureId);

        Assert.Equal(ExileNorthernWildsDepartureTerminalCreatureId, exilePlayer.StarterTutorialDepartureTerminalCreatureId);
        Assert.Equal(DominionLevianBayDepartureTerminalCreatureId, dominionPlayer.StarterTutorialDepartureTerminalCreatureId);
    }

    private static Player CreatePlayer()
    {
        return (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
    }
}
