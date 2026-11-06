using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

// Inputs: all 23 CommunicatorMessages rows with FactionIdReputation != 0 in the build 16042 client's
// DB\CommunicatorMessages.tbl (4,105 rows, 494,256 bytes, SHA-256
// 086346547b6e5f457ff25d121b5c555bf708a69a40b49db34c6870b18d137a75), extracted with Tools/ArchiveProbe.
// See Decomp/Analysis/RAW_TABLE_EXTRACTION_20261106.md. Evaluation uses the current predicate only:
// no native timing, UI or retail packet behavior is established here.
public sealed class CommunicatorRetailReputationRowsTests
{
    private static readonly (uint Id, uint Faction, uint Min, uint Max)[] Rows =
    [
        (8, 231, 500, 0), (17, 231, 1000, 0), (21, 231, 500, 0),
        (897, 533, 2000, 3999), (898, 533, 2000, 3999), (899, 533, 4000, 7999),
        (900, 533, 8000, 15999), (901, 533, 16000, 31999), (902, 533, 32000, 0),
        (1206, 609, 2000, 3999), (1207, 609, 2000, 3999), (1208, 609, 4000, 7999),
        (1209, 609, 8000, 15999), (1210, 609, 16000, 31999), (1211, 609, 32000, 0),
        (2609, 166, 0, 0), (3720, 167, 0, 0), (3879, 167, 0, 0), (5346, 166, 0, 0),
        (5443, 167, 0, 0), (5444, 166, 0, 0), (5445, 167, 0, 0), (5446, 166, 0, 0)
    ];

    [Fact]
    public void RetailRowsContainExactlyTheRecordedBoundShapes()
    {
        Assert.Equal(23, Rows.Length);
        Assert.Equal(8, Rows.Count(row => row.Min == 0 && row.Max == 0));
        Assert.Equal(5, Rows.Count(row => row.Min != 0 && row.Max == 0));
        Assert.Equal(10, Rows.Count(row => row.Min != 0 && row.Max != 0));
        Assert.DoesNotContain(Rows, row => row.Max != 0 && row.Max < row.Min);
    }

    [Fact]
    public void RetailTierBandsAreContiguousPerFaction()
    {
        foreach (var faction in new uint[] { 533, 609 })
        {
            var tiers = Rows.Where(row => row.Faction == faction).Select(row => (row.Min, row.Max)).Distinct()
                .OrderBy(tier => tier.Min).ToArray();
            for (int i = 0; i < tiers.Length - 1; i++)
                Assert.Equal(tiers[i].Max + 1, tiers[i + 1].Min);
            Assert.Equal(0u, tiers[^1].Max); // the top tier has no upper bound
        }
    }

    [Fact]
    public void EveryRetailRowIsEvaluatedAtItsBoundaries()
    {
        foreach (var (id, faction, min, max) in Rows)
        {
            Assert.Equal(Expect(min, max, min), Evaluate(id, faction, min, max, min));
            if (min != 0) Assert.Equal(CommunicatorResult.Reputation, Evaluate(id, faction, min, max, min - 1));
            else Assert.Equal(Expect(min, max, -1000), Evaluate(id, faction, min, max, -1000));
            if (max != 0)
            {
                Assert.Equal(CommunicatorResult.Eligible, Evaluate(id, faction, min, max, max));
                Assert.Equal(CommunicatorResult.Reputation, Evaluate(id, faction, min, max, max + 1));
            }
            else Assert.Equal(CommunicatorResult.Eligible, Evaluate(id, faction, min, max, 1_000_000));
        }
    }

    private static CommunicatorResult Expect(uint min, uint max, float amount)
        => (min == 0 || amount >= min) && (max == 0 || amount <= max) ? CommunicatorResult.Eligible : CommunicatorResult.Reputation;

    private static CommunicatorResult Evaluate(uint id, uint faction, uint min, uint max, float amount)
    {
        var row = new CommunicatorMessagesEntry { Id = id, FactionIdReputation = faction, ReputationMin = min, ReputationMax = max };
        return new CommunicatorMessage(row, CommunicatorKind.ClientUnscheduled, [], []).Evaluate(new View(amount));
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
