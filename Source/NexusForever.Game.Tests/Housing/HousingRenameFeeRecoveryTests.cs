using System.Reflection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Entity;
using NexusForever.Game.Guild;
using NexusForever.Game.Housing;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.Challenges;
using NexusForever.Game.Tests.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network;
using NexusForever.Network.Internal;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;
using OwnerFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Housing;

public sealed class HousingRenameFeeRecoveryTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public HousingRenameFeeRecoveryTests() { }
    internal HousingRenameFeeRecoveryTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicRenameChargesOnlyItsFormulaSelectedCurrencyAndCommitsBothNames(bool renown)
    {
        using var f = await Create(); f.Handler.HandleMessage(f.Session, Request("Accepted", renown)); await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", renown ? 5000ul : 4900ul, renown ? 122ul : 200ul);
        Assert.Equal(HousingResult.Success, Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>()).Result);
        Assert.Same(f.Community, f.Guilds.GetGuild(GuildType.Community, "Accepted"));
        Assert.Null(f.Guilds.GetGuild(GuildType.Community, "Original"));
    }

    [Fact]
    public async Task SuccessfulRenameReplyCannotPrecedeTheCommittedFeeAndBothNames()
    {
        using var f = await Create(); bool premature = false;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerHousingCommunityRenameResult { Result: HousingResult.Success })
            {
                using var probe = f.Source.Context();
                premature |= probe.Guild.Single(row => row.Id == 77).Name != "Accepted"
                    || probe.Residence.Single(row => row.Id == 100).Name != "Accepted"
                    || probe.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount != 4900;
            }
            return null;
        });
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        Assert.False(premature, "Successful rename reply preceded its fee/guild/residence transaction.");
        Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EitherFailedSideKeepsTheWholeFeeAndNameGraphUncommitted(bool failCurrency)
    {
        using var f = await Create();
        using (var setup = f.Source.Context())
            await RejectUpdate(setup, "reject_rename_fee", failCurrency ? "character_currency" : "guild");
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        using (var failed = f.Source.Context())
        {
            AssertGraph(failed, "Original", 5000, 200);
            Assert.DoesNotContain(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_rename_fee");
        }
        await Flush(f); using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4900, 200);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
    }

    [Fact]
    public async Task RepeatedRequestCannotDebitASecondFeeWhileTheFirstRenameIsPending()
    {
        using var f = await Create(); f.Handler.HandleMessage(f.Session, Request("First"));
        _ = Record.Exception(() => f.Handler.HandleMessage(f.Session, Request("Second")));
        await Flush(f); using var verify = f.Source.Context(); AssertGraph(verify, "First", 4900, 200);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
    }

    [Fact]
    public async Task AThrowingWalletPacketCannotStrandAPaidButUnacceptedName()
    {
        using var f = await Create(); int packets = 0;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerCombatReward && ++packets == 1) throw new IOException("owned rename wallet enqueue failure");
            return null;
        });
        Exception admission = Record.Exception(() => f.Handler.HandleMessage(f.Session, Request("Accepted")));
        Assert.Null(admission); await Flush(f); await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4900, 200);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
    }

    [Fact]
    public async Task NameCollisionRejectsBeforeAnyFeeOrSuccessfulReply()
    {
        using var f = await Create(); f.Handler.HandleMessage(f.Session, Request("Taken")); await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Original", 5000, 200);
        Assert.Equal(HousingResult.Failed, Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>()).Result);
    }

    [Fact]
    public async Task GuildRenameBroadcastCannotPrecedeTheCommittedFeeAndNames()
    {
        using var f = await Create(); bool premature = false; int broadcasts = 0;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not ServerGuildRename) return null;
            broadcasts++; using var probe = f.Source.Context();
            premature |= probe.Guild.Single(row => row.Id == 77).Name != "Accepted"
                || probe.Residence.Single(row => row.Id == 100).Name != "Accepted"
                || probe.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount != 4900;
            return null;
        });
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        Assert.False(premature, "Guild broadcast preceded its fee/name transaction."); Assert.True(broadcasts > 0);
    }

    [Fact]
    public async Task MapRenamePublicationCannotPrecedeTheCommittedFeeAndNames()
    {
        using var f = await Create(); bool premature = false; int notices = 0;
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var loaded);
        f.Community.Residence.Map = map;
        object Observe(object[] args)
        {
            notices++; using var probe = f.Source.Context();
            premature |= probe.Guild.Single(row => row.Id == 77).Name != "Accepted"
                || probe.Residence.Single(row => row.Id == 100).Name != "Accepted"
                || probe.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount != 4900;
            return null;
        }
        loaded.SetMethodHandler(nameof(IResidenceMapInstance.RenameResidence), Observe);
        loaded.SetMethodHandler(nameof(IResidenceMapInstance.EnqueueToAll), Observe);
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        Assert.False(premature, "Map rename notice preceded its fee/name transaction."); Assert.True(notices > 0);
    }

    [Fact]
    public async Task AuthFailureRetainsTheOriginalFeeAndNamesForRetry()
    {
        using var f = await Create(); bool reject = true; int attempts = 0;
        f.Actor.OnAuth = () => { attempts++; if (reject) throw new IOException("owned rename Auth failure"); };
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        using (var failed = f.Source.Context()) AssertGraph(failed, "Original", 5000, 200);
        Assert.True(attempts > 0); Assert.DoesNotContain(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
        reject = false; await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4900, 200);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
    }

    [Fact]
    public async Task EarningDuringAuthIsSavedAfterTheFeeBeforeItsPrivatePublication()
    {
        using var f = await Create(); bool earned = false;
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var loaded); f.Community.Residence.Map = map;
        f.Actor.OnAuth = () =>
        {
            if (earned) return; earned = true;
            f.Currency.CurrencyAddAmount(CurrencyType.Credits, 25);
            f.Community.Residence.Music = 29;
        };
        bool premature = false;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerCombatReward)
            { using var context = f.Source.Context(); premature |= context.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount != 4925; }
            return null;
        });
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4925, 200); Assert.False(premature);
        Assert.Equal((ushort)29, verify.Residence.Single().MusicId);
        Assert.Equal((ushort)29, Assert.Single(Assert.IsType<ServerHousingProperties>(Assert.Single(loaded.GetInvocations(nameof(IResidenceMapInstance.EnqueueToAll))).Arguments[0]).Residences).Music);
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task LostCommitAcknowledgementUsesItsReceiptWithoutChargingAgain()
    {
        var lost = new LostAcknowledgement(); using var f = await Create(lost);
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f); await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4900, 200);
        Assert.Equal(2, verify.BankReconciliationReceipt.Count());
        Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
        Assert.True(lost.Observed);
    }

    [Fact]
    public async Task ReplacedSessionPlayerCannotReceiveTheCommittedRename()
    {
        using var f = await Create(); f.Actor.OnAuth = () => f.Messages.SetProperty(nameof(IWorldSession.Player), null);
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4900, 200);
        Assert.Empty(Messages(f).OfType<ServerHousingCommunityRenameResult>());
        Assert.Empty(Messages(f).OfType<ServerCombatReward>());
        Assert.Empty(Messages(f).OfType<ServerGuildRename>());
        Assert.False(f.Session.CanProcessIncomingPackets);
    }

    [Theory]
    [InlineData("guild")]
    [InlineData("map")]
    [InlineData("result")]
    public async Task PublicationRetryResumesAtItsFailedStageWithoutAnotherFee(string stage)
    {
        using var f = await Create(); int failedStageCalls = 0;
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out var loaded); f.Community.Residence.Map = map;
        object Observe(object[] args)
        {
            bool selected = stage == "map" && args[0] is ServerHousingProperties
                || stage == "guild" && args[0] is ServerGuildRename
                || stage == "result" && args[0] is ServerHousingCommunityRenameResult { Result: HousingResult.Success };
            if (selected && ++failedStageCalls == 1)
            {
                if (args[0] is ServerGuildRename rename) rename.Name = "mutated";
                if (args[0] is ServerHousingProperties properties) properties.Residences.Clear();
                throw new IOException("owned rename publication failure");
            }
            if (selected && args[0] is ServerGuildRename retried) Assert.Equal("Accepted", retried.Name);
            if (selected && args[0] is ServerHousingProperties retry) Assert.Single(retry.Residences);
            return null;
        }
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), Observe); loaded.SetMethodHandler(nameof(IResidenceMapInstance.EnqueueToAll), Observe);
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f); await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4900, 200);
        Assert.Equal(2, failedStageCalls); Assert.Single(Messages(f).OfType<ServerCombatReward>());
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task OnlyTheExactRegisteredNameReservationCanEnterPaidRename()
    {
        using var f = await Create(); using var reservation = f.Guilds.TryReserveGuildName(f.Community, "Accepted");
        Assert.NotNull(reservation);
        Assert.False(f.Community.QueuePaidRename(f.Actor.Player, "Accepted", CurrencyType.Credits, 100, new MemoryStream()));
        using var verify = f.Source.Context(); AssertGraph(verify, "Original", 5000, 200);
    }

    [Fact]
    public async Task FailedQuestWriteRollsBackTheWholeFeeAndNameGraph()
    {
        using var f = await Create(); Assert.IsType<QuestObjective>(Assert.Single(f.Actor.Quest)).Progress = 2;
        using (var setup = f.Source.Context())
            await RejectUpdate(setup, "reject_rename_quest", "character_quest_objective");
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        using (var failed = f.Source.Context())
        {
            AssertGraph(failed, "Original", 5000, 200); Assert.Equal(1u, failed.CharacterQuestObjective.Single().Progress);
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_rename_quest");
        }
        await Flush(f); using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4900, 200);
        Assert.Equal(2u, verify.CharacterQuestObjective.Single().Progress);
        Assert.False(f.Actor.Quests.RequiresAuthoritativeReload); Assert.False(f.Actor.Challenges.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task ReentrantWorldUpdateCannotReplayTheUnacknowledgedPublication()
    {
        using var f = await Create(); int wallet = 0;
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerCombatReward)
            {
                wallet++; f.Guilds.Update(0);
                Assert.Null(Record.Exception(() => f.Handler.HandleMessage(f.Session, Request("Second"))));
            }
            return null;
        });
        f.Handler.HandleMessage(f.Session, Request("First")); await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "First", 4900, 200); Assert.Equal(1, wallet);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
    }

    [Fact]
    public async Task AcceptedFeeCompletesBeforeCommunityRetirementWithoutAStaleSuccess()
    {
        using var f = await Create(); bool disbanded = false;
        f.Actor.OnAuth = () => { if (!disbanded) { disbanded = true; f.Community.DisbandGuild(); } };
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        using (var paid = f.Source.Context()) AssertGraph(paid, "Accepted", 4900, 200);
        Assert.DoesNotContain(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
        using (var retirement = f.Source.Context()) { f.Community.Save(retirement); await retirement.SaveChangesAsync(); }
        using var verify = f.Source.Context(); Assert.NotNull(verify.Guild.Single(row => row.Id == 77).DeleteTime);
        Assert.Null(verify.Residence.Single().GuildOwnerId); Assert.Equal(4900ul, verify.CharacterCurrency.Single(row => row.CurrencyId == 1).Amount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ForgedReceiptCallbacksCannotAuthorizeAForeignSqlParent(bool foreignRoot)
    {
        using var f = await Create();
        using (var changed = f.Source.Context())
        {
            if (foreignRoot) changed.Residence.Single().GuildOwnerId = 88;
            else changed.Guild.Single(row => row.Id == 77).LeaderId = 43;
            await changed.SaveChangesAsync();
        }
        var receipt = new HousingRenameSaveReconciliation(Guid.NewGuid().ToString("N"), 42, 77, 100,
            (byte)GuildType.Community, "Forged", _ => { }, _ => { }, _ => true);
        await Assert.ThrowsAsync<HousingRenameConflictException>(() => CharacterAtomicSave.ExecuteHousingRenameReconciliation(() => f.Source.Context(), 42, context =>
        {
            var shell = new GuildModel { Id = 77, Type = (byte)GuildType.Community, LeaderId = 42, Name = "Original" };
            context.Attach(shell); shell.Name = "Forged";
        }, receipt));
        using var verify = f.Source.Context(); AssertGraph(verify, "Original", 5000, 200); Assert.Empty(verify.BankReconciliationReceipt);
    }

    [Fact]
    public async Task ForeignResidenceSqlCannotBeAdoptedAsThePaidGraph()
    {
        using var f = await Create();
        using (var setup = f.Source.Context()) await RejectUpdate(setup, "reject_rename_fee", "character_currency");
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        using (var foreign = f.Source.Context())
        {
            await foreign.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_rename_fee");
            await foreign.Database.ExecuteSqlRawAsync("UPDATE residence SET musicId=29 WHERE id=100");
        }
        await Flush(f); using var verify = f.Source.Context(); AssertGraph(verify, "Original", 5000, 200);
        Assert.Equal((ushort)29, verify.Residence.Single().MusicId);
        Assert.DoesNotContain(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
        using var escape = f.Source.Context(); Assert.Throws<InvalidOperationException>(() => f.Community.Residence.Save(escape));
    }

    [Fact]
    public async Task ZeroFeeRenamePreservesAnAbsentSelectedWallet()
    {
        using var f = await Create(zeroFee: true);
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        using var verify = f.Source.Context(); Assert.Equal("Accepted", verify.Guild.Single(row => row.Id == 77).Name);
        Assert.Equal("Accepted", verify.Residence.Single().Name); Assert.DoesNotContain(verify.CharacterCurrency, row => row.CurrencyId == 1);
        Assert.Equal(200ul, verify.CharacterCurrency.Single().Amount);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
        Assert.Empty(Messages(f).OfType<ServerCombatReward>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EarningAfterTheReadyCallbackIsReconciledBeforePublication(bool otherWallet)
    {
        using var f = await Create(); f.Handler.HandleMessage(f.Session, Request("Accepted"));
        // Stop between the completed second actor save and the next world
        // publication tick, using the committed receipts as the barrier.
        bool ready = false;
        for (int tick = 0; tick < 8 && !ready; tick++)
        {
            f.Guilds.Update(61); await Wait(f, f.Guilds, "cacheLock", "guildSavesInFlight"); await f.Actor.Drain(allowErrors: true);
            using var probe = f.Source.Context(); ready = probe.BankReconciliationReceipt.Count() == 2;
        }
        Assert.True(ready); Assert.Empty(Messages(f).OfType<ServerHousingCommunityRenameResult>());
        f.Currency.CurrencyAddAmount(otherWallet ? CurrencyType.Renown : CurrencyType.Credits, 25);
        await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", otherWallet ? 4900ul : 4925ul, otherWallet ? 225ul : 200ul);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
        Assert.True(f.Session.CanProcessIncomingPackets);
    }

    [Fact]
    public async Task ReplacedCommunityRootStillAdoptsItsOwnCommittedName()
    {
        using var f = await Create(); IResidence root = f.Community.Residence;
        f.Actor.OnAuth = () => f.Community.Residence = null;
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f); await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4900, 200);
        Assert.Equal("Accepted", root.Name); Assert.False(Assert.IsType<Residence>(root).NeedsSave);
        Assert.Empty(Messages(f).OfType<ServerHousingCommunityRenameResult>());
    }

    [Fact]
    public async Task MidEnrollmentResidenceReadFailureRetainsTheAlreadyStagedGuild()
    {
        var fault = new MidEnrollmentReadFailure(); using var f = await Create(fault);
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4900, 200); Assert.True(fault.Observed);
        Assert.Single(Messages(f).OfType<ServerHousingCommunityRenameResult>(), row => row.Result == HousingResult.Success);
        Assert.False(f.Actor.Quests.RequiresAuthoritativeReload); Assert.False(f.Actor.Challenges.RequiresAuthoritativeReload);
    }

    [Fact]
    public async Task ReceiptCannotChangeItsSqlLeaderAfterInitialAdmission()
    {
        using var f = await Create();
        var receipt = new HousingRenameSaveReconciliation(Guid.NewGuid().ToString("N"), 42, 77, 100,
            (byte)GuildType.Community, "Forged", context => context.Database.ExecuteSqlRaw("UPDATE guild SET leaderId=43 WHERE id=77"), _ => { }, _ => true);
        var rollback = await Assert.ThrowsAsync<CharacterReconciliationRolledBackException>(() => CharacterAtomicSave.ExecuteHousingRenameReconciliation(() => f.Source.Context(), 42, context =>
        {
            var shell = new GuildModel { Id = 77, Type = (byte)GuildType.Community, LeaderId = 42, Name = "Original" };
            context.Attach(shell); shell.Name = "Forged";
        }, receipt));
        Assert.IsType<HousingRenameConflictException>(rollback.InnerException);
        using var verify = f.Source.Context(); AssertGraph(verify, "Original", 5000, 200);
        Assert.Equal(42ul, verify.Guild.Single(row => row.Id == 77).LeaderId); Assert.Empty(verify.BankReconciliationReceipt);
    }

    [Fact]
    public async Task WalletPublicationRejectsReentrantInventoryAndSatchelMutations()
    {
        using var f = await Create(); int observed = 0;
        using var read = f.Source.Context();
        var satchel = new SupplySatchelManager(f.Actor.Player, read.Character.Single(row => row.Id == 42), f.Source.Source.Tables);
        f.Actor.Set(nameof(Player.SupplySatchelManager), satchel);
        f.Messages.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerCombatReward)
            {
                observed++;
                Assert.Throws<InvalidOperationException>(() => f.Actor.Player.Inventory.ItemCreate(InventoryLocation.Inventory, 999, 1));
                Assert.Throws<InvalidOperationException>(() => satchel.AddAmount(null, 1));
            }
            return null;
        });
        f.Handler.HandleMessage(f.Session, Request("Accepted")); await Flush(f);
        using var verify = f.Source.Context(); AssertGraph(verify, "Accepted", 4900, 200); Assert.Equal(1, observed);
        Assert.Equal(1u, satchel.AddAmount(null, 1));
    }

    private static void AssertGraph(CharacterContext context, string name, ulong credits, ulong renown)
    {
        Assert.Equal(name, context.Guild.Single(row => row.Id == 77).Name);
        Assert.Equal(name, context.Residence.Single(row => row.Id == 100).Name);
        Assert.Equal(credits, context.CharacterCurrency.Single(row => row.Id == 42 && row.CurrencyId == 1).Amount);
        Assert.Equal(renown, context.CharacterCurrency.Single(row => row.Id == 42 && row.CurrencyId == 2).Amount);
    }
    private static Task RejectUpdate(CharacterContext context, string trigger, string table)
        => context.Database.ExecuteSqlRawAsync(context.Database.IsSqlite()
            ? $"CREATE TRIGGER {trigger} BEFORE UPDATE ON {table} BEGIN SELECT RAISE(ABORT, 'owned rename write failure'); END"
            : $"CREATE TRIGGER {trigger} BEFORE UPDATE ON {table} FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'owned rename write failure'");
    private static object[] Messages(Fixture f) => f.Messages.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(row => row.Arguments[0]).ToArray();
    internal static ClientHousingCommunityRename Request(string name, bool renown = false)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            new NexusForever.Network.World.Message.Model.Shared.Identity { Id = 999, RealmId = 1 }.Write(writer);
            writer.WriteStringWide(name); writer.Write(renown); writer.FlushBits();
        }
        using var input = new MemoryStream(stream.ToArray()); using var reader = new GamePacketReader(input);
        var request = new ClientHousingCommunityRename(); request.Read(reader); return request;
    }
    internal static async Task Flush(Fixture f)
    {
        // Exercise the actual independent owners as well as both periodic
        // managers. A retained settlement must reject escaping one-sided saves.
        try { using var context = f.Source.Context(); f.Currency.Save(context); await context.SaveChangesAsync(); }
        catch (Exception error) when (error is InvalidOperationException or DbUpdateException) { }
        f.Guilds.Update(61); await Wait(f, f.Guilds, "cacheLock", "guildSavesInFlight");
        f.Residences.Update(61); await Wait(f, f.Residences, "saveGate", "savesInFlight");
        await f.Actor.Drain(allowErrors: true);
        for (int tick = 0; tick < 4; tick++)
        {
            f.Guilds.Update(0); await Wait(f, f.Guilds, "cacheLock", "guildSavesInFlight");
            f.Residences.Update(0); await Wait(f, f.Residences, "saveGate", "savesInFlight");
            await f.Actor.Drain(allowErrors: true);
        }
    }
    internal static async Task Wait(Fixture f, object manager, string gateName, string pendingName)
    {
        object gate = manager.GetType().GetField(gateName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        var pending = (HashSet<ulong>)manager.GetType().GetField(pendingName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(manager);
        for (int index = 0; index < 500; index++)
        {
            f.Actor.Events.Update(.01); // Network/session work precedes guild updates in the real world loop.
            lock (gate) if (pending.Count == 0) return;
            await Task.Delay(10);
        }
        Assert.Fail("The rename owner did not settle.");
    }
    internal async Task<Fixture> Create(IInterceptor interceptor = null, bool zeroFee = false)
    {
        var source = new OwnerFixture(options); source.Seed();
        var tables = source.Source.Tables; var table = (RecordingDispatchProxy<IGameTableManager>)(object)tables;
        table.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000000 }, new CurrencyTypeEntry { Id = 2, CapAmount = 1000000 }));
        table.SetProperty(nameof(IGameTableManager.GameFormula), PlayerBindPointTests.Table(new GameFormulaEntry { Id = 237, Datafloat0 = 5 }, new GameFormulaEntry { Id = 2395, Dataint0 = zeroFee ? 0u : 100u, Dataint01 = 78 }));
        table.SetProperty(nameof(IGameTableManager.HousingPlotInfo), PlayerBindPointTests.Table(new HousingPlotInfoEntry { Id = 3, HousingPropertyInfoId = (uint)PropertyInfoId.Community, HousingPropertyPlotIndex = 0 }));
        table.SetProperty(nameof(IGameTableManager.HousingPlugItem), PlayerBindPointTests.Table(new HousingPlugItemEntry { Id = 531 }, new HousingPlugItemEntry { Id = 573 }));
        table.SetProperty(nameof(IGameTableManager.HousingWallpaperInfo), PlayerBindPointTests.Table(new HousingWallpaperInfoEntry { Id = 29, Flags = 0x100 }));
        CharacterDatabase database = interceptor == null ? new CharacterDatabase() : new InterceptedDatabase(interceptor);
        using (var seed = source.Context())
        {
            string configuredConnection = seed.Database.GetDbConnection().ConnectionString;
            var character = seed.Character.Single(); character.AccountId = 12; character.Name = "Leader";
            seed.Character.Add(new() { Id = 43, AccountId = 13, Name = "Other", OriginalName = "Other" });
            if (!zeroFee) seed.CharacterCurrency.Add(new CharacterCurrencyModel { Id = 42, CurrencyId = 1, Amount = 5000 });
            seed.CharacterCurrency.Add(new CharacterCurrencyModel { Id = 42, CurrencyId = 2, Amount = 200 });
            seed.Guild.AddRange(Guild(77, 42, "Original"), Guild(88, 43, "Taken"));
            seed.Residence.Add(new() { Id = 100, GuildOwnerId = 77, Name = "Original", PropertyInfoId = (byte)PropertyInfoId.Community,
                Plot = [new ResidencePlotModel { Id = 100, Index = 0, PlotInfoId = 3 }] });
            await seed.SaveChangesAsync();
            database.Initialise(new DatabaseConnectionString { Provider = seed.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql, ConnectionString = configuredConnection });
        }
        var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var db); db.SetMethodReturn(nameof(IDatabaseManager.GetDatabase), database);
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy); realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var messages); messages.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var text); text.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        var characters = RecordingDispatchProxy<ICharacterManager>.Create(out var characterProxy);
        ICharacter leader = RecordingDispatchProxy<ICharacter>.Create(out var leaderProxy); leaderProxy.SetProperty(nameof(ICharacter.Name), "Leader");
        characterProxy.SetMethodReturn(nameof(ICharacterManager.GetCharacter), leader);
        Player originalPlayer = null;
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var playerProxy);
        playerProxy.SetMethodReturnFactory(nameof(IPlayerManager.GetPlayer), () => originalPlayer);
        GlobalGuildManager guilds = null;
        var guildProxy = RecordingDispatchProxy<IGlobalGuildManager>.Create(out var forward); forward.SetMethodHandler(nameof(IGlobalGuildManager.GetGuild), args => guilds.GetGuild((ulong)args[0]));
        var residences = new GlobalResidenceManager(characterManager: characters, globalGuildManager: guildProxy,
            realmContext: realm, databaseManager: databases, gameTableManager: tables);
        var factory = RecordingDispatchProxy<IGuildFactory>.Create(out var creation);
        creation.SetMethodHandler(nameof(IGuildFactory.CreateGuild), args =>
        {
            var community = new Community(realm, publisher, residences, filter, RecordingDispatchProxy<IMapLockManager>.Create(out _),
                characterManager: characters, globalGuildManager: guilds, playerManager: players);
            if (args[0] is GuildModel loaded) community.Initialise(loaded);
            else community.Initialise((string)args[1], (string)args[2], (string)args[3], (string)args[4]);
            return community;
        });
        guilds = new GlobalGuildManager(factory, databases); guilds.Initialise(); residences.Initialise();
        var community = Assert.IsType<Community>(guilds.GetGuild(77));
        var account = (RecordingDispatchProxy<IAccount>)(object)source.Source.Player.Account; account.SetProperty(nameof(IAccount.Id), 12u);
        var events = new EventQueue(); IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var connection);
        connection.SetProperty(nameof(IWorldSession.Events), events); connection.SetProperty(nameof(IWorldSession.CanProcessIncomingPackets), true); account.SetProperty(nameof(IAccount.Session), session);
        CharacterModel model = Assert.Single(await database.GetCharacters(12));
        var actor = new RewardNormalSaveSnapshotTests.Actor(source, actorModel: model, actorSession: session, actorEvents: events, characterDatabase: database,
            inventoryFactory: (owner, _) => BankDeferredCurrencyRecoveryTests.Inventory(owner, (IItemManager)(object)source.Source.ManagerProxy, tables, new NexusForever.Game.RealmBank.RealmBankManager(databases)));
        connection.SetProperty(nameof(IWorldSession.Player), actor.Player);
        var currency = new CurrencyManager(tables); currency.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), currency);
        var membership = RecordingDispatchProxy<IGuildManager>.Create(out var member); member.SetMethodReturn(nameof(IGuildManager.GetGuild), community); actor.Set(nameof(Player.GuildManager), membership);
        actor.Set(nameof(Player.Map), RecordingDispatchProxy<IResidenceMapInstance>.Create(out _));
        await actor.Load();
        originalPlayer = actor.Player; community.OnPlayerLogin(actor.Player);
        return new(source, actor, currency, guilds, residences, community, session, connection, new(filter, tables, realm, guilds));
    }
    private static GuildModel Guild(ulong id, ulong leader, string name) => new() { Id = id, Type = (byte)GuildType.Community, Name = name, LeaderId = leader,
        GuildRank = [new GuildRankModel { Id = id, Index = 0, Name = "Leader", Permission = (uint)GuildRankPermission.Leader }],
        GuildMember = [new GuildMemberModel { Id = id, CharacterId = leader, Rank = 0 }] };
    private sealed class InterceptedDatabase(IInterceptor interceptor) : CharacterDatabase
    {
        public override Task SaveHousingRenameReconciliation(ulong characterId, Action<CharacterContext> action, HousingRenameSaveReconciliation receipt)
        {
            using var context = CreateContext();
            var config = new DatabaseConnectionString { Provider = context.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql,
                ConnectionString = context.Database.GetDbConnection().ConnectionString };
            return CharacterAtomicSave.ExecuteHousingRenameReconciliation(() => new InterceptedContext(config, interceptor), characterId, action, receipt);
        }
    }
    private sealed class InterceptedContext(DatabaseConnectionString config, IInterceptor interceptor) : CharacterContext(config)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder) { base.OnConfiguring(builder); builder.AddInterceptors(interceptor); }
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        internal bool Observed;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        { Observed = true; return Task.FromException(new IOException("owned rename acknowledgement loss")); }
    }
    private sealed class MidEnrollmentReadFailure : DbCommandInterceptor
    {
        internal bool Observed;
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result)
        {
            if (!Observed && (command.CommandText.Contains("FROM \"residence\"", StringComparison.OrdinalIgnoreCase)
                || command.CommandText.Contains("FROM `residence`", StringComparison.OrdinalIgnoreCase))
                && data.Context!.ChangeTracker.Entries<GuildModel>().Any(row => row.Property(guild => guild.Name).IsModified))
            { Observed = true; throw new IOException("owned mid-enrollment residence read failure"); }
            return result;
        }
    }
    internal sealed record Fixture(OwnerFixture Source, RewardNormalSaveSnapshotTests.Actor Actor, CurrencyManager Currency,
        GlobalGuildManager Guilds, GlobalResidenceManager Residences, Community Community, IWorldSession Session,
        RecordingDispatchProxy<IWorldSession> Messages,
        ClientHousingCommunityRenameHandler Handler) : IDisposable
    {
        public void Dispose() { Actor.Dispose(); Source.Dispose(); }
    }
}
