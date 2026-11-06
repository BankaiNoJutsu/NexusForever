using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public sealed class CommunicatorReputationBoundaryTests
{
    // Build16042 14043d501..14043d524 bypasses a zero bound and compares
    // finite reputation inclusively. These cases do not establish retail UI.
    [Theory]
    [InlineData(0u, 0u, -1f, true)] [InlineData(0u, 0u, -100f, true)]
    [InlineData(0u, 0u, 0f, true)] [InlineData(0u, 0u, 1f, true)]
    [InlineData(0u, 20u, -1f, true)] [InlineData(0u, 20u, 20f, true)] [InlineData(0u, 20u, 21f, false)]
    [InlineData(10u, 0u, 9f, false)] [InlineData(10u, 0u, 10f, true)] [InlineData(10u, 0u, 100f, true)]
    [InlineData(10u, 20u, 9f, false)] [InlineData(10u, 20u, 10f, true)]
    [InlineData(10u, 20u, 20f, true)] [InlineData(10u, 20u, 21f, false)]
    [InlineData(10u, 10u, 10f, true)] [InlineData(20u, 10u, 15f, false)]
    public void FiniteReputationUsesInclusiveNonzeroBounds(uint minimum, uint maximum, float amount, bool eligible)
        => Check(minimum, maximum, amount, eligible);

    // Preserve the fork's existing nonfinite policy separately. Static finite
    // branches do not qualify native NaN/infinity or runtime invalid-data behavior.
    [Theory]
    [InlineData(0u, 0u, float.NaN, false)]
    [InlineData(0u, 0u, float.NegativeInfinity, false)]
    [InlineData(0u, 0u, float.PositiveInfinity, true)]
    [InlineData(10u, 0u, float.PositiveInfinity, true)]
    [InlineData(0u, 20u, float.PositiveInfinity, false)]
    [InlineData(10u, 20u, float.NaN, false)]
    public void NonfiniteReputationRetainsExistingServerPolicy(uint minimum, uint maximum, float amount, bool eligible)
        => Check(minimum, maximum, amount, eligible);

    private static void Check(uint minimum, uint maximum, float amount, bool eligible)
    {
        var row = new CommunicatorMessagesEntry { Id = 1, FactionIdReputation = 1,
            ReputationMin = minimum, ReputationMax = maximum };
        var message = new CommunicatorMessage(row, CommunicatorKind.ClientUnscheduled, [], []);
        Assert.Equal(eligible ? CommunicatorResult.Eligible : CommunicatorResult.Reputation, message.Evaluate(new View(amount)));
    }

    private sealed record View(float Amount) : ICommunicatorPlayerView
    {
        public uint WorldId => 0;
        public Class Class => default;
        public Race Race => default;
        public Faction Faction => default;
        public uint Level => 1;
        public bool IsInZone(uint zone) => true;
        public float GetReputation(Faction faction) => Amount;
        public CommunicatorQuestSnapshot? GetQuest(ushort quest) => null;
        public bool MeetsPrerequisite(uint prerequisite) => true;
        public CommunicatorOfferBlock CheckOffer(ushort quest) => CommunicatorOfferBlock.None;
    }
}
