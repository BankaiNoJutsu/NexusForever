using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Achievement;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Achievement;

// Native evaluator (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): type 201 (FUN_14049f810) looks up the achievement with id ObjectId and tests bit Value of its
// progress state (record +0x14, bits 0 to 31) or credited-checklist word (+0x18, bits 32 to 63), both fields the server sends in the Achievement record; Equal meets when the bit
// is set, NotEqual when it is clear (a missing achievement has no bits set), other comparisons never meet. Retail: 93 slots, NotEqual 71, Equal 22, Value 0 to 11.
public class AchievementChecklistBitPrerequisiteTests
{
    [Theory]
    [InlineData(0u, true)]
    [InlineData(1u, false)]
    [InlineData(2u, true)]
    [InlineData(5u, false)]
    [InlineData(31u, true)]
    [InlineData(32u, true)]   // credited word bit 0
    [InlineData(33u, false)]
    [InlineData(63u, true)]   // credited word bit 31
    [InlineData(64u, false)]  // beyond both words
    public void ReadsTheBitFromTheProgressStateAndCreditedWords(uint bit, bool expected)
    {
        TestManager manager = ManagerWith(progressState: 0x80000005u, creditedMask: 0x80000001u);

        Assert.Equal(expected, manager.IsChecklistBitSet(10, bit));
    }

    [Fact]
    public void AnAchievementThePlayerHasNotStartedHasNoBitsSet()
    {
        TestManager manager = ManagerWith(progressState: uint.MaxValue, creditedMask: uint.MaxValue);

        Assert.False(manager.IsChecklistBitSet(999, 0u));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 1u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 1u, false)]
    public void ComparesTheChecklistBitOfTheNamedAchievement(PrerequisiteComparison comparison, uint bit, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        ICharacterAchievementManager achievements = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var achievementProxy);
        achievementProxy.SetMethodHandler(nameof(IBaseAchievementManager<CharacterAchievementModel>.IsChecklistBitSet),
            args => (ushort)args[0] == 10 && (uint)args[1] == 0u);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievements);

        Assert.Equal(expected, new PrerequisiteCheckAchievementChecklistBit().Meets(player, comparison, bit, 10u, new PrerequisiteParameters()));
    }

    private static TestManager ManagerWith(uint progressState, uint creditedMask)
    {
        var info = new TestInfo(new AchievementEntry { Id = 10 }, new AchievementChecklistEntry { Bit = 0u, ObjectId = 1u });
        return new TestManager(new Achievement<CharacterAchievementModel>(1ul, info)
        {
            CompletedChecklistMask = progressState,
            CreditedChecklistMask  = creditedMask
        });
    }

    private sealed class TestInfo : IAchievementInfo
    {
        public TestInfo(AchievementEntry entry, params AchievementChecklistEntry[] checklist)
        {
            Entry            = entry;
            ChecklistEntries = checklist.ToList();
        }

        public ushort Id => (ushort)Entry.Id;
        public AchievementEntry Entry { get; }
        public List<AchievementChecklistEntry> ChecklistEntries { get; }
        public bool IsPlayerAchievement => true;
        public bool IsRealmFirst => false;
    }

    private sealed class TestManager : BaseAchievementManager<CharacterAchievementModel>
    {
        protected override ulong OwnerId => 1ul;

        public TestManager(IAchievement achievement)
        {
            achievements.Add(achievement.Id, achievement);
        }

        protected override void SendAchievementUpdate(IEnumerable<IAchievement> updates)
        {
        }

        public override void CheckAchievements(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt = 0u, uint count = 1u)
            => throw new NotSupportedException();

        public override void SetAchievementProgress(IPlayer target, AchievementType type, uint objectId, uint objectIdAlt, uint value)
            => throw new NotSupportedException();
    }
}
