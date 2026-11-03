using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class TradeskillPublicationTests
{
    [Fact]
    public async Task LostAcknowledgementPublishesOnlyAfterTheOriginalRetryReconciles()
    {
        using var f = await Create(); f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0);
        await Assert.ThrowsAsync<IOException>(() => f.Save(new LostAcknowledgement())); f.Pump(); Assert.Empty(f.Packets<ServerProfessionUpdate>());
        using (var read = f.Context()) Assert.Single(read.CharacterTradeskill);
        await f.Save(); f.Pump(); Assert.Single(f.Packets<ServerProfessionUpdate>());
        await f.Save(); f.Pump(); Assert.Single(f.Packets<ServerProfessionUpdate>());
    }

    [Fact]
    public async Task SavepointRollbackAndOuterCommitLeaveBothCollectionsAndNoticesPending()
    {
        using var f = await Create(); f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0); f.Actor.Player.LearnSchematic(501, true);
        await using (var context = f.Context()) await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await transaction.CreateSavepointAsync("before_progression"); f.Actor.Player.Save(context); await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before_progression"); await transaction.CommitAsync();
        }
        f.Pump(); Assert.Empty(f.Packets<ServerProfessionUpdate>()); Assert.Empty(f.Packets<ServerSchematicAddLearned>());
        using (var read = f.Context()) { Assert.Empty(read.CharacterTradeskill); Assert.Empty(read.CharacterSchematic); }
        await f.Save(); f.Pump(); Assert.Single(f.Packets<ServerProfessionUpdate>()); Assert.Single(f.Packets<ServerSchematicAddLearned>());
    }

    [Fact]
    public async Task FrozenNormalSaveReleasesOnlyItsCapturedNoticeVersions()
    {
        using var f = await Create(); EnableCurrency(f); f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0);
        f.Actor.OnAuth = () => f.Actor.Player.AddTradeskillXp(TradeskillType.Armorer, 20);
        await f.Actor.Player.SaveDirect(); await f.Actor.Drain(); f.Pump();
        Assert.Equal(0u, Assert.Single(f.Packets<ServerProfessionUpdate>()).Tradeskill.TradeskillXp);
        using (var read = f.Context()) Assert.Equal(0u, Assert.Single(read.CharacterTradeskill).TradeskillXp);
        f.Actor.OnAuth = null; await f.Actor.Player.SaveDirect(); await f.Actor.Drain(); f.Pump();
        Assert.Equal(new uint[] { 0, 20 }, f.Packets<ServerProfessionUpdate>().Select(row => row.Tradeskill.TradeskillXp));
        using var final = f.Context(); Assert.Equal(20u, Assert.Single(final.CharacterTradeskill).TradeskillXp);
    }

    [Fact]
    public async Task AlreadyActiveLearningNoticesCanConfirmAnUnchangedGraph()
    {
        using var f = await Create(existing: true);
        f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0); f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0);
        Assert.Empty(f.Packets<ServerProfessionUpdate>()); await f.Save(); f.Pump();
        Assert.Equal(2, f.Packets<ServerProfessionUpdate>().Count());
        using var read = f.Context(); Assert.Equal(180u, Assert.Single(read.CharacterTradeskill).TradeskillXp);
    }

    [Fact]
    public async Task QuestCallbackCanSaveWithoutLosingTheAcceptedLearningNotice()
    {
        using var f = await Create(); var quests = RecordingDispatchProxy<IQuestManager>.Create(out var callback); int saves = 0;
        callback.SetMethodHandler(nameof(IQuestManager.ObjectiveUpdate), _ =>
        {
            Assert.Empty(f.Packets<ServerProfessionUpdate>());
            using var context = f.Context(); f.Actor.Player.Save(context); context.SaveChanges(); saves++; return null;
        });
        f.Actor.Set(nameof(Player.QuestManager), quests);
        try { Assert.True(f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0)); }
        finally { f.Actor.Set(nameof(Player.QuestManager), f.Actor.Quests); }
        Assert.Equal(1, saves); Assert.Empty(f.Packets<ServerProfessionUpdate>()); f.Pump(); Assert.Single(f.Packets<ServerProfessionUpdate>());
        using var read = f.Context(); Assert.Single(read.CharacterTradeskill);
    }

    [Fact]
    public async Task FailedRecipeSendRetriesFreshPayloadWithoutRepeatingTheProfession()
    {
        using var f = await Create(); f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0); f.Actor.Player.LearnSchematic(501, true);
        int attempts = 0, professions = 0; var coordinates = new List<float>();
        f.Messages.SetMethodHandler("EnqueueMessageEncrypted", args =>
        {
            if (args[0] is ServerProfessionUpdate) professions++;
            if (args[0] is ServerSchematicAddLearned recipe)
            {
                if (attempts++ == 0) { recipe.DiscoveryCoordinates = new(999, 999); throw new IOException("Owned recipe send fault."); }
                coordinates.Add(recipe.DiscoveryCoordinates.X);
            }
            return null;
        });
        await f.Save(); f.Pump(); Assert.Equal(1, professions); Assert.Equal(2, attempts); Assert.Equal(new[] { 18f }, coordinates);
    }

    [Fact]
    public async Task ReentrantInitialSendCannotApplyAnOlderSkillAfterCommittedUpdates()
    {
        using var f = await Create(existing: true); bool entered = false; var delivered = new List<uint>();
        f.Messages.SetMethodHandler("EnqueueMessageEncrypted", args =>
        {
            if (args[0] is ServerProfessionsLoad load)
            {
                delivered.Add(Assert.Single(load.Tradeskills).TradeskillXp);
                if (!entered)
                {
                    entered = true; f.Actor.Player.AddTradeskillXp(TradeskillType.Armorer, 20);
                    using var context = f.Context(); f.Actor.Player.Save(context); context.SaveChanges(); f.Actor.Events.Update(.01);
                }
            }
            if (args[0] is ServerProfessionUpdate update) delivered.Add(update.Tradeskill.TradeskillXp);
            return null;
        });
        f.Actor.Player.SendTradeskillInitialPackets(); f.Pump(); Assert.Equal(new uint[] { 180, 200 }, delivered); Assert.Empty(f.Actor.Errors);
    }

    [Theory]
    [InlineData("identity")] [InlineData("session-player")] [InlineData("standalone-session")]
    public async Task ObsoleteOwnersCannotPublishOrFallBackToImmediateDelivery(string change)
    {
        using var f = await Create(); f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0); await f.Save();
        var identity = f.Actor.Player.Identity; var session = f.Actor.Player.Session;
        var standalone = RecordingDispatchProxy<IGameSession>.Create(out var other);
        try
        {
            if (change == "identity") f.Actor.Set(nameof(Player.Identity), null);
            else if (change == "session-player") f.Messages.SetProperty("Player", null);
            else f.Actor.Set(nameof(Player.Session), standalone);
            f.Pump(); f.Actor.Player.AddTradeskillXp(TradeskillType.Armorer, 1); f.Actor.Player.SendTradeskillInitialPackets();
            Assert.Empty(f.Packets<ServerProfessionUpdate>()); Assert.Empty(f.Packets<ServerProfessionsLoad>());
            Assert.Empty(other.GetInvocations("EnqueueMessageEncrypted")); Assert.Empty(f.Actor.Errors);
        }
        finally { f.Actor.Set(nameof(Player.Identity), identity); f.Actor.Set(nameof(Player.Session), session); f.Messages.SetProperty("Player", f.Actor.Player); }
    }

    [Fact]
    public async Task AnUnchangedNormalSaveRetriesPublicationAfterTheEventQueueReturns()
    {
        using var f = await Create(); EnableCurrency(f); f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0);
        f.Messages.SetProperty("Events", null); await f.Save(); f.Pump(); Assert.Empty(f.Packets<ServerProfessionUpdate>());
        f.Messages.SetProperty("Events", f.Actor.Events); await f.Actor.Player.SaveDirect(); await f.Actor.Drain(); f.Pump();
        Assert.Single(f.Packets<ServerProfessionUpdate>());
    }

    [Fact]
    public async Task QuietDiscoveryKeepsItsLegacyReturnAndNoticePolicyWhileInitialFlagsAdvance()
    {
        using var f = await Create(existing: true); Assert.False(f.Actor.Player.DiscoverSchematic(501));
        f.Actor.Player.SendTradeskillInitialPackets(); Assert.Empty(Assert.Single(f.Packets<ServerProfessionsLoad>()).DiscoveredSchematics);
        Assert.Empty(f.Packets<ServerSchematicAddLearned>()); await f.Save(); f.Pump(); f.Actor.Player.SendTradeskillInitialPackets();
        var initial = f.Packets<ServerProfessionsLoad>().Last(); Assert.Equal(501u, Assert.Single(initial.DiscoveredSchematics).TradeskillSchematic2Id);
        Assert.Equal(new uint[] { 1 }, initial.LearnedSchematicDiscoveredFlags); Assert.Empty(f.Packets<ServerSchematicAddLearned>());
    }

    private static void EnableCurrency(Fixture f)
    {
        var currencies = new CurrencyManager(f.Source.Source.Tables); currencies.Initialise(f.Actor.Player, new CharacterModel { Id = 42 }); f.Actor.Set(nameof(Player.CurrencyManager), currencies);
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken token = default)
            => Task.FromException(new IOException("Owned progression acknowledgement loss."));
    }
}
