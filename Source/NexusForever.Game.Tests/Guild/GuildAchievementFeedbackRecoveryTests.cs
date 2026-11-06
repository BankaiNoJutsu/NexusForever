using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Achievement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Achievement;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildCreationFinanceTests
{
    [Theory]
    [InlineData("achievement")]
    [InlineData("delivery")]
    [InlineData("recipient-identity")]
    public async Task RejectedCharacterFeedbackSaveRetainsItsIntentAndRecoversOneContribution(string rejection)
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f);
        string sql = rejection switch
        {
            "achievement" => "CREATE TRIGGER reject_recipient BEFORE INSERT ON character_achievement BEGIN SELECT RAISE(ABORT, 'owned feedback recipient rejection'); END",
            "delivery" => "CREATE TRIGGER reject_recipient BEFORE INSERT ON bank_reconciliation_receipt WHEN NEW.id LIKE 'h%' BEGIN SELECT RAISE(ABORT, 'owned feedback recipient rejection'); END",
            _ => "CREATE TRIGGER reject_recipient AFTER INSERT ON bank_reconciliation_receipt WHEN NEW.id LIKE 'h%' BEGIN UPDATE character SET accountId = 99 WHERE id = 42; END"
        };
        using (var setup = f.Source.Context())
        {
            if (setup.Database.IsMySql()) sql = rejection switch
            {
                "achievement" => "CREATE TRIGGER reject_recipient BEFORE INSERT ON character_achievement FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'owned feedback recipient rejection'",
                "delivery" => "CREATE TRIGGER reject_recipient BEFORE INSERT ON bank_reconciliation_receipt FOR EACH ROW BEGIN IF NEW.id LIKE 'h%' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'owned feedback recipient rejection'; END IF; END",
                _ => "CREATE TRIGGER reject_recipient AFTER INSERT ON bank_reconciliation_receipt FOR EACH ROW BEGIN IF NEW.id LIKE 'h%' THEN UPDATE `character` SET accountId = 99 WHERE id = 42; END IF; END"
            };
            await setup.Database.ExecuteSqlRawAsync(sql);
        }
        await CommitFeedbackParent(f); await f.Actor.Drain(allowErrors: true);
        using (var failed = f.Source.Context())
        {
            Assert.Single(failed.GuildAchievement); Assert.Empty(failed.CharacterAchievement);
            Assert.Equal(12u, failed.Character.Single().AccountId);
            Assert.Single(failed.BankReconciliationReceipt.Where(row => row.Id.StartsWith("g")));
            Assert.Empty(failed.BankReconciliationReceipt.Where(row => row.Id.StartsWith("h")));
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_recipient");
        }
        Assert.Empty(FeedbackPackets(f)); f.Actor.Errors.Clear();
        f.Actor.StartSave(); await f.Actor.Drain();
        using (var quarantined = f.Source.Context()) Assert.Empty(quarantined.CharacterAchievement);
        using var fresh = await FreshFeedbackActor(f);
        Assert.IsType<CharacterAchievementManager>(fresh.Player.AchievementManager).PumpGuildFeedbackRecovery(0); await fresh.Drain();
        AssertFeedbackDelivered(f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParentRollbackOrLostAcknowledgementRetainsOneRecoverableFeedbackIntent(bool loseAcknowledgement)
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f);
        using (var attempt = loseAcknowledgement ? f.Source.Context(new FeedbackLostAcknowledgement()) : f.Source.Context())
        {
            if (loseAcknowledgement)
            {
                f.Actor.Player.GuildManager.Guild.Save(attempt);
                await Assert.ThrowsAsync<IOException>(() => attempt.SaveChangesAsync());
            }
            else
            {
                await WithFeedbackStrategy(attempt, async () =>
                {
                    using var transaction = await attempt.Database.BeginTransactionAsync();
                    f.Actor.Player.GuildManager.Guild.Save(attempt); await attempt.SaveChangesAsync(); await transaction.RollbackAsync();
                });
            }
        }
        await f.Actor.Drain(); Assert.Empty(FeedbackPackets(f));
        using (var pending = f.Source.Context())
        {
            Assert.Empty(pending.CharacterAchievement);
            Assert.Equal(loseAcknowledgement ? 1 : 0, pending.BankReconciliationReceipt.Count(row => row.Id.StartsWith("g")));
        }
        await CommitFeedbackParent(f); await f.Actor.Drain(); AssertFeedbackDelivered(f);
    }

    [Fact]
    public async Task RepeatedFeedbackAfterDeliveryDoesNotIncrementTheSameScalarAgain()
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f); await CommitFeedbackParent(f); await f.Actor.Drain();
        AssertFeedbackDelivered(f);
        var manager = Assert.IsType<CharacterAchievementManager>(f.Actor.Player.AchievementManager); var root = FeedbackRoot(f);
        manager.ApplyGuildFeedback(root); manager.ApplyGuildFeedback(root); f.Actor.StartSave(); await f.Actor.Drain();
        AssertFeedbackDelivered(f); Assert.Single(FeedbackPackets(f));
    }

    [Theory]
    [InlineData("session-player")]
    [InlineData("manager")]
    [InlineData("account-epoch")]
    [InlineData("events")]
    [InlineData("session")]
    public async Task StaleOriginalRecipientLeavesIntentForFreshOwnerWithoutRecheckingEligibility(string replacement)
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f);
        if (replacement == "session-player") f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        else if (replacement == "manager") f.Actor.Set(nameof(Player.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
        else if (replacement == "account-epoch") ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.WorldSessionEpoch), 8ul);
        else if (replacement == "events") f.Messages.SetProperty(nameof(IWorldSession.Events), new EventQueue());
        else f.Actor.Set(nameof(Player.Session), RecordingDispatchProxy<IWorldSession>.Create(out _));
        await CommitFeedbackParent(f); await f.Actor.Drain();
        using (var waiting = f.Source.Context()) Assert.Empty(waiting.CharacterAchievement);
        Assert.Empty(FeedbackPackets(f));
        using var fresh = await FreshFeedbackActor(f);
        var manager = Assert.IsType<CharacterAchievementManager>(fresh.Player.AchievementManager);
        manager.PumpGuildFeedbackRecovery(0); await fresh.Drain();
        using var delivered = f.Source.Context();
        Assert.Equal(1u, Assert.Single(delivered.CharacterAchievement).ProgressState);
        Assert.Single(delivered.BankReconciliationReceipt.Where(row => row.Id.StartsWith("h"))); Assert.Empty(FeedbackPackets(f));
    }

    [Fact]
    public async Task LaterCharacterProgressSurvivesAnEarlierFrozenFeedbackSave()
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f); bool later = false;
        f.Actor.OnAuth = () =>
        {
            if (later) return; later = true;
            f.Actor.Player.AchievementManager.CheckAchievements(f.Actor.Player, AchievementType.AchievementComplete, 600);
        };
        await CommitFeedbackParent(f); await f.Actor.Drain(); f.Actor.StartSave(); await f.Actor.Drain();
        using var stored = f.Source.Context(); var row = Assert.Single(stored.CharacterAchievement);
        Assert.Equal(2u, row.ProgressState); Assert.NotNull(row.DateCompleted);
        Assert.Single(stored.BankReconciliationReceipt.Where(receipt => receipt.Id.StartsWith("h")));
    }

    [Fact]
    public async Task FailedEligibilityPreparationCannotCommitAnUnaccompaniedGuildCompletion()
    {
        using var f = await CreateFeedbackFixture();
        ((RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements).SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements),
            _ => throw new IOException("owned feedback eligibility failure"));
        Assert.Throws<IOException>(() => f.Actor.Player.GuildManager.Guild.AchievementManager.CheckAchievements(f.Actor.Player, AchievementType.KillCreatureEntry, 500));
        using var attempt = f.Source.Context();
        Assert.Throws<InvalidOperationException>(() => f.Actor.Player.GuildManager.Guild.Save(attempt));
        Assert.True(attempt.IsSaveRejected);
        using var stored = f.Source.Context(); Assert.Empty(stored.GuildAchievement); Assert.Empty(stored.CharacterAchievement);
    }

    [Fact]
    public async Task ReusingClearedContextAfterRollbackReenrolsTheParentIntent()
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f);
        using (var context = f.Source.Context())
        {
            await WithFeedbackStrategy(context, async () =>
            {
                using (var first = await context.Database.BeginTransactionAsync())
                {
                    f.Actor.Player.GuildManager.Guild.Save(context); await context.SaveChangesAsync(); await first.RollbackAsync();
                }
                context.ChangeTracker.Clear();
                using var retry = await context.Database.BeginTransactionAsync();
                f.Actor.Player.GuildManager.Guild.Save(context); await context.SaveChangesAsync(); await retry.CommitAsync();
            });
        }
        await f.Actor.Drain(); AssertFeedbackDelivered(f);
    }

    [Fact]
    public async Task ReentrantDeliveryDuringFrozenSaveDoesNotRepeatItsContribution()
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f); await CommitFeedbackParent(f);
        var root = FeedbackRoot(f); var manager = Assert.IsType<CharacterAchievementManager>(f.Actor.Player.AchievementManager);
        f.Actor.OnAuth = () => { manager.ApplyGuildFeedback(root); manager.ApplyGuildFeedback(root); };
        await f.Actor.Drain(); AssertFeedbackDelivered(f); Assert.Single(FeedbackPackets(f));
    }

    [Fact]
    public async Task RejectedIntentReceiptRollsBackItsGuildParent()
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f);
        using (var setup = f.Source.Context())
            await setup.Database.ExecuteSqlRawAsync(setup.Database.IsMySql()
                ? "CREATE TRIGGER reject_feedback_intent BEFORE INSERT ON bank_reconciliation_receipt FOR EACH ROW BEGIN IF NEW.id LIKE 'g%' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'owned feedback intent rejection'; END IF; END"
                : "CREATE TRIGGER reject_feedback_intent BEFORE INSERT ON bank_reconciliation_receipt WHEN NEW.id LIKE 'g%' BEGIN SELECT RAISE(ABORT, 'owned feedback intent rejection'); END");
        await Assert.ThrowsAsync<DbUpdateException>(() => CommitFeedbackParent(f)); await f.Actor.Drain();
        using (var failed = f.Source.Context())
        {
            Assert.Empty(failed.GuildAchievement); Assert.Empty(failed.CharacterAchievement);
            Assert.Empty(failed.BankReconciliationReceipt.Where(row => row.Id.StartsWith("g")));
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_feedback_intent");
        }
        await CommitFeedbackParent(f); await f.Actor.Drain(); AssertFeedbackDelivered(f);
    }

    [Fact]
    public async Task ChangedAcceptedMetadataRemainsRecoverableUntilItsOriginalCatalogIsRestored()
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f); await CommitFeedbackParent(f);
        var info = f.Achievements.GetAchievement(601); info.Entry.RequiredProgress = 99;
        await f.Actor.Drain(allowErrors: true); Assert.NotEmpty(f.Actor.Errors);
        using (var blocked = f.Source.Context())
        {
            Assert.Empty(blocked.CharacterAchievement); Assert.Empty(blocked.BankReconciliationReceipt.Where(row => row.Id.StartsWith("h")));
        }
        info.Entry.RequiredProgress = 2; f.Actor.Errors.Clear();
        Assert.IsType<CharacterAchievementManager>(f.Actor.Player.AchievementManager).PumpGuildFeedbackRecovery(30);
        await f.Actor.Drain(); AssertFeedbackDelivered(f);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DescendantFeedbackEligibilityIsFrozenOnOriginalActor(bool acceptDescendant)
    {
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var originalGates);
        originalGates.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => (uint)args[1] != 2345
            || acceptDescendant && ((IPlayer)args[0]).AchievementManager.HasCompletedAchievement(601));
        using var f = await CreateFeedbackFixture(prerequisites);
        var child = f.Achievements.GetAchievement(601); child.Entry.RequiredProgress = 1;
        var descendant = new AchievementInfo(new AchievementEntry { Id = 602, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 601, RequiredProgress = 2, PrerequisiteIdServer = 2345 });
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements;
        var parent = f.Achievements.GetAchievement(600);
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] switch { 600 => parent, 601 => child, 602 => descendant, _ => null });
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { child, descendant } : []);
        EarnFeedback(f);
        f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        await CommitFeedbackParent(f); await f.Actor.Drain();
        using var fresh = await FreshFeedbackActor(f);
        Assert.IsType<CharacterAchievementManager>(fresh.Player.AchievementManager).PumpGuildFeedbackRecovery(0); await fresh.Drain();
        using var stored = f.Source.Context(); var direct = stored.CharacterAchievement.Single(row => row.AchievementId == 601);
        Assert.Equal(1u, direct.ProgressState); Assert.NotNull(direct.DateCompleted);
        Assert.Equal(acceptDescendant ? 1 : 0, stored.CharacterAchievement.Count(row => row.AchievementId == 602));
        if (acceptDescendant) Assert.Equal(1u, stored.CharacterAchievement.Single(row => row.AchievementId == 602).ProgressState);
    }

    [Fact]
    public async Task LostCharacterAcknowledgementIsRecoveredWithoutApplyingItsContributionAgain()
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f); await CommitFeedbackParent(f);
        var originalAuth = f.Actor.Player.GetAuthDatabase();
        var fault = new FeedbackLostAcknowledgement(); var database = new FeedbackDatabase(() => f.Source.Context(fault));
        using (var context = f.Source.Context()) database.Initialise(new DatabaseConnectionString { Provider = context.Database.IsMySql() ? DatabaseProvider.MySql : DatabaseProvider.Sqlite,
            ConnectionString = context.Database.GetDbConnection().ConnectionString });
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var routes);
        routes.SetMethodHandler(nameof(IDatabaseManager.GetDatabase), (method, _) => method.GetGenericArguments()[0] == typeof(AuthDatabase) ? originalAuth : database);
        typeof(Player).GetField("databaseManager", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(f.Actor.Player, databases);
        await f.Actor.Drain(allowErrors: true); AssertFeedbackDelivered(f); Assert.Empty(FeedbackPackets(f));
        using var fresh = await FreshFeedbackActor(f);
        var manager = Assert.IsType<CharacterAchievementManager>(fresh.Player.AchievementManager);
        manager.PumpGuildFeedbackRecovery(0); await fresh.Drain();
        fresh.StartSave();
        await fresh.Drain(); AssertFeedbackDelivered(f);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ConvergingFeedbackFreezesEligibilityForActualCompletedBranches(bool negateSibling, bool siblingRemainsPartial)
    {
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var gates);
        gates.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => (uint)args[1] != 3456
            || ((IPlayer)args[0]).AchievementManager.HasCompletedAchievement(602) != negateSibling);
        using var f = await CreateFeedbackFixture(prerequisites);
        var parent = f.Achievements.GetAchievement(600); var first = f.Achievements.GetAchievement(601); first.Entry.RequiredProgress = 1;
        var second = new AchievementInfo(new AchievementEntry { Id = 602, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 600, RequiredProgress = siblingRemainsPartial ? 2u : 1u });
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Tables;
        tables.SetProperty(nameof(IGameTableManager.AchievementChecklist), PlayerBindPointTests.Table(
            new AchievementChecklistEntry { Id = 1, AchievementId = 603, Bit = 0, ObjectId = 601 },
            new AchievementChecklistEntry { Id = 2, AchievementId = 603, Bit = 1, ObjectId = 602 }));
        var shared = new AchievementInfo(new AchievementEntry { Id = 603, AchievementTypeId = (uint)AchievementType.AchievementComplete }, f.Tables);
        var last = new AchievementInfo(new AchievementEntry { Id = 604, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 603, RequiredProgress = 2, PrerequisiteIdServer = 3456 });
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements;
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] switch
            { 600 => parent, 601 => first, 602 => second, 603 => shared, 604 => last, _ => null });
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { first, second, shared, last } : []);
        EarnFeedback(f); f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        await CommitFeedbackParent(f); await f.Actor.Drain();
        using var fresh = await FreshFeedbackActor(f);
        Assert.IsType<CharacterAchievementManager>(fresh.Player.AchievementManager).PumpGuildFeedbackRecovery(0); await fresh.Drain();
        using var stored = f.Source.Context();
        Assert.Equal(siblingRemainsPartial ? 1u : 3u, stored.CharacterAchievement.Single(row => row.AchievementId == 603).ProgressState);
        Assert.Equal(siblingRemainsPartial, stored.CharacterAchievement.Single(row => row.AchievementId == 602).DateCompleted == null);
        Assert.Equal(!siblingRemainsPartial && !negateSibling ? 1 : 0, stored.CharacterAchievement.Count(row => row.AchievementId == 604));
        Assert.Single(stored.BankReconciliationReceipt.Where(row => row.Id.StartsWith("h")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectFeedbackSiblingSeesEarlierCompletionInTheSameAcceptedEvent(bool negateEarlier)
    {
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var gates);
        gates.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => (uint)args[1] != 3456
            || ((IPlayer)args[0]).AchievementManager.HasCompletedAchievement(601) != negateEarlier);
        using var f = await CreateFeedbackFixture(prerequisites);
        var parent = f.Achievements.GetAchievement(600); var first = f.Achievements.GetAchievement(601); first.Entry.RequiredProgress = 1;
        var second = new AchievementInfo(new AchievementEntry { Id = 602, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 600, RequiredProgress = 2, PrerequisiteIdServer = 3456 });
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements;
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] switch { 600 => parent, 601 => first, 602 => second, _ => null });
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { first, second } : []);
        EarnFeedback(f); f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        await CommitFeedbackParent(f); await f.Actor.Drain();
        using var fresh = await FreshFeedbackActor(f);
        Assert.IsType<CharacterAchievementManager>(fresh.Player.AchievementManager).PumpGuildFeedbackRecovery(0); await fresh.Drain();
        using var stored = f.Source.Context();
        Assert.NotNull(stored.CharacterAchievement.Single(row => row.AchievementId == 601).DateCompleted);
        Assert.Equal(negateEarlier ? 0 : 1, stored.CharacterAchievement.Count(row => row.AchievementId == 602));
        if (!negateEarlier) Assert.Equal(1u, stored.CharacterAchievement.Single(row => row.AchievementId == 602).ProgressState);
    }

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 0)]
    [InlineData(true, true, 0)]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 1)]
    [InlineData(false, false, 2)]
    [InlineData(true, false, 2)]
    public async Task GuildMetaFeedbackSharesOneAtomicCharacterDelivery(bool negateEarlier, bool replaceAfterSnapshot, int blockMetadata)
    {
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var gates);
        gates.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => (uint)args[1] != 3456
            || ((IPlayer)args[0]).AchievementManager.HasCompletedAchievement(601) != negateEarlier);
        using var f = await CreateFeedbackFixture(prerequisites);
        var parent = f.Achievements.GetAchievement(600); var first = f.Achievements.GetAchievement(601); first.Entry.RequiredProgress = 1;
        var meta = new AchievementInfo(new AchievementEntry { Id = 604, Flags = (uint)AchievementFlags.Guild,
            AchievementTypeId = (uint)AchievementType.AchievementComplete, ObjectId = 600, RequiredProgress = 1 });
        var second = new AchievementInfo(new AchievementEntry { Id = 602, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 604, RequiredProgress = 2, PrerequisiteIdServer = 3456 });
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements;
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] switch
            { 600 => parent, 601 => first, 602 => second, 604 => meta, _ => null });
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { first, second } : []);
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetGuildAchievements), args => (AchievementType)args[0] switch
            { AchievementType.KillCreatureEntry => new[] { parent }, AchievementType.AchievementComplete => new IAchievementInfo[] { meta }, _ => [] });
        EarnFeedback(f);
        if (replaceAfterSnapshot)
        {
            f.Actor.OnAuth = () => f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        }
        else if (blockMetadata == 0) f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        await CommitFeedbackParent(f);
        if (blockMetadata == 1) first.Entry.RequiredProgress = 9;
        if (blockMetadata == 2) second.Entry.RequiredProgress = 9;
        await f.Actor.Drain(allowErrors: replaceAfterSnapshot || blockMetadata != 0);
        using (var beforeReload = f.Source.Context())
        {
            Assert.Empty(beforeReload.BankReconciliationReceipt.Where(row => row.Id.StartsWith("h")));
            Assert.Empty(beforeReload.CharacterAchievement);
        }
        if (blockMetadata != 0)
        {
            Assert.NotEmpty(f.Actor.Errors);
            first.Entry.RequiredProgress = 1; second.Entry.RequiredProgress = 2;
            f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        }
        using var fresh = await FreshFeedbackActor(f);
        Assert.IsType<CharacterAchievementManager>(fresh.Player.AchievementManager).PumpGuildFeedbackRecovery(0); await fresh.Drain();
        using var stored = f.Source.Context();
        Assert.NotNull(stored.CharacterAchievement.Single(row => row.AchievementId == 601).DateCompleted);
        Assert.Equal(negateEarlier ? 0 : 1, stored.CharacterAchievement.Count(row => row.AchievementId == 602));
        if (!negateEarlier) Assert.Equal(1u, stored.CharacterAchievement.Single(row => row.AchievementId == 602).ProgressState);
        Assert.Single(stored.BankReconciliationReceipt.Where(row => row.Id.StartsWith("h")));
    }

    [Fact]
    public async Task FailedGuildMetaForwardingRetriesTheRemainingChainWithoutCompletingItsParentAgain()
    {
        using var f = await CreateFeedbackFixture(); bool fail = true;
        var parent = f.Achievements.GetAchievement(600); parent.Entry.AchievementPointEnum = 3;
        var first = f.Achievements.GetAchievement(601);
        var meta = new AchievementInfo(new AchievementEntry { Id = 604, Flags = (uint)AchievementFlags.Guild,
            AchievementTypeId = (uint)AchievementType.AchievementComplete, ObjectId = 600, RequiredProgress = 1 });
        var second = new AchievementInfo(new AchievementEntry { Id = 602, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 604, RequiredProgress = 2 });
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements;
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] switch
            { 600 => parent, 601 => first, 602 => second, 604 => meta, _ => null });
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { first, second } : []);
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetGuildAchievements), args =>
        {
            if ((AchievementType)args[0] == AchievementType.KillCreatureEntry) return new[] { parent };
            if ((AchievementType)args[0] != AchievementType.AchievementComplete) return Array.Empty<IAchievementInfo>();
            if (fail) { fail = false; throw new IOException("owned forwarding failure before nested completion"); }
            return new IAchievementInfo[] { meta };
        });
        Assert.Throws<IOException>(() => EarnFeedback(f));
        var manager = Assert.IsType<GuildAchievementManager>(f.Actor.Player.GuildManager.Guild.AchievementManager);
        DateTime? originalCompletion = Assert.Single(manager.Capture()).Completed;
        using (var rejected = f.Source.Context()) Assert.Throws<InvalidOperationException>(() => f.Actor.Player.GuildManager.Guild.Save(rejected));
        EarnFeedback(f); await CommitFeedbackParent(f); await f.Actor.Drain();
        using var stored = f.Source.Context(); Assert.Equal(2, stored.GuildAchievement.Count(row => row.DateCompleted != null));
        DateTime? expectedCompletion = stored.Database.IsMySql() && originalCompletion is DateTime instant
            ? stored.Database.SqlQueryRaw<DateTime>("SELECT CAST({0} AS DATETIME) AS Value", instant).AsEnumerable().Single() : originalCompletion;
        Assert.Equal(expectedCompletion, stored.GuildAchievement.Single(row => row.AchievementId == 600).DateCompleted);
        Assert.Equal(50u, manager.AchievementPoints);
        Assert.Equal(2, stored.CharacterAchievement.Count(row => row.ProgressState == 1));
        Assert.Single(stored.BankReconciliationReceipt.Where(row => row.Id.StartsWith("g")));
        Assert.Single(stored.BankReconciliationReceipt.Where(row => row.Id.StartsWith("h")));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task NestedPreparationRetryKeepsTheOriginalGuildSiblingOrder(bool negateEarlier, bool partialSibling)
    {
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var gates);
        gates.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => (uint)args[1] != 3456
            || ((IPlayer)args[0]).AchievementManager.HasCompletedAchievement(601) != negateEarlier);
        using var f = await CreateFeedbackFixture(prerequisites);
        var parent = f.Achievements.GetAchievement(600);
        var firstGuild = new AchievementInfo(new AchievementEntry { Id = 604, Flags = (uint)AchievementFlags.Guild,
            AchievementTypeId = (uint)AchievementType.AchievementComplete, ObjectId = 600, RequiredProgress = partialSibling ? 2u : 1u, AchievementPointEnum = 1 });
        var nextGuild = new AchievementInfo(new AchievementEntry { Id = 605, Flags = (uint)AchievementFlags.Guild,
            AchievementTypeId = (uint)AchievementType.AchievementComplete, ObjectId = 600, RequiredProgress = 1 });
        var first = new AchievementInfo(new AchievementEntry { Id = 601, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 604, RequiredProgress = 1 });
        var next = new AchievementInfo(new AchievementEntry { Id = 602, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 605, RequiredProgress = 2, PrerequisiteIdServer = 3456 });
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements; int preparations = 0;
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] switch
            { 600 => parent, 601 => first, 602 => next, 604 => firstGuild, 605 => nextGuild, _ => null });
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
        {
            if ((AchievementType)args[0] != AchievementType.AchievementComplete) return Array.Empty<IAchievementInfo>();
            if (++preparations == 2) throw new IOException("owned nested preparation failure before the second Guild sibling");
            return new IAchievementInfo[] { first, next };
        });
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetGuildAchievements), args => (AchievementType)args[0] switch
            { AchievementType.KillCreatureEntry => new[] { parent }, AchievementType.AchievementComplete => new IAchievementInfo[] { firstGuild, nextGuild }, _ => [] });
        Assert.Throws<IOException>(() => EarnFeedback(f));
        using (var rejected = f.Source.Context()) Assert.Throws<InvalidOperationException>(() => f.Actor.Player.GuildManager.Guild.Save(rejected));
        EarnFeedback(f); await CommitFeedbackParent(f); await f.Actor.Drain();
        using var stored = f.Source.Context(); Assert.Equal(partialSibling ? 2 : 3, stored.GuildAchievement.Count(row => row.DateCompleted != null));
        Assert.Equal(1u, stored.GuildAchievement.Single(row => row.AchievementId == 604).ProgressState);
        Assert.Equal(partialSibling ? 0u : 10u, f.Actor.Player.GuildManager.Guild.AchievementManager.AchievementPoints);
        if (!partialSibling) Assert.NotNull(stored.CharacterAchievement.Single(row => row.AchievementId == 601).DateCompleted);
        else Assert.Empty(stored.CharacterAchievement.Where(row => row.AchievementId == 601));
        Assert.Equal(partialSibling == negateEarlier ? 1 : 0, stored.CharacterAchievement.Count(row => row.AchievementId == 602));
        Assert.Single(stored.BankReconciliationReceipt.Where(row => row.Id.StartsWith("h")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OriginalCleanupKeepsItsAcceptedFeedbackAndLaterEditsWhileReplacementCannotTakeItsSave(bool replaceRegistry)
    {
        IPlayer registered = null;
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), _ => registered);
        using var f = await CreateFeedbackFixture(players: players); registered = f.Actor.Player;
        f.Currency.CurrencyAddAmount(NexusForever.Game.Static.Entity.CurrencyType.Credits, 25);
        f.Actor.OnAuth = () =>
        {
            registered = replaceRegistry ? RecordingDispatchProxy<IPlayer>.Create(out _) : null;
            ((RecordingDispatchProxy<ILogoutManager>)(object)f.Actor.Player.LogoutManager).SetProperty(nameof(ILogoutManager.State), LogoutState.Logout);
        };
        EarnFeedback(f); await CommitFeedbackParent(f); await f.Actor.Drain(allowErrors: replaceRegistry);
        using var stored = f.Source.Context();
        Assert.Equal(replaceRegistry ? 0 : 1, stored.CharacterAchievement.Count(row => row.AchievementId == 601));
        Assert.Equal(replaceRegistry ? 4900ul : 4925ul, stored.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
        Assert.Equal(replaceRegistry ? 0 : 1, stored.BankReconciliationReceipt.Count(row => row.Id.StartsWith("h")));
        Assert.Empty(FeedbackPackets(f));
    }

    private sealed class FeedbackDatabase(Func<CharacterContext> contexts) : CharacterDatabase
    {
        public override CharacterContext CreateContext() => contexts();
    }
    private static async Task WithFeedbackStrategy(CharacterContext context, Func<Task> action)
    {
        bool entered = false;
        await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            if (entered) throw new InvalidOperationException("Feedback qualification cannot replay gameplay callbacks.");
            entered = true; await action();
        });
    }

    [Fact]
    public async Task GuildCompletionFeedbackKeepsItsGuildMetaInTheSameParentTransaction()
    {
        using var f = await CreateFeedbackFixture();
        var parent = f.Achievements.GetAchievement(600); var child = f.Achievements.GetAchievement(601);
        var meta = new AchievementInfo(new AchievementEntry { Id = 604, Flags = (uint)AchievementFlags.Guild,
            AchievementTypeId = (uint)AchievementType.AchievementComplete, ObjectId = 600, RequiredProgress = 1 });
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements;
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] switch
            { 600 => parent, 601 => child, 604 => meta, _ => null });
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetGuildAchievements), args => (AchievementType)args[0] switch
            { AchievementType.KillCreatureEntry => new[] { parent }, AchievementType.AchievementComplete => new IAchievementInfo[] { meta }, _ => [] });
        EarnFeedback(f);
        Assert.True(f.Actor.Player.GuildManager.Guild.AchievementManager.HasCompletedAchievement(604));
        using (var setup = f.Source.Context())
            await setup.Database.ExecuteSqlRawAsync(setup.Database.IsMySql()
                ? "CREATE TRIGGER reject_feedback_meta BEFORE INSERT ON guild_achievement FOR EACH ROW BEGIN IF NEW.achievementId = 604 THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'owned feedback Guild meta rejection'; END IF; END"
                : "CREATE TRIGGER reject_feedback_meta BEFORE INSERT ON guild_achievement WHEN NEW.achievementId = 604 BEGIN SELECT RAISE(ABORT, 'owned feedback Guild meta rejection'); END");
        await Assert.ThrowsAsync<DbUpdateException>(() => CommitFeedbackParent(f)); await f.Actor.Drain();
        using (var failed = f.Source.Context())
        {
            Assert.Empty(failed.GuildAchievement); Assert.Empty(failed.CharacterAchievement);
            Assert.Empty(failed.BankReconciliationReceipt.Where(row => row.Id.StartsWith("g")));
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_feedback_meta");
        }
        await CommitFeedbackParent(f); await f.Actor.Drain();
        using var final = f.Source.Context(); Assert.Equal(2, final.GuildAchievement.Count(row => row.DateCompleted != null));
        Assert.Equal(1u, Assert.Single(final.CharacterAchievement).ProgressState);
        Assert.Single(final.BankReconciliationReceipt.Where(row => row.Id.StartsWith("g")));
        Assert.Single(final.BankReconciliationReceipt.Where(row => row.Id.StartsWith("h")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeepFeedbackRecipesRetainEveryCausalAncestor(bool negateAncestor)
    {
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var gates);
        gates.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args => (uint)args[1] switch
        {
            2345 => ((IPlayer)args[0]).AchievementManager.HasCompletedAchievement(601),
            3456 => ((IPlayer)args[0]).AchievementManager.HasCompletedAchievement(601) != negateAncestor
                && ((IPlayer)args[0]).AchievementManager.HasCompletedAchievement(602),
            _ => true
        });
        using var f = await CreateFeedbackFixture(prerequisites);
        var parent = f.Achievements.GetAchievement(600); var child = f.Achievements.GetAchievement(601); child.Entry.RequiredProgress = 1;
        var middle = new AchievementInfo(new AchievementEntry { Id = 602, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 601, RequiredProgress = 1, PrerequisiteIdServer = 2345 });
        var last = new AchievementInfo(new AchievementEntry { Id = 603, AchievementTypeId = (uint)AchievementType.AchievementComplete,
            ObjectId = 602, RequiredProgress = 2, PrerequisiteIdServer = 3456 });
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements;
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => (ushort)args[0] switch
            { 600 => parent, 601 => child, 602 => middle, 603 => last, _ => null });
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { child, middle, last } : []);
        EarnFeedback(f); f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        await CommitFeedbackParent(f); await f.Actor.Drain();
        using var fresh = await FreshFeedbackActor(f);
        Assert.IsType<CharacterAchievementManager>(fresh.Player.AchievementManager).PumpGuildFeedbackRecovery(0); await fresh.Drain();
        using var stored = f.Source.Context();
        Assert.NotNull(stored.CharacterAchievement.Single(row => row.AchievementId == 601).DateCompleted);
        Assert.NotNull(stored.CharacterAchievement.Single(row => row.AchievementId == 602).DateCompleted);
        Assert.Equal(negateAncestor ? 0 : 1, stored.CharacterAchievement.Count(row => row.AchievementId == 603));
        if (!negateAncestor) Assert.Equal(1u, stored.CharacterAchievement.Single(row => row.AchievementId == 603).ProgressState);
    }

    [Fact]
    public async Task RecipientReplacementAfterSnapshotCannotCommitTheStaleDelivery()
    {
        using var f = await CreateFeedbackFixture(); EarnFeedback(f);
        f.Actor.OnAuth = () => f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        await CommitFeedbackParent(f); await f.Actor.Drain(allowErrors: true);
        using (var blocked = f.Source.Context())
        {
            Assert.Empty(blocked.CharacterAchievement); Assert.Empty(blocked.BankReconciliationReceipt.Where(row => row.Id.StartsWith("h")));
        }
        using var fresh = await FreshFeedbackActor(f);
        Assert.IsType<CharacterAchievementManager>(fresh.Player.AchievementManager).PumpGuildFeedbackRecovery(0); await fresh.Drain();
        AssertFeedbackDelivered(f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OriginalActorCanRetryAnUnfinishedEligibilityPreparation(bool useSetter)
    {
        using var f = await CreateFeedbackFixture(); var child = f.Achievements.GetAchievement(601); bool fail = true;
        ((RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements).SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] != AchievementType.AchievementComplete ? Array.Empty<IAchievementInfo>()
                : fail ? throw new IOException("owned transient eligibility failure") : new IAchievementInfo[] { child });
        void Earn()
        {
            if (useSetter) f.Actor.Player.GuildManager.Guild.AchievementManager.SetAchievementProgress(f.Actor.Player, AchievementType.KillCreatureEntry, 500, 0, 1);
            else EarnFeedback(f);
        }
        Assert.Throws<IOException>(Earn);
        using (var rejected = f.Source.Context()) Assert.Throws<InvalidOperationException>(() => f.Actor.Player.GuildManager.Guild.Save(rejected));
        fail = false; Earn(); await CommitFeedbackParent(f); await f.Actor.Drain(); AssertFeedbackDelivered(f);
    }

    private sealed class FeedbackLostAcknowledgement : DbTransactionInterceptor
    {
        private bool lost;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (lost) return Task.CompletedTask; lost = true;
            return Task.FromException(new IOException("owned feedback acknowledgement loss"));
        }
    }
    private async Task<Fixture> CreateFeedbackFixture(IPrerequisiteManager prerequisites = null, IPlayerManager players = null)
    {
        if (prerequisites == null)
        {
            prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var prepared); prepared.SetMethodReturn(nameof(IPrerequisiteManager.Meets), true);
        }
        var f = await Create(achievementPrerequisites: prerequisites, achievementPlayers: players); f.Register(); await f.Flush();
        ConfigureFeedback(f, new AchievementEntry { Id = 600, Flags = (uint)AchievementFlags.Guild,
            AchievementTypeId = (uint)AchievementType.KillCreatureEntry, ObjectId = 500, RequiredProgress = 1 },
            new AchievementEntry { Id = 601, AchievementTypeId = (uint)AchievementType.AchievementComplete,
                ObjectId = 600, RequiredProgress = 2, PrerequisiteIdServer = 1234 }, []);
        return f;
    }
    private static void EarnFeedback(Fixture f) => f.Actor.Player.AchievementManager.CheckAchievements(f.Actor.Player, AchievementType.KillCreatureEntry, 500);
    private static async Task CommitFeedbackParent(Fixture f)
    {
        using var context = f.Source.Context(); f.Actor.Player.GuildManager.Guild.Save(context); await context.SaveChangesAsync();
    }
    private static GuildAchievementFeedbackReceipt.Stored FeedbackRoot(Fixture f)
    {
        using var context = f.Source.Context(); var row = Assert.Single(context.BankReconciliationReceipt.Where(row => row.Id.StartsWith("g")));
        return new(row.Id, row.Payload, row.CreatedUtc);
    }
    private static IEnumerable<ServerAchievementUpdate> FeedbackPackets(Fixture f)
        => f.Packets<ServerAchievementUpdate>().Where(packet => packet.Achievements.Any(row => row.AchievementId == 601));
    private static void AssertFeedbackDelivered(Fixture f)
    {
        using var stored = f.Source.Context(); var row = Assert.Single(stored.CharacterAchievement);
        Assert.Equal((ushort)601, row.AchievementId); Assert.Equal(1u, row.ProgressState); Assert.Null(row.DateCompleted);
        Assert.Single(stored.GuildAchievement);
        Assert.Single(stored.BankReconciliationReceipt.Where(receipt => receipt.Id.StartsWith("g")));
        Assert.Single(stored.BankReconciliationReceipt.Where(receipt => receipt.Id.StartsWith("h")));
    }
    private static async Task<Actor> FreshFeedbackActor(Fixture f)
    {
        CharacterModel model = Assert.Single(await f.Actor.Player.GetCharacterDatabase().GetCharacters(12));
        var events = new EventQueue(); var session = RecordingDispatchProxy<IWorldSession>.Create(out var connection);
        connection.SetProperty(nameof(IWorldSession.Events), events); connection.SetProperty(nameof(IWorldSession.CanProcessIncomingPackets), true);
        var actor = new Actor(f.Source, actorModel: model, actorSession: session, actorEvents: events, actorAccount: f.Actor.Player.Account,
            characterDatabase: f.Actor.Player.GetCharacterDatabase(), inventoryFactory: (owner, _) => NexusForever.Game.Tests.Entity.BankDeferredCurrencyRecoveryTests.Inventory(owner,
                (IItemManager)(object)f.Source.Source.ManagerProxy, f.Tables, null));
        connection.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var denied);
        denied.SetMethodHandler(nameof(IPrerequisiteManager.Meets), _ => throw new InvalidOperationException("Recovery must not reevaluate accepted eligibility."));
        actor.Set(nameof(Player.AchievementManager), new CharacterAchievementManager(actor.Player, model,
            globalAchievementManager: f.Achievements, gameTableManager: f.Tables, prerequisiteManager: prerequisites));
        await actor.Load(); return actor;
    }
    private static void ConfigureFeedback(Fixture f, AchievementEntry guildEntry, AchievementEntry characterEntry, AchievementChecklistEntry[] checklist)
    {
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Tables).SetProperty(nameof(IGameTableManager.AchievementChecklist), PlayerBindPointTests.Table(checklist));
        var guildInfo = new AchievementInfo(guildEntry, f.Tables); var characterInfo = new AchievementInfo(characterEntry, f.Tables);
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements;
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args =>
            (ushort)args[0] == guildEntry.Id ? guildInfo : (ushort)args[0] == characterEntry.Id ? characterInfo : null);
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetGuildAchievements), args =>
            (uint)(AchievementType)args[0] == guildEntry.AchievementTypeId ? new IAchievementInfo[] { guildInfo } : []);
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { characterInfo } : []);
    }
}
