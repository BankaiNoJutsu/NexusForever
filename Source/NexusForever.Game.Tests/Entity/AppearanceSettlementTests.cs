using System.Reflection;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Customisation;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity.Player;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SourceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed class AppearanceSettlementTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PublicChangeKeepsItsAppearanceAndWalletPrivateUntilCharacterConfirmation(bool tokens)
    {
        using var f = await Fixture.Create();
        f.Request(tokens);

        Assert.Equal(Sex.Male, f.Actor.Player.Sex);
        Assert.Equal(10u, Assert.Single(f.Appearance.GetCustomisations()).Value);
        Assert.Equal(.25f, Assert.Single(f.Appearance.GetBones()).BoneValue);
        Assert.Equal(100ul, f.Credits.Single().Amount);
        Assert.Equal(100ul, f.Tokens.GetCurrencyAmount(AccountCurrencyType.ServiceToken));
        Assert.Empty(f.Packets<ServerEntityBoneUpdate>());
        using var stored = f.Source.Context();
        Assert.Equal((byte)Sex.Male, stored.Character.Single().Sex);
        Assert.Equal(100ul, stored.CharacterCurrency.Single().Amount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PublicChangeSettlesTheFullAppearanceAndOnlyItsSelectedPayment(bool tokens)
    {
        using var f = await Fixture.Create();
        f.Request(tokens); await f.Actor.Drain();

        using var stored = f.Source.Context(); using var auth = f.Auth.Context();
        Assert.Equal((byte)Sex.Female, stored.Character.Single().Sex);
        Assert.Equal(20u, stored.CharacterCustomisation.Single().Value);
        Assert.Equal((ushort)22, stored.CharacterAppearance.Single().DisplayId);
        Assert.Equal(.75f, stored.CharacterBone.Single().Bone);
        Assert.Equal(tokens ? 100ul : 93ul, stored.CharacterCurrency.Single().Amount);
        Assert.Equal(tokens ? 93ul : 100ul, auth.AccountCurrency.Single().Amount);
        Assert.Equal(Sex.Female, f.Actor.Player.Sex);
        Assert.Single(f.Packets<ServerEntityBoneUpdate>());
        Assert.True(f.Session.CanProcessIncomingPackets);
        Assert.Empty(stored.CharacterEntitlement); Assert.Empty(auth.AccountEntitlement);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PrimaryCommitKeepsLiveAppearancePrivateUntilFollowupConfirmation(bool tokens)
    {
        var hold = new FollowupHold(); using var f = await Fixture.Create(hold);
        f.Request(tokens); await f.Drive(() => hold.Reached);
        Assert.Equal(Sex.Male, f.Actor.Player.Sex);
        Assert.Equal(10u, Assert.Single(f.Appearance.GetCustomisations()).Value);
        Assert.Empty(f.Packets<ServerEntityBoneUpdate>());
        hold.Release.TrySetResult(); await f.Actor.Drain();
        Assert.Equal(Sex.Female, f.Actor.Player.Sex); Assert.Single(f.Packets<ServerEntityBoneUpdate>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RejectedCompleteGraphRestoresPaymentBeforeRetry(bool tokens)
    {
        using var f = await Fixture.Create();
        using (var setup = f.Source.Context()) await setup.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_appearance BEFORE UPDATE ON character_customisation BEGIN SELECT RAISE(ABORT, 'owned appearance rejection'); END");
        f.Request(tokens); await f.Drive(() => f.Database.Rollbacks != 0);
        using (var stored = f.Source.Context()) using (var auth = f.Auth.Context())
        {
            Assert.Equal((byte)Sex.Male, stored.Character.Single().Sex);
            Assert.Equal(10u, stored.CharacterCustomisation.Single().Value);
            Assert.Equal(100ul, stored.CharacterCurrency.Single().Amount); Assert.Equal(100ul, auth.AccountCurrency.Single().Amount);
            Assert.Empty(f.Packets<ServerEntityBoneUpdate>());
            if (tokens) Assert.Contains(auth.AccountCurrencyOperation, row => !row.Subtract && row.OperationKey.StartsWith("appearance-change-refund:"));
            await stored.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_appearance");
        }
        await f.Actor.Drain();
        using var final = f.Source.Context(); using var payer = f.Auth.Context();
        Assert.Equal((byte)Sex.Female, final.Character.Single().Sex);
        Assert.Equal(tokens ? 100ul : 93ul, final.CharacterCurrency.Single().Amount);
        Assert.Equal(tokens ? 93ul : 100ul, payer.AccountCurrency.Single().Amount);
        Assert.Single(f.Packets<ServerEntityBoneUpdate>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LostCommitAcknowledgementDoesNotReplayThePayment(bool authFailure)
    {
        var lost = new LostAcknowledgement(authFailure); using var f = await Fixture.Create(authFailure ? null : lost);
        if (authFailure) f.Auth.Interceptor = lost;
        f.Request(true); await f.Actor.Drain();
        Assert.True(lost.Observed);
        using var auth = f.Auth.Context(); using var stored = f.Source.Context();
        Assert.Equal(93ul, auth.AccountCurrency.Single().Amount); Assert.Single(auth.AccountCurrencyOperation);
        Assert.Equal(20u, stored.CharacterCustomisation.Single().Value); Assert.Single(f.Packets<ServerEntityBoneUpdate>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AcceptedChangeFencesDirectLeafAndHeaderMutation(bool tokens)
    {
        using var f = await Fixture.Create(); f.Request(tokens);
        Assert.Throws<InvalidOperationException>(() => f.Actor.Player.Sex = Sex.Female);
        Assert.Throws<InvalidOperationException>(() => f.Actor.Player.Race = Race.Human);
        Assert.Throws<InvalidOperationException>(() => Assert.Single(f.Appearance.GetCustomisations()).Value = 88);
        Assert.Throws<InvalidOperationException>(() => Assert.Single(f.Appearance.GetAppearances()).DisplayId = 88);
        Assert.Throws<InvalidOperationException>(() => Assert.Single(f.Appearance.GetBones()).BoneValue = 88);
        Assert.Throws<InvalidOperationException>(() => Assert.Single(f.Appearance.GetBones()).Delete());
        Assert.Throws<InvalidOperationException>(() => f.Appearance.Update(Race.Human, Sex.Male, [(7, 99)], [.9f]));
        await f.Actor.Drain();
        using var stored = f.Source.Context(); Assert.Equal(20u, stored.CharacterCustomisation.Single().Value);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PacketRetryKeepsTheConfirmedPaymentAndFreshBonePayload(bool tokens)
    {
        using var f = await Fixture.Create(); int calls = 0; ServerEntityBoneUpdate rejected = null;
        f.Connection.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerEntityBoneUpdate packet && ++calls == 1)
            { rejected = packet; packet.Bones[0] = 99; throw new IOException("Owned notice rejection."); }
            return null;
        });
        f.Request(tokens); await f.Actor.Drain(); Assert.Equal(2, calls);
        var packets = f.Packets<ServerEntityBoneUpdate>().ToArray(); Assert.NotSame(rejected, packets.Last());
        Assert.Equal(.75f, Assert.Single(packets.Last().Bones));
        using var auth = f.Auth.Context(); using var stored = f.Source.Context();
        Assert.Equal(tokens ? 100ul : 93ul, stored.CharacterCurrency.Single().Amount);
        Assert.Equal(tokens ? 93ul : 100ul, auth.AccountCurrency.Single().Amount);
        Assert.Equal(2, f.Database.Attempts);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PricingCannotRebindTheRequestToAnotherPlayer(bool tokens)
    {
        using var f = await Fixture.Create(); var replacement = RecordingDispatchProxy<IPlayer>.Create(out _);
        var catalog = (RecordingDispatchProxy<ICustomisationManager>)(object)f.Customisations;
        catalog.SetMethodHandler(tokens ? nameof(ICustomisationManager.CalculateCostTokens) : nameof(ICustomisationManager.CalculateCostCredits), _ =>
        { f.Connection.SetProperty(nameof(IWorldSession.Player), replacement); return 7u; });
        f.Request(tokens); await f.Actor.Drain();
        Assert.Equal(0, f.Database.Attempts); Assert.Equal(Sex.Male, f.Actor.Player.Sex);
        using var stored = f.Source.Context(); using var auth = f.Auth.Context();
        Assert.Equal(100ul, stored.CharacterCurrency.Single().Amount); Assert.Equal(100ul, auth.AccountCurrency.Single().Amount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UnavailableFundsBeforeActorCaptureRejectTheQueuedChange(bool tokens)
    {
        using var f = await Fixture.Create(); f.Request(tokens);
        if (tokens) f.Tokens.CurrencySubtractAmount(AccountCurrencyType.ServiceToken, 100);
        else f.Credits.CurrencySubtractAmount(CurrencyType.Credits, 100);
        await f.Actor.Drain(); Assert.Equal(0, f.Database.Attempts);
        Assert.Equal(Sex.Male, f.Actor.Player.Sex); Assert.Empty(f.Packets<ServerEntityBoneUpdate>());
        // Rejected admission must release the appearance mutation fence.
        Assert.Single(f.Appearance.GetCustomisations()).Value = 11;
    }

    [Fact]
    public async Task NewAdmissionRecognizesThePaidAppearanceWithoutRefundOrRedelivery()
    {
        using var f = await Fixture.Create(); f.Request(true); await f.Actor.Drain();
        using (var update = f.Auth.Context()) await update.AccountWorldSession.ExecuteUpdateAsync(set => set.SetProperty(row => row.Epoch, 2ul));
        var admitted = new AccountModel { Id = 12, WorldSessionEpoch = 2 };
        await VendorAccountPurchase.RecoverAtAdmissionAsync(admitted, f.Auth.Database, f.Database, 1);
        Assert.Equal(93ul, Assert.Single(admitted.AccountCurrency).Amount);
        using var stored = f.Source.Context(); using var auth = f.Auth.Context();
        Assert.Equal(20u, stored.CharacterCustomisation.Single().Value); Assert.Single(auth.AccountCurrencyOperation);
        Assert.Single(f.Packets<ServerEntityBoneUpdate>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NextRequestReplacesTheFullGraphAndPaysOnlyItsOwnFee(bool tokens)
    {
        using var f = await Fixture.Create(); f.Request(tokens); await f.Actor.Drain();
        ((RecordingDispatchProxy<ICustomisationManager>)(object)f.Customisations).SetMethodReturn(nameof(ICustomisationManager.GetItemVisuals), Array.Empty<IItemVisual>());
        f.Request(tokens, empty: true); await f.Actor.Drain();
        using var stored = f.Source.Context(); using var auth = f.Auth.Context();
        Assert.Empty(stored.CharacterCustomisation); Assert.Empty(stored.CharacterBone); Assert.Empty(stored.CharacterAppearance);
        Assert.DoesNotContain(f.Actor.Player.GetVisuals(), row => row.Slot == ItemSlot.BodyHair);
        Assert.Equal(tokens ? 100ul : 86ul, stored.CharacterCurrency.Single().Amount);
        Assert.Equal(tokens ? 86ul : 100ul, auth.AccountCurrency.Single().Amount);
        Assert.Equal(2, f.Packets<ServerEntityBoneUpdate>().Count()); Assert.Empty(f.Appearance.GetBones());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FreeAppearanceCommitsWithoutAnAccountPaymentAnchor(bool tokens)
    {
        using var f = await Fixture.Create();
        var catalog = (RecordingDispatchProxy<ICustomisationManager>)(object)f.Customisations;
        catalog.SetMethodReturn(nameof(ICustomisationManager.CalculateCostCredits), 0u);
        catalog.SetMethodReturn(nameof(ICustomisationManager.CalculateCostTokens), 0u);
        f.Request(tokens); await f.Actor.Drain();
        using var auth = f.Auth.Context(); using var stored = f.Source.Context();
        Assert.Equal(100ul, auth.AccountCurrency.Single().Amount); Assert.Empty(auth.AccountCurrencyOperation);
        Assert.Equal(100ul, stored.CharacterCurrency.Single().Amount); Assert.Equal(20u, stored.CharacterCustomisation.Single().Value);
        Assert.Single(f.Packets<ServerEntityBoneUpdate>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ReboundSessionCannotReceiveTheOriginalConfirmedAppearance(bool tokens)
    {
        using var f = await Fixture.Create(); var replacement = RecordingDispatchProxy<IPlayer>.Create(out _);
        f.Database.Committed = primary => { if (primary) f.Connection.SetProperty(nameof(IWorldSession.Player), replacement); };
        f.Request(tokens); await f.Actor.Drain();
        using var stored = f.Source.Context(); using var auth = f.Auth.Context();
        Assert.Equal(20u, stored.CharacterCustomisation.Single().Value);
        Assert.Equal(tokens ? 100ul : 93ul, stored.CharacterCurrency.Single().Amount);
        Assert.Equal(tokens ? 93ul : 100ul, auth.AccountCurrency.Single().Amount);
        Assert.Empty(f.Packets<ServerEntityBoneUpdate>()); Assert.Same(replacement, f.Session.Player);
        Assert.False(f.Session.CanProcessIncomingPackets); Assert.Equal(2, f.Database.Attempts);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ProjectionFailureCannotReserveAPaymentOrPartiallyChangeTheActor(bool tokens)
    {
        using var f = await Fixture.Create();
        var catalog = (RecordingDispatchProxy<ICustomisationManager>)(object)f.Customisations;
        catalog.SetMethodHandler(nameof(ICustomisationManager.GetItemVisuals), _ => throw new IOException("Owned projection failure."));
        Assert.Throws<IOException>(() => f.Request(tokens)); await f.Actor.Drain();
        Assert.Equal(0, f.Database.Attempts); Assert.Equal(Sex.Male, f.Actor.Player.Sex);
        Assert.Equal(100ul, f.Credits.Single().Amount); Assert.Equal(100ul, f.Tokens.GetCurrencyAmount(AccountCurrencyType.ServiceToken));
        Assert.Single(f.Appearance.GetCustomisations()).Value = 11;
    }

    [Fact]
    public async Task RejectedAuthDebitCannotCommitTheAppearanceAndCanRetry()
    {
        using var f = await Fixture.Create();
        using (var auth = f.Auth.Context()) await auth.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_appearance_debit BEFORE INSERT ON account_currency_operation BEGIN SELECT RAISE(ABORT, 'owned debit rejection'); END");
        f.Request(true); await f.Drive(() => f.Database.Rollbacks != 0);
        using (var stored = f.Source.Context()) using (var auth = f.Auth.Context())
        {
            Assert.Equal(10u, stored.CharacterCustomisation.Single().Value); Assert.Empty(auth.AccountCurrencyOperation);
            Assert.Equal(100ul, auth.AccountCurrency.Single().Amount);
            await auth.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_appearance_debit");
        }
        await f.Actor.Drain(); using var final = f.Auth.Context(); Assert.Equal(93ul, final.AccountCurrency.Single().Amount);
    }

    [Fact]
    public async Task AdmissionCompletesAnInterruptedRefundFromTheOriginalDurableAnchor()
    {
        using var f = await Fixture.Create();
        using (var stored = f.Source.Context()) await stored.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_appearance BEFORE UPDATE ON character_customisation BEGIN SELECT RAISE(ABORT, 'owned graph rejection'); END");
        using (var auth = f.Auth.Context()) await auth.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_appearance_refund BEFORE INSERT ON account_currency_operation WHEN NEW.subtract = 0 BEGIN SELECT RAISE(ABORT, 'owned refund rejection'); END");
        f.Request(true); await f.Drive(() => { using var auth = f.Auth.Context(); return auth.AccountCurrencyOperation.Any(row => row.Subtract); });
        using (var auth = f.Auth.Context())
        {
            await auth.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_appearance_refund");
            await auth.AccountWorldSession.ExecuteUpdateAsync(set => set.SetProperty(row => row.Epoch, 2ul));
        }
        var admitted = new AccountModel { Id = 12, WorldSessionEpoch = 2 };
        await VendorAccountPurchase.RecoverAtAdmissionAsync(admitted, f.Auth.Database, f.Database, 1);
        Assert.Equal(100ul, Assert.Single(admitted.AccountCurrency).Amount);
        await f.Drive(() => f.Database.Rollbacks != 0);
        using var storedFinal = f.Source.Context(); using var paid = f.Auth.Context();
        Assert.Equal(10u, storedFinal.CharacterCustomisation.Single().Value); Assert.Equal(100ul, paid.AccountCurrency.Single().Amount);
        Assert.Equal(2, paid.AccountCurrencyOperation.Count()); Assert.Empty(f.Packets<ServerEntityBoneUpdate>());
        // The old queue stays fenced. Fresh admission owns the recovered wallet; it never adopts the old actor.
    }

    [Theory]
    [InlineData("epoch")] [InlineData("queue")] [InlineData("wallet")]
    public async Task QueuedRequestCannotAcquireReplacementOwnership(string replacement)
    {
        using var f = await Fixture.Create(); f.Request(true);
        if (replacement == "epoch") ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.WorldSessionEpoch), 2ul);
        else if (replacement == "queue") f.Connection.SetProperty(nameof(IWorldSession.Events), new EventQueue());
        else f.Actor.Set(nameof(Player.CurrencyManager), RecordingDispatchProxy<ICurrencyManager>.Create(out _));
        await f.Actor.Drain(); Assert.Equal(0, f.Database.Attempts);
        using var stored = f.Source.Context(); using var auth = f.Auth.Context();
        Assert.Equal(10u, stored.CharacterCustomisation.Single().Value); Assert.Equal(100ul, auth.AccountCurrency.Single().Amount);
        Assert.Empty(f.Packets<ServerEntityBoneUpdate>());
    }

    [Fact]
    public async Task SignedZeroSurvivesProviderNormalizationAndCompleteFollowup()
    {
        using var f = await Fixture.Create(); f.Request(false, bone: -0f); await f.Actor.Drain();
        using var stored = f.Source.Context(); Assert.Equal(0f, stored.CharacterBone.Single().Bone);
        Assert.Equal(93ul, stored.CharacterCurrency.Single().Amount); Assert.Single(f.Packets<ServerEntityBoneUpdate>());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ProjectionCannotRebindTheRequestToAnotherPlayer(bool tokens)
    {
        using var f = await Fixture.Create(); var replacement = RecordingDispatchProxy<IPlayer>.Create(out _);
        ((RecordingDispatchProxy<ICustomisationManager>)(object)f.Customisations).SetMethodHandler(nameof(ICustomisationManager.GetItemVisuals), _ =>
        {
            f.Connection.SetProperty(nameof(IWorldSession.Player), replacement);
            return new IItemVisual[] { new ItemVisual { Slot = ItemSlot.BodyHair, DisplayId = 22 } };
        });
        f.Request(tokens); await f.Actor.Drain(); Assert.Equal(0, f.Database.Attempts);
        using var auth = f.Auth.Context(); Assert.Equal(100ul, auth.AccountCurrency.Single().Amount);
        Assert.Single(f.Appearance.GetCustomisations()).Value = 11;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ChangedSqlPayerCannotReceiveOrPayForTheAcceptedAppearance(bool tokens)
    {
        using var f = await Fixture.Create(); f.Request(tokens);
        using (var stored = f.Source.Context()) await stored.Character.ExecuteUpdateAsync(set => set.SetProperty(row => row.AccountId, 13u));
        await f.Drive(() => f.Database.Attempts >= 2);
        using (var stored = f.Source.Context()) using (var auth = f.Auth.Context())
        {
            Assert.Equal(10u, stored.CharacterCustomisation.Single().Value); Assert.Equal(100ul, stored.CharacterCurrency.Single().Amount);
            Assert.Equal(100ul, auth.AccountCurrency.Single().Amount); Assert.Empty(auth.AccountCurrencyOperation);
            Assert.Empty(f.Packets<ServerEntityBoneUpdate>());
            await stored.Character.ExecuteUpdateAsync(set => set.SetProperty(row => row.AccountId, 12u));
        }
        await f.Actor.Drain(); using var final = f.Source.Context(); Assert.Equal(20u, final.CharacterCustomisation.Single().Value);
    }

    [Fact]
    public async Task FailedBoneNoticeCannotCrossAVisiblePeersReplacedQueue()
    {
        using var f = await Fixture.Create(); using var peer = await Fixture.Create(); peer.Actor.Set(nameof(Player.Guid), 88u);
        var visible = (Dictionary<uint, IGridEntity>)typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Actor.Player)!;
        visible.Add(88, peer.Actor.Player); int attempts = 0;
        f.Connection.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerEntityBoneUpdate && ++attempts == 1)
            { peer.Connection.SetProperty(nameof(IWorldSession.Events), new EventQueue()); throw new IOException("Owned visible notice rejection."); }
            return null;
        });
        f.Request(false); await f.Actor.Drain();
        Assert.Equal(2, attempts); Assert.Empty(peer.Packets<ServerEntityBoneUpdate>());
        using var stored = f.Source.Context(); Assert.Equal(93ul, stored.CharacterCurrency.Single().Amount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PricingCannotMoveTheSamePlayerToAReplacementSession(bool tokens)
    {
        using var f = await Fixture.Create(); var replacement = RecordingDispatchProxy<IWorldSession>.Create(out var swapped);
        swapped.SetProperty(nameof(IWorldSession.Events), f.Actor.Events); swapped.SetProperty(nameof(IWorldSession.Player), f.Actor.Player);
        ((RecordingDispatchProxy<ICustomisationManager>)(object)f.Customisations).SetMethodHandler(
            tokens ? nameof(ICustomisationManager.CalculateCostTokens) : nameof(ICustomisationManager.CalculateCostCredits), _ =>
            {
                f.Actor.Set(nameof(Player.Session), replacement);
                ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.Session), replacement);
                return 7u;
            });
        f.Request(tokens); await f.Actor.Drain();
        Assert.Equal(0, f.Database.Attempts); Assert.Equal(Sex.Male, f.Actor.Player.Sex);
        using var auth = f.Auth.Context(); using var stored = f.Source.Context();
        Assert.Equal(100ul, auth.AccountCurrency.Single().Amount); Assert.Equal(100ul, stored.CharacterCurrency.Single().Amount);
    }

    private sealed class AppearanceDatabase(SourceFixture source, IInterceptor interceptor) : CharacterDatabase
    {
        internal int Attempts, Rollbacks;
        internal Action<bool> Committed;
        public override async Task SaveAppearanceChangeReconciliation(ulong id, Action<CharacterContext> enroll, AppearanceChangeSaveReconciliation receipt)
        {
            Attempts++;
            try
            {
                await CharacterAtomicSave.ExecuteAppearanceChangeReconciliation(() => interceptor == null ? source.Context() : source.Context(interceptor), id, enroll, receipt);
                Committed?.Invoke(receipt.Primary);
            }
            catch (CharacterReconciliationRolledBackException) { Rollbacks++; throw; }
        }
    }
    private sealed class FollowupHold : DbTransactionInterceptor
    {
        internal bool Reached;
        internal readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData data,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (data.Context is CharacterContext context && context.BankReconciliationReceipt.Local.Any(row => row.Payload.Contains("normal-appearance-change") && row.Payload.Contains("\"Primary\":false")))
            { Reached = true; await Release.Task; }
            return result;
        }
    }
    private sealed class LostAcknowledgement(bool auth) : DbTransactionInterceptor
    {
        internal bool Observed;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
        {
            bool matched = auth ? data.Context is AuthContext payer && payer.AccountCurrencyOperation.Local.Any(row => row.OperationKey.StartsWith("appearance-change-debit:"))
                : data.Context is CharacterContext source && source.BankReconciliationReceipt.Local.Any(row => row.Payload.Contains("normal-appearance-change"));
            if (!matched || Observed) return Task.CompletedTask;
            Observed = true; return Task.FromException(new IOException("Owned appearance acknowledgement loss."));
        }
    }

    private sealed class Fixture(SourceFixture source, AuthStore auth, Actor actor, IWorldSession session,
        RecordingDispatchProxy<IWorldSession> connection, ICustomisationManager customisations,
        AppearanceManager appearance, CurrencyManager credits, AccountCurrencyManager tokens, AppearanceDatabase database) : IDisposable
    {
        internal readonly SourceFixture Source = source;
        internal readonly AppearanceDatabase Database = database;
        internal readonly RecordingDispatchProxy<IWorldSession> Connection = connection;
        internal readonly ICustomisationManager Customisations = customisations;
        internal readonly AuthStore Auth = auth;
        internal readonly Actor Actor = actor;
        internal readonly IWorldSession Session = session;
        internal readonly AppearanceManager Appearance = appearance;
        internal readonly CurrencyManager Credits = credits;
        internal readonly AccountCurrencyManager Tokens = tokens;
        internal IEnumerable<T> Packets<T>() => Connection.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(row => row.Arguments[0]).OfType<T>();

        internal static async Task<Fixture> Create(IInterceptor interceptor = null)
        {
            var source = new SourceFixture(null); source.Seed();
            using (var setup = source.Context())
            {
                var character = setup.Character.Single(); character.AccountId = 12; character.Race = (byte)Race.Human; character.Sex = (byte)Sex.Male;
                setup.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
                setup.CharacterCustomisation.Add(new() { Id = 42, Label = 7, Value = 10 });
                setup.CharacterAppearance.Add(new() { Id = 42, Slot = (byte)ItemSlot.BodyHair, DisplayId = 12 });
                setup.CharacterBone.Add(new() { Id = 42, BoneIndex = 0, Bone = .25f });
                await setup.SaveChangesAsync();
            }
            var table = (RecordingDispatchProxy<IGameTableManager>)(object)source.Source.Tables;
            table.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1 }));
            table.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = 9 }));
            var customisations = RecordingDispatchProxy<ICustomisationManager>.Create(out var custom);
            custom.SetMethodReturn(nameof(ICustomisationManager.Validate), true);
            custom.SetMethodReturn(nameof(ICustomisationManager.CalculateCostCredits), 7u);
            custom.SetMethodReturn(nameof(ICustomisationManager.CalculateCostTokens), 7u);
            custom.SetMethodReturn(nameof(ICustomisationManager.GetItemVisuals), new IItemVisual[] { new ItemVisual { Slot = ItemSlot.BodyHair, DisplayId = 22 } });
            var auth = new AuthStore();
            var events = new EventQueue(); var session = RecordingDispatchProxy<IWorldSession>.Create(out var connection);
            connection.SetProperty(nameof(IWorldSession.Events), events); connection.SetProperty(nameof(IWorldSession.CanProcessIncomingPackets), true);
            var account = (RecordingDispatchProxy<IAccount>)(object)source.Source.Player.Account;
            account.SetProperty(nameof(IAccount.Id), 12u); account.SetProperty(nameof(IAccount.WorldSessionEpoch), 1ul); account.SetProperty(nameof(IAccount.Session), session);
            var database = new AppearanceDatabase(source, interceptor);
            using (var context = source.Context()) database.Initialise(new DatabaseConnectionString { Provider = DatabaseProvider.Sqlite, ConnectionString = context.Database.GetDbConnection().ConnectionString });
            CharacterModel model = Assert.Single(await database.GetCharacters(12));
            var actor = new Actor(source, actorModel: model, actorSession: session, actorEvents: events, characterDatabase: database, authDatabase: auth.Database);
            actor.Set(nameof(Player.Race), Race.Human); actor.Set(nameof(Player.Sex), Sex.Male); actor.Set(nameof(Player.Guid), 77u);
            typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, new Dictionary<uint, IGridEntity>());
            typeof(WorldEntity).GetField("itemVisuals", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(actor.Player, new Dictionary<ItemSlot, IItemVisual>());
            connection.SetProperty(nameof(IWorldSession.Player), actor.Player); connection.SetProperty(nameof(IWorldSession.Account), actor.Player.Account);
            var credits = new CurrencyManager(source.Source.Tables); credits.Initialise(actor.Player, model); actor.Set(nameof(Player.CurrencyManager), credits);
            var tokens = new AccountCurrencyManager(actor.Player.Account, auth.Model, source.Source.Tables); account.SetProperty(nameof(IAccount.CurrencyManager), tokens);
            account.SetMethodHandler(nameof(IAccount.Save), args => { actor.OnAuth?.Invoke(); tokens.Save((AuthContext)args[0]); return null; });
            var appearance = new AppearanceManager(actor.Player, model, customisations); actor.Set(nameof(Player.AppearanceManager), appearance);
            await actor.Load(); connection.Invocations.Clear();
            return new(source, auth, actor, session, connection, customisations, appearance, credits, tokens, database);
        }
        internal void Request(bool tokens, bool empty = false, float bone = .75f)
        {
            using var output = new MemoryStream();
            using (var writer = new GamePacketWriter(output))
            {
                writer.Write((uint)Race.Human, 5); writer.Write((uint)Sex.Female, 2); writer.Write(empty ? 0u : 1u); if (!empty) { writer.Write(7u); writer.Write(20u); }
                writer.Write(empty ? 0u : 1u); if (!empty) writer.Write(bone); writer.Write(tokens); writer.FlushBits();
            }
            using var input = new MemoryStream(output.ToArray()); using var reader = new GamePacketReader(input);
            var request = new ClientCharacterAppearanceChange(); request.Read(reader);
            new ClientAppearanceChangeHandler(Customisations).HandleMessage(Session, request);
        }
        internal async Task Drive(Func<bool> ready)
        {
            var elapsed = Stopwatch.StartNew();
            while (!ready()) { Actor.Events.Update(.01); await Task.Delay(5); Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(20), "Appearance did not reach its boundary."); }
            Assert.Empty(Actor.Errors);
        }
        public void Dispose() { Actor.Dispose(); Source.Dispose(); Auth.Dispose(); }
    }

    private sealed class AppearanceAuthDatabase(Func<AuthContext> contexts) : AuthDatabase { public override AuthContext CreateContext() => contexts(); }
    private sealed class FaultAuthContext(DatabaseConnectionString config, IInterceptor interceptor) : AuthContext(config)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder builder) { base.OnConfiguring(builder); builder.AddInterceptors(interceptor); }
    }
    private sealed class AuthStore : IDisposable
    {
        private readonly string path = Path.Combine(Path.GetTempPath(), $"nf-appearance-{Guid.NewGuid():N}.sqlite");
        private DatabaseConnectionString Config => new() { Provider = DatabaseProvider.Sqlite, ConnectionString = $"Data Source={path};Pooling=False" };
        internal readonly AuthDatabase Database;
        internal IInterceptor Interceptor;
        internal readonly AccountModel Model;
        internal AuthContext Context() => Interceptor == null ? new AuthContext(Config) : new FaultAuthContext(Config, Interceptor);
        internal AuthStore()
        {
            Database = new AppearanceAuthDatabase(Context); Database.Initialise(Config);
            using var context = Context(); context.Database.EnsureCreated();
            Model = new() { Id = 12, Email = "appearance-12@example.local", WorldSessionEpoch = 1 };
            Model.AccountCurrency.Add(new() { Id = 12, CurrencyId = 9, Amount = 100 });
            context.Account.Add(Model); context.AccountWorldSession.Add(new() { AccountId = 12, Epoch = 1 }); context.SaveChanges();
        }
        public void Dispose() { if (File.Exists(path)) File.Delete(path); }
    }
}
