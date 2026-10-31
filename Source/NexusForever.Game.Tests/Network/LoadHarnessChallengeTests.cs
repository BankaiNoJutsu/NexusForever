using System.Buffers.Binary;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Challenges;
using NexusForever.Network.World.Message.Model.Reward;

namespace NexusForever.Game.Tests.Network;

public sealed class LoadHarnessChallengeTests
{
    private static byte[] Encode(IWritable message)
    {
        using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream); message.Write(writer); writer.FlushBits(); return stream.ToArray();
    }

    [Fact]
    public void NpcPrefixUsesTheActualEntityWriterAndDoesNotRequireUnmappedTailFields()
    {
        var bytes = Encode(new ServerEntityCreate { Guid = 987, Type = EntityType.NonPlayer, EntityModel = new NonPlayerEntityModel { CreatureId = 11907, QuestChecklistIdx = 3 } });
        Assert.Equal((987u, 11907u), ChallengeProtocol.ReadNpc(bytes));
        Assert.Equal((987u, 11907u), ChallengeProtocol.ReadNpc(bytes[..8]));
        Assert.ThrowsAny<Exception>(() => ChallengeProtocol.ReadNpc(bytes[..6]));
    }

    [Fact]
    public void OtherEntityModelsAreIgnoredAfterTheirMappedTypePrefix()
    {
        using var stream = new MemoryStream(); using var writer = new GamePacketWriter(stream); writer.Write(9u); writer.Write(EntityType.Player, 6); writer.FlushBits();
        Assert.Null(ChallengeProtocol.ReadNpc(stream.ToArray()));
    }

    [Fact]
    public void ChallengeFieldsRemainAlignedAcrossMultipleRowsAndFourPackedBooleans()
    {
        var message = new ServerChallengeUpdate { ActiveChallenges = [
            new() { ChallengeId = 103, Type = ChallengeType.Combat, TargetGroupId = 8851, QualifyCount = 17, QualityTotal = 18, CurrentCount = 4, GoalCount = 5, ObjectiveCompletion = 9,
                CurrentTier = 2, LastRewardTier = 1, CompletionCount = 7, Unlocked = true, Activated = true, OnCooldown = true, LeftArea = true, TimeActivatedDt = 123, TimeTotalActive = 300000,
                TimeCooldownDt = 456, TimeTotalCooldown = 1800000, TimeAreaFailDt = 789, TimeTotalAreaFail = 10000, TierGoalCount = [5, 6, 7] },
            new() { ChallengeId = 105, Type = ChallengeType.Ability, CurrentCount = 3, GoalCount = 20, LastRewardTier = 0, CompletionCount = 2, Activated = false, TierGoalCount = [20, 0, 0] }] };
        var rows = ChallengeProtocol.ReadChallenges(Encode(message));
        Assert.Equal(new ChallengeWireState(103, 4, 5, 2, 1, 7, true), rows[0]);
        Assert.Equal(new ChallengeWireState(105, 3, 20, 0, 0, 2, false), rows[1]);
    }

    [Fact]
    public void RewardRowsPreserveUnalignedClaimMasksSelectedRowsAndTheFinalActiveBit()
    {
        var mask = new NetworkBitArray(32, NetworkBitArray.BitOrder.LeastSignificantBit); mask.SetBit(0, true); mask.SetBit(31, true);
        var message = new ServerRewardTracksReload { RewardsTracks = [new() { RewardTrackId = 16, RewardPointsEarned = 1234, ClaimedRewards = mask,
            RewardTrackRewardsIds = Enumerable.Range(1, 32).Select(v => (uint)v).ToList(), Unlocked = 3, IsActive = true }, new() { RewardTrackId = 9, RewardTrackRewardsIds = [276, 312] }] };
        var rows = ChallengeProtocol.ReadRewards(Encode(message)); Assert.Equal(2, rows.Count); Assert.Equal(0x80000001u, rows[0].Claimed);
        Assert.Equal(1234u, rows[0].Points); Assert.True(rows[0].Active); Assert.Equal(32, rows[0].Rows.Count); Assert.Equal(9u, rows[1].Id); Assert.Equal(new uint[] { 276, 312 }, rows[1].Rows);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CollectionsRejectUnboundedCountsBeforeAllocating(bool rewards)
    {
        byte[] payload = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(payload, uint.MaxValue);
        Assert.Throws<InvalidDataException>(() => { if (rewards) ChallengeProtocol.ReadRewards(payload); else ChallengeProtocol.ReadChallenges(payload); });
    }

    [Fact]
    public void TheObserverUsesFullReloadReplacementAndLiveNpcRemoval()
    {
        var observer = new ChallengeSocketObserver(103);
        observer.Receive(new(GameMessageOpcode.ServerEntityCreate, Encode(new ServerEntityCreate { Guid = 9, Type = EntityType.NonPlayer, EntityModel = new NonPlayerEntityModel { CreatureId = 11907 } })));
        Assert.Equal(new uint[] { 9 }, observer.Targets(new HashSet<uint> { 11907 }));
        observer.Receive(new(GameMessageOpcode.ServerEntityDestroy, Encode(new ServerEntityDestroy { Guid = 9 }))); Assert.Empty(observer.Targets(new HashSet<uint> { 11907 }));
        observer.Receive(new(GameMessageOpcode.ServerRewardTracksReload, Encode(new ServerRewardTracksReload { RewardsTracks = [new() { RewardTrackId = 16, RewardTrackRewardsIds = [320] }] })));
        Assert.NotNull(observer.Reward(16)); observer.Receive(new(GameMessageOpcode.ServerRewardTracksReload, Encode(new ServerRewardTracksReload())));
        Assert.Null(observer.Reward(16)); Assert.Equal(2, observer.Reloads);
    }

    [Fact]
    public void ChoosingAnAlreadyActiveTrackStillRequiresItsNewPublishedReloadBeforeClaiming()
    {
        var observer = new ChallengeSocketObserver(103);
        var active = new GameFrame(GameMessageOpcode.ServerRewardTracksReload, Encode(new ServerRewardTracksReload
            { RewardsTracks = [new() { RewardTrackId = 16, IsActive = true, RewardTrackRewardsIds = [320, 321, 324] }] }));
        observer.Receive(active); long beforeChoose = observer.Reloads;
        Assert.True(observer.Reward(16)!.Active); Assert.False(observer.ChoicePublishedSince(16, beforeChoose));
        observer.Receive(active); Assert.True(observer.ChoicePublishedSince(16, beforeChoose));
        Assert.False(observer.ChoicePublishedSince(17, beforeChoose));
        observer.Receive(new(GameMessageOpcode.ServerRewardTracksReload, Encode(new ServerRewardTracksReload())));
        Assert.False(observer.ChoicePublishedSince(16, beforeChoose));
    }

    [Fact]
    public void CastPayloadRoundTripsThroughTheExistingMappedClientReader()
    {
        using var stream = new MemoryStream(); using (var writer = new GamePacketWriter(stream)) { ChallengeProtocol.WriteSelectedCast(writer, 123, 26434, 987, 4721, -732, -5680); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray())); var message = new ClientCastSpellSelected(); message.Read(reader);
        Assert.Equal(123u, message.ContextToken); Assert.Equal(26434u, message.SelectedEntryId); Assert.Equal(987u, message.TargetEntityId);
        Assert.Equal(new System.Numerics.Vector3(4721, -732, -5680), message.Position.Vector);
    }

    [Fact]
    public void AcceptedCastTargetIsRetainedOnlyForTheConfiguredSpellAndClearedWhenThatEntityDisappears()
    {
        var observer = new ChallengeSocketObserver(103, 26434);
        observer.Receive(new(GameMessageOpcode.ServerEntityCreate, Encode(new ServerEntityCreate { Guid = 9, Type = EntityType.NonPlayer, EntityModel = new NonPlayerEntityModel { CreatureId = 11907 } })));
        observer.Receive(new(GameMessageOpcode.ServerSpellStart, Encode(new ServerSpellStart { Spell4Id = 1, PrimaryTargetId = 9 })));
        Assert.Equal(0u, observer.AcceptedTarget);
        observer.Receive(new(GameMessageOpcode.ServerSpellStart, Encode(new ServerSpellStart { Spell4Id = 26434, PrimaryTargetId = 9 })));
        Assert.Equal(9u, observer.AcceptedTarget);
        observer.Receive(new(GameMessageOpcode.ServerEntityDestroy, Encode(new ServerEntityDestroy { Guid = 9 }))); Assert.Equal(0u, observer.AcceptedTarget);
    }

    [Theory]
    [InlineData(5u, false, 1u, true, true)] // A new attempt starts after a completed source row retained count5.
    [InlineData(1u, true, 2u, true, true)]
    [InlineData(2u, true, 2u, true, false)]
    [InlineData(2u, true, 1u, true, false)]
    public void ProgressRequiresANewAcceptedGoalEventRatherThanAnInitialReload(uint before, bool active, uint after, bool nextActive, bool expected)
        => Assert.Equal(expected, ChallengeSocketObserver.Advanced(new(103, before, 5, 0, 0, 1, active), new(103, after, 5, 0, 0, 1, nextActive)));

    [Theory]
    [InlineData("loadtest9701@example.local", "nexus_assessment_20261029_auth", "nexus_assessment_20261029_character", "127.0.0.1", true, true)]
    [InlineData("real@example.local", "nexus_assessment_20261029_auth", "nexus_assessment_20261029_character", "127.0.0.1", true, false)]
    [InlineData("loadtest9701@example.local", "nexus_forever_auth", "nexus_forever_character", "127.0.0.1", true, false)]
    [InlineData("loadtest9701@example.local", "nexus_assessment_a_auth", "nexus_assessment_b_character", "127.0.0.1", true, false)]
    [InlineData("loadtest9701@example.local", "nexus_assessment_a_auth", "nexus_assessment_a_character", "192.0.2.1", true, false)]
    [InlineData("loadtest9701@example.local", "nexus_assessment_a_auth", "nexus_assessment_a_character", "127.0.0.1", false, false)]
    public void OfflinePlacementCannotTargetOrdinaryAccountsOriginalSchemasRemoteServersOrMixedStacks(string user, string auth, string character, string host, bool allowed, bool valid)
    {
        string Connection(string schema) => $"server={host};user=test;password=test;database={schema}";
        if (valid) ChallengeRewardsCommand.ValidateFixture(user, Connection(auth), Connection(character), allowed);
        else Assert.Throws<InvalidOperationException>(() => ChallengeRewardsCommand.ValidateFixture(user, Connection(auth), Connection(character), allowed));
    }

    [Fact]
    public void RecipeValidationRejectsNonfiniteOrUnboundTargetsBeforeAnyFixtureWrite()
    {
        var recipe = new ChallengeRecipe(103, 16, 26434, 300, [11907], [new(18060, 11907, 426, 604, 4721, -732, -5680)]);
        ChallengeRewardsCommand.ValidateRecipe(recipe);
        Assert.Throws<InvalidDataException>(() => ChallengeRewardsCommand.ValidateRecipe(recipe with { Placements = [recipe.Placements[0] with { X = float.NaN }] }));
        Assert.Throws<InvalidDataException>(() => ChallengeRewardsCommand.ValidateRecipe(recipe with { Creatures = [1] }));
        Assert.Throws<InvalidDataException>(() => ChallengeRewardsCommand.ValidateRecipe(recipe with { MaximumPoints = 0 }));
    }
}
