using System.Reflection;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Costume;
using NexusForever.Game.Abstract.Account.Option;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Account.Unlock;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reward;
using RuntimeSpell = NexusForever.Game.Spell.Spell;

namespace NexusForever.Game.Tests.Account.Reward;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public sealed class ChallengeRewardPropertyTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"NexusChallengeProperty_{Guid.NewGuid():N}");
    private readonly DatabaseConnectionString connection;
    private readonly IGameTableManager tables;

    public ChallengeRewardPropertyTests()
    {
        Directory.CreateDirectory(directory);
        connection = new() { Provider = NexusForever.Database.DatabaseProvider.Sqlite,
            ConnectionString = $"Data Source={Path.Combine(directory, "auth.sqlite")};Pooling=False" };
        tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Tracks()));
        proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Rewards()));
        proxy.SetProperty(nameof(IGameTableManager.AccountItem), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.AccountItems()));
        proxy.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = 8 }));
        proxy.SetProperty(nameof(IGameTableManager.RewardProperty), PlayerBindPointTests.Table(
            new RewardPropertyEntry { Id = 1, RewardModifierValueTypeEnum = 0 },
            new RewardPropertyEntry { Id = 4, RewardModifierValueTypeEnum = 0 }));
        // Exact prepared Entitlement.tbl rows. 44 has no default grant.
        proxy.SetProperty(nameof(IGameTableManager.Entitlement), PlayerBindPointTests.Table(
            new EntitlementEntry { Id = 15, MaxCount = 1, Flags = 3 },
            new EntitlementEntry { Id = 17, MaxCount = 1, Flags = 3 },
            new EntitlementEntry { Id = 44, MaxCount = 1, Flags = 0, LocalizedTextIdName = 721802, LocalizedTextIdDescription = 721801 }));
        using var context = Context(); context.Database.EnsureCreated();
        var model = new AccountModel { Id = 1, Email = "challenge-property@example.test", S = "", V = "", GameToken = "", SessionKey = "" };
        AccountDefaultEntitlements.EnsureBaseline(model);
        context.Account.Add(model); context.SaveChanges();
    }

    [Theory]
    [InlineData(false, false, 1f)]
    [InlineData(true, false, 1.25f)]
    [InlineData(false, true, 1.25f)]
    [InlineData(true, true, 1.5f)]
    public void NativeConsumerReceivesWholeMultiplierAndExactKnownOwners(bool signature, bool earned, float expected)
    {
        if (earned) SeedEarned();
        var c = ClientFor(signature);
        c.Properties.SendInitialPackets();
        var row = ReadChallenge(c.Packets.Last());
        Assert.Equal(expected, row.Value);
        Assert.Equal(signature ? 0.25f : (float?)null, c.Properties.GetRewardProperty(RewardPropertyType.RewardTrackType).GetValue(3));
        Assert.Equal((signature ? 1 : 0) + (earned ? 1 : 0), row.OwnerMultipliers.Count);
        if (signature) Assert.Contains(row.OwnerMultipliers, x => x.OwnerType == RewardModifierOwner.RewardPropertyPremiumModifier && x.OwnerId == 223 && x.Value == 0.25f);
        if (earned) Assert.Contains(row.OwnerMultipliers, x => x.OwnerType == RewardModifierOwner.Entitlement && x.OwnerId == 44 && x.Value == 0.25f);
        Assert.Equal(expected - 1, row.OwnerMultipliers.Sum(x => x.Value));
        // Native140711110/140711220 multiply and truncate each medal increment separately.
        uint increment = (uint)(100f * row.Value);
        Assert.Equal(new[] { (uint)(100 * expected), (uint)(100 * expected) * 2, (uint)(100 * expected) * 3 },
            new[] { increment, increment * 2, increment * 3 });
        using var verify = Context(); AssertDefaults(verify);
        Assert.Equal(earned ? 1 : 0, verify.AccountEntitlement.Count(x => x.EntitlementId == 44));
    }

    [Fact]
    public void RawDeltaApiAndOtherDataKeepTheirMeaningWithoutInventedAnonymousOwnership()
    {
        var c = ClientFor(true);
        var property = c.Properties.GetRewardProperty(RewardPropertyType.RewardTrackType);
        c.Properties.UpdateRewardProperty(RewardPropertyType.RewardTrackType, 0.5f, 3);
        Assert.Equal(0.75f, property.GetValue(3)); Assert.Equal(1.75f, ReadChallenge(c.Packets.Last()).Value);
        property.SetValue(3, 0.239f);
        c.Properties.UpdateRewardProperty(RewardPropertyType.RewardTrackType, 0, 0);
        var row = ReadChallenge(c.Packets.Last());
        Assert.Equal(1.239f, row.Value);
        Assert.DoesNotContain(row.OwnerMultipliers, x => x.OwnerType is RewardModifierOwner.None or RewardModifierOwner.System);
        Assert.Equal(369u, (uint)(100f * row.Value) * 3); Assert.NotEqual((uint)(300f * row.Value), (uint)(100f * row.Value) * 3);
        c.Properties.UpdateRewardProperty(RewardPropertyType.XP, 0.5f, 3);
        Assert.Equal(0.5f, c.Packets.Last().Properties.Single().Value);
        Assert.Equal(0f, property.GetValue(0));
        Assert.Equal(1f, property.Build().Single(x => x.Data == 0).Value); // Mapped Contract key now projects the whole scalar.
    }

    [Theory]
    [InlineData(218046u, 82014u, 10800000u)]
    [InlineData(218049u, 82016u, 21600000u)]
    [InlineData(218052u, 82017u, 36000000u)]
    [InlineData(227802u, 87221u, 3600000u)]
    [InlineData(227806u, 87222u, 10800000u)]
    [InlineData(227810u, 87223u, 21600000u)]
    public void ActualChallengeEffectUsesRealApplyAndCapturedLifetimeCleanup(uint rowId, uint spellId, uint duration)
    {
        var c = ClientFor(true);
        Assert.True(c.Properties.CaptureChallengeModifier(out var initialModifier));
        // Spell4Effects.tbl sha256 ad49cb47af98a4b9b85e5f715af85545a8b286fb3ca1f26ad8cf8ea09097b3ed.
        var info = new SpellTargetInfo.SpellTargetEffectInfo(700, new Spell4EffectsEntry
        {
            Id = rowId, SpellId = spellId, TargetFlags = 1, EffectType = SpellEffectType.RewardPropertyModifier,
            DurationTime = duration, DataBits00 = 4, DataBits01 = 3, DataBits02 = 0x3f000000, ThreatMultiplier = 1, PhaseFlags = uint.MaxValue
        });
        var spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out var sp);
        sp.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = spellId });
        sp.SetProperty(nameof(ISpellInfo.BaseInfo), RecordingDispatchProxy<ISpellBaseInfo>.Create(out _));
        var parameters = new SpellParameters { SpellInfo = spellInfo };
        var spell = (RuntimeSpell)RuntimeHelpers.GetUninitializedObject(typeof(RuntimeSpell));
        Field(spell, "<Parameters>k__BackingField", parameters); Field(spell, "<Caster>k__BackingField", c.Player);
        using var provider = new ServiceCollection().AddSingleton(tables).BuildServiceProvider();
        var previous = SpellHandler.InitialiseDependencyResolver(new SpellEffectDependencyResolver(provider));
        try
        {
            SpellHandler.HandleEffectRewardPropertyModifier(spell, c.Player, info);
            Assert.Equal(1.75f, ReadChallenge(c.Packets.Last()).Value);
            Assert.True(c.Properties.CaptureChallengeModifier(out var flaskModifier));
            Assert.Equal(1.75f, flaskModifier.Multiplier);
            Assert.True(flaskModifier.Revision > initialModifier.Revision);
            Assert.True(flaskModifier.TryGetPoints(100, out uint flaskPoints));
            Assert.Equal(175u, flaskPoints);
            Assert.Contains(c.Packets.Last().Properties.Single(x => x.Data == 3).OwnerMultipliers,
                x => x.OwnerType == RewardModifierOwner.Spell && x.OwnerId == spellId && x.Value == 0.5f);
            var cleanup = (Action)typeof(RuntimeSpell).GetMethod("BuildLifetimeRemovalAction", BindingFlags.NonPublic | BindingFlags.Instance,
                [typeof(SpellEffectInterpretation), typeof(IUnitEntity), typeof(ISpellTargetEffectInfo)])!.Invoke(spell, [info.Interpretation, c.Player, info]);
            Assert.NotNull(cleanup);
            // A later mutable parameter change must not change which source this cleanup owns.
            var different = RecordingDispatchProxy<ISpellInfo>.Create(out var dp);
            dp.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 99999 });
            dp.SetProperty(nameof(ISpellInfo.BaseInfo), RecordingDispatchProxy<ISpellBaseInfo>.Create(out _));
            parameters.SpellInfo = different;
            cleanup(); cleanup();
            Assert.Equal(1.25f, ReadChallenge(c.Packets.Last()).Value);
            Assert.True(c.Properties.CaptureChallengeModifier(out var removedModifier));
            Assert.Equal(1.25f, removedModifier.Multiplier);
            Assert.True(removedModifier.Revision > flaskModifier.Revision);
            Assert.True(flaskModifier.TryGetPoints(100, out uint replayPoints));
            Assert.Equal(flaskPoints, replayPoints);
            Assert.DoesNotContain(c.Packets.Last().Properties.Single(x => x.Data == 3).OwnerMultipliers, x => x.OwnerType == RewardModifierOwner.Spell);
        }
        finally { SpellHandler.InitialiseDependencyResolver(previous); }
    }

    [Fact]
    public void ReinitializationAndLateOldActorCleanupPreserveCurrentSpellOwnership()
    {
        var c = ClientFor(true);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 1, 82014, 0.5f, false);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 2, 82014, 0.5f, false);
        c.Properties.Initialise(c.Player); c.Properties.SendInitialPackets();
        Assert.Equal(2.25f, ReadChallenge(c.Packets.Last()).Value);
        Assert.Equal(1f, Assert.Single(ReadChallenge(c.Packets.Last()).OwnerMultipliers, x => x.OwnerType == RewardModifierOwner.Spell).Value);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 1, 82014, 0.5f, true);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 1, 82014, 0.5f, true);
        Assert.Equal(1.75f, ReadChallenge(c.Packets.Last()).Value);
        var next = PlayerFor(c.Account, 200);
        c.Properties.Initialise(next);
        c.Properties.UpdateChallengeSpellModifier(next, 2, 87221, 0.5f, false);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 2, 82014, 0.5f, true);
        Assert.Equal(1.75f, ReadChallenge(c.Packets.Last()).Value);
        Assert.Contains(ReadChallenge(c.Packets.Last()).OwnerMultipliers, x => x.OwnerId == 87221);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CapturedOldSourceCannotRemoveAReplacementUsingTheSameRuntimeToken(bool differentSpell)
    {
        var c = ClientFor(false);
        uint replacementSpell = differentSpell ? 87221u : 82014u;
        float replacementValue = differentSpell ? 0.5f : 0.75f;
        c.Properties.UpdateChallengeSpellModifier(c.Player, 7, 82014, 0.5f, false);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 7, replacementSpell, replacementValue, false);
        int before = c.Packets.Count;
        c.Properties.UpdateChallengeSpellModifier(c.Player, 7, 82014, 0.5f, true);
        Assert.Equal(before, c.Packets.Count);
        Assert.Equal(1 + replacementValue, ReadChallenge(c.Packets.Last()).Value);
        Assert.Equal(replacementSpell, Assert.Single(ReadChallenge(c.Packets.Last()).OwnerMultipliers).OwnerId);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 7, replacementSpell, replacementValue, true);
        Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(4u)]
    public void SiblingEffectDataDoesNotChangeChallengeMultiplier(uint data)
    {
        var c = ClientFor(false);
        c.Properties.UpdateRewardProperty(RewardPropertyType.RewardTrackType, 0.5f, data);
        Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
        Assert.Equal(0.5f, c.Properties.GetRewardProperty(RewardPropertyType.RewardTrackType).GetValue(data));
        Assert.Equal(data < 2 ? 1.5f : 0.5f, c.Packets.Last().Properties.Single(x => x.Data == data).Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Earned169BonusFollowsActualOuterCommitAndRetainsSpellAcrossRetry(bool commit)
    {
        var c = Earn169();
        c.Properties.UpdateChallengeSpellModifier(c.Player, 1, 82014, 0.5f, false);
        c.Properties.SendInitialPackets();
        Assert.Equal(1.5f, ReadChallenge(c.Packets.Last()).Value);
        using (var context = Context())
        {
            using var transaction = context.Database.BeginTransaction();
            c.Account.Save(context); context.SaveChanges();
            Assert.Equal(1.5f, ReadChallenge(c.Packets.Last()).Value);
            if (commit) transaction.Commit(); else transaction.Rollback();
        }
        Assert.Equal(commit ? 1.75f : 1.5f, ReadChallenge(c.Packets.Last()).Value);
        Save(c);
        Assert.Equal(1.75f, ReadChallenge(c.Packets.Last()).Value);
        var reload = ClientFor(false, 200); reload.Properties.SendInitialPackets();
        Assert.Equal(1.25f, ReadChallenge(reload.Packets.Last()).Value);
        using var verify = Context(); AssertDefaults(verify);
        Assert.DoesNotContain(verify.AccountInventory, x => x.AccountItemId == 169);
        Assert.True(verify.AccountLoyaltySlot.Single(x => x.TrackId == 12 && x.Position == 0).Claimed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SavepointUncertaintyDoesNotPublishUntilFreshSuccessfulAcknowledgement(bool afterWrites)
    {
        var c = Earn169(); c.Properties.SendInitialPackets();
        using (var context = Context())
        {
            using var tx = context.Database.BeginTransaction();
            if (!afterWrites) tx.CreateSavepoint("benefit");
            c.Account.Save(context); context.SaveChanges();
            if (afterWrites) tx.CreateSavepoint("benefit");
            tx.RollbackToSavepoint("benefit"); tx.Commit();
        }
        Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
        Save(c); Save(c);
        Assert.Equal(1.25f, ReadChallenge(c.Packets.Last()).Value);
        Assert.Single(c.Packets, x => ReadChallenge(x).Value == 1.25f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedFlushOrDisposedTransactionRetainsBonusIntent(bool disposeTransaction)
    {
        var c = Earn169(); c.Properties.SendInitialPackets();
        if (disposeTransaction)
        {
            using var context = Context();
            var tx = context.Database.BeginTransaction(); c.Account.Save(context); context.SaveChanges(); tx.Dispose(); context.SaveChanges();
        }
        else
        {
            using (var setup = Context()) setup.Database.ExecuteSqlRaw("CREATE TRIGGER reject_bonus BEFORE INSERT ON account_entitlement WHEN NEW.entitlementId = 44 BEGIN SELECT RAISE(ABORT, 'bonus failure'); END");
            Assert.Throws<DbUpdateException>(() => Save(c));
            using var setupAfter = Context(); setupAfter.Database.ExecuteSqlRaw("DROP TRIGGER reject_bonus");
        }
        Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
        Save(c); Assert.Equal(1.25f, ReadChallenge(c.Packets.Last()).Value);
    }

    [Fact]
    public void PendingWorkingAmountAndOldTerminalNotificationsCannotPublishOrRegressCommittedBonus()
    {
        var c = ClientFor(false);
        c.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyChallengeBonus, 1);
        var entitlement = (AccountEntitlement)c.Entitlements.GetEntitlement(EntitlementType.LoyaltyChallengeBonus);
        // A manager built after the provisional update must still read the durable snapshot0.
        var replacement = new RewardPropertyManager(c.Account, null, tables); Set(c.Account, nameof(IAccount.RewardPropertyManager), replacement);
        replacement.Initialise(c.Player); replacement.SendInitialPackets();
        Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
        using (var context = Context())
        {
            using var tx = context.Database.BeginTransaction();
            c.Account.Save(context); context.SaveChanges();
            entitlement.Amount = 0; // Later unenrolled intent is allowed to survive the old captured commit.
            tx.Commit();
        }
        Assert.Equal(1.25f, ReadChallenge(c.Packets.Last()).Value);
        Save(c); Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
        replacement.RefreshCommittedEntitlement(entitlement, 1, 1); // Delayed notification from the older commit.
        replacement.SendInitialPackets(); Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
        using var verify = Context(); Assert.Equal(0u, verify.AccountEntitlement.Single(x => x.EntitlementId == 44).Amount);
    }

    [Fact]
    public void PreSqlRefreshAndRejectedReenrollmentKeepProjectionAtTheAcknowledgedSnapshot()
    {
        var c = ClientFor(false); c.Properties.SendInitialPackets();
        c.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyChallengeBonus, 1);
        var ent = c.Entitlements.GetEntitlement(EntitlementType.LoyaltyChallengeBonus);
        using (var context = Context())
        {
            c.Account.Save(context); ent.Amount = 0; c.Account.Save(context); context.SaveChanges();
        }
        Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
        ent.Amount = 1;
        using (var context = Context())
        {
            using var tx = context.Database.BeginTransaction(); c.Account.Save(context); context.SaveChanges();
            ent.Amount = 0; ent.Amount = 1;
            Assert.Throws<InvalidOperationException>(() => c.Account.Save(context));
            Assert.Throws<InvalidOperationException>(() => tx.Commit()); tx.Rollback();
        }
        Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
        Save(c); Assert.Equal(1.25f, ReadChallenge(c.Packets.Last()).Value);
    }

    [Fact]
    public void DerivedNotificationFailureCannotFailCommittedSaveOrSuppressOtherAcknowledgements()
    {
        var c = Earn169();
        var broken = RecordingDispatchProxy<IRewardPropertyManager>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IRewardPropertyManager.RefreshCommittedEntitlement), _ => throw new InvalidOperationException("notification only"));
        Set(c.Account, nameof(IAccount.RewardPropertyManager), broken);
        bool completed = false;
        using (var context = Context())
        {
            c.Account.Save(context); context.EnlistSaveWork(_ => { }, result => completed = result); context.SaveChanges();
        }
        Assert.True(completed);
        Assert.Single(proxy.GetInvocations(nameof(IRewardPropertyManager.RefreshCommittedEntitlement)));
        var reloaded = ClientFor(false); reloaded.Properties.SendInitialPackets();
        Assert.Equal(1.25f, ReadChallenge(reloaded.Packets.Last()).Value);
    }

    [Fact]
    public void CommitBeforeInitialSnapshotUpdatesSilentlyThenEmitsOnce()
    {
        var c = Earn169(); Assert.Empty(c.Packets);
        Save(c); Assert.Empty(c.Packets);
        c.Properties.SendInitialPackets();
        Assert.Equal(1.25f, ReadChallenge(Assert.Single(c.Packets)).Value);
        Save(c); Assert.Single(c.Packets);
    }

    [Fact]
    public void LosingOverlapCannotPublishAWorkingBenefitAfterOriginalOwnerFinishes()
    {
        var c = Earn169(); c.Properties.SendInitialPackets();
        using var owner = Context(); c.Account.Save(owner);
        using var rejected = Context();
        Assert.Throws<InvalidOperationException>(() => c.Account.Save(rejected));
        Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
        owner.SaveChanges(); Assert.Equal(1.25f, ReadChallenge(c.Packets.Last()).Value);
        Assert.Throws<InvalidOperationException>(() => rejected.SaveChanges());
        Assert.Single(c.Packets, x => ReadChallenge(x).Value == 1.25f);
    }

    [Fact]
    public void ForeignOrProvisionalRefreshCannotSupplyAnEarnedBonus()
    {
        var c = ClientFor(false); c.Properties.SendInitialPackets();
        c.Entitlements.UpdateEntitlement(EntitlementType.LoyaltyChallengeBonus, 1);
        var current = c.Entitlements.GetEntitlement(EntitlementType.LoyaltyChallengeBonus);
        c.Properties.RefreshCommittedEntitlement(current, 1, 1);
        var foreign = new AccountEntitlement(c.Account, new AccountEntitlementModel { Id = 1, EntitlementId = 44, Amount = 1 },
            new EntitlementEntry { Id = 44, MaxCount = 1 });
        c.Properties.RefreshCommittedEntitlement(foreign, 1, 0);
        c.Properties.SendInitialPackets(); Assert.Equal(1f, ReadChallenge(c.Packets.Last()).Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IdentityProjectionRequiresAnActualAdditivePropertyTable(bool wrongKind)
    {
        var c = ClientFor(false);
        var absent = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        if (wrongKind) proxy.SetProperty(nameof(IGameTableManager.RewardProperty), PlayerBindPointTests.Table(new RewardPropertyEntry { Id = 4, RewardModifierValueTypeEnum = 1 }));
        var manager = new RewardPropertyManager(c.Account, null, absent);
        manager.SendInitialPackets(); Assert.Empty(c.Packets.Last().Properties);
        Assert.False(manager.CaptureChallengeModifier(out _));
    }

    [Fact]
    public void LegacyInterfaceImplementationKeepsItsExistingDeltaFallback()
    {
        var implementation = new LegacyRewardProperties();
        IRewardPropertyManager manager = implementation;
        manager.UpdateChallengeSpellModifier(null, 1, 82014, 0.5f, false);
        Assert.Equal(0.5f, implementation.Delta);
        manager.UpdateChallengeSpellModifier(null, 1, 82014, 0.5f, true);
        Assert.Equal(0f, implementation.Delta);
        manager.RefreshCommittedEntitlement(null, 1, 1);
        Assert.False(manager.CaptureChallengeModifier(out _));
        Assert.Equal(2, implementation.Calls);
    }

    [Theory]
    [InlineData(false, false, 1f)]
    [InlineData(true, false, 1.25f)]
    [InlineData(false, true, 1.25f)]
    [InlineData(true, true, 1.5f)]
    public void CapturedModifierMatchesActualBasicSignatureAndCommittedEarned44WireValue(bool signature, bool earned, float expected)
    {
        if (earned) SeedEarned();
        var c = ClientFor(signature);
        c.Properties.SendInitialPackets();
        Assert.True(c.Properties.CaptureChallengeModifier(out var snapshot));
        Assert.Equal(expected, snapshot.Multiplier);
        Assert.Equal(ReadChallenge(c.Packets.Last()).Value, snapshot.Multiplier);
        Assert.True(snapshot.TryGetPoints(100, out uint points));
        Assert.Equal((uint)(100 * expected), points);
        Assert.Equal(signature ? 0.25f : (float?)null,
            c.Properties.GetRewardProperty(RewardPropertyType.RewardTrackType).GetValue(3));
        using var context = Context(); AssertDefaults(context);
        Assert.Equal(earned ? 1 : 0, context.AccountEntitlement.Count(row => row.EntitlementId == 44));
    }

    [Fact]
    public void FrozenModifierReplaysInitialEvaluationAfterLaterMedalSourceChanges()
    {
        var c = ClientFor(false);
        var property = c.Properties.GetRewardProperty(RewardPropertyType.RewardTrackType);
        property.SetValue(3, 0.239f);
        Assert.True(c.Properties.CaptureChallengeModifier(out var earlier));
        Assert.True(earlier.TryGetPoints(100, out uint oldPoints));
        Assert.Equal(123u, oldPoints);

        property.SetValue(3, 0.5f);
        Assert.True(c.Properties.CaptureChallengeModifier(out var later));
        Assert.True(later.Revision > earlier.Revision);
        Assert.Equal(1.5f, later.Multiplier);
        Assert.True(later.TryGetPoints(100, out uint newPoints));
        Assert.Equal(150u, newPoints);
        Assert.True(earlier.TryGetPoints(100, out uint replay));
        Assert.Equal(oldPoints, replay);
    }

    [Fact]
    public void SnapshotRevisionTracksAcceptedSourcesAndIgnoresStaleCallbacksAndSiblingData()
    {
        var c = ClientFor(false);
        Assert.True(c.Properties.CaptureChallengeModifier(out var identity));
        c.Properties.Initialise(c.Player);
        c.Properties.UpdateRewardProperty(RewardPropertyType.RewardTrackType, 0.5f, 0);
        Assert.True(c.Properties.CaptureChallengeModifier(out var same));
        Assert.Equal(identity.Revision, same.Revision);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 7, 82014, 0.5f, false);
        Assert.True(c.Properties.CaptureChallengeModifier(out var applied));
        Assert.True(applied.Revision > same.Revision);

        c.Properties.UpdateChallengeSpellModifier(c.Player, 7, 82014, 0.5f, false);
        c.Properties.UpdateChallengeSpellModifier(PlayerFor(c.Account, 200), 7, 82014, 0.5f, true);
        Assert.True(c.Properties.CaptureChallengeModifier(out var duplicate));
        Assert.Equal(applied.Revision, duplicate.Revision);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 7, 87221, 0.5f, false);
        Assert.True(c.Properties.CaptureChallengeModifier(out var replaced));
        Assert.Equal(applied.Multiplier, replaced.Multiplier);
        Assert.True(replaced.Revision > applied.Revision);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 7, 82014, 0.5f, true);
        Assert.True(c.Properties.CaptureChallengeModifier(out var stale));
        Assert.Equal(replaced.Revision, stale.Revision);
        c.Properties.UpdateChallengeSpellModifier(c.Player, 7, 87221, 0.5f, true);
        Assert.True(c.Properties.CaptureChallengeModifier(out var removed));
        Assert.Equal(1f, removed.Multiplier);
        Assert.True(removed.Revision > replaced.Revision);
        c.Properties.Initialise(PlayerFor(c.Account, 201));
        Assert.True(c.Properties.CaptureChallengeModifier(out var newActor));
        Assert.True(newActor.Revision > removed.Revision);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CapturedEarned44ChangesOnlyAfterActualCommitAndNoopRefreshKeepsRevision(bool commit)
    {
        var c = Earn169();
        c.Properties.SendInitialPackets();
        Assert.True(c.Properties.CaptureChallengeModifier(out var before));
        Assert.Equal(1f, before.Multiplier);
        using (var context = Context())
        {
            using var transaction = context.Database.BeginTransaction();
            c.Account.Save(context); context.SaveChanges();
            Assert.True(c.Properties.CaptureChallengeModifier(out var pending));
            Assert.Equal(before.Revision, pending.Revision);
            Assert.Equal(before.Multiplier, pending.Multiplier);
            if (commit) transaction.Commit(); else transaction.Rollback();
        }
        Assert.True(c.Properties.CaptureChallengeModifier(out var terminal));
        Assert.Equal(commit ? 1.25f : 1f, terminal.Multiplier);
        if (commit) Assert.True(terminal.Revision > before.Revision);
        else Assert.Equal(before.Revision, terminal.Revision);
        Save(c);
        Assert.True(c.Properties.CaptureChallengeModifier(out var committed));
        Assert.Equal(1.25f, committed.Multiplier);
        var owner = (AccountEntitlement)c.Entitlements.GetEntitlement(EntitlementType.LoyaltyChallengeBonus);
        var durable = owner.GetCommittedSnapshot();
        c.Properties.RefreshCommittedEntitlement(owner, durable.Amount, durable.Revision);
        Assert.True(c.Properties.CaptureChallengeModifier(out var refreshed));
        Assert.Equal(committed.Revision, refreshed.Revision);
        Assert.True(before.TryGetPoints(100, out uint replay));
        Assert.Equal(100u, replay);
    }

    private sealed class LegacyRewardProperties : IRewardPropertyManager
    {
        public float Delta { get; private set; }
        public int Calls { get; private set; }
        public void Initialise(IPlayer player) { }
        public void SendInitialPackets() { }
        public IRewardProperty GetRewardProperty(RewardPropertyType type) => null;
        public void UpdateRewardProperty(RewardPropertyType type, float value, uint data = 0) { Assert.Equal(RewardPropertyType.RewardTrackType, type); Assert.Equal(3u, data); Delta += value; Calls++; }
        public void UpdateRewardProperty(RewardPropertyEntry entry, float value, uint data = 0) => UpdateRewardProperty((RewardPropertyType)entry.Id, value, data);
    }

    private Client Earn169()
    {
        var c = ClientFor(false);
        // First three actual Loyalty tiers total24,000; tier12's first slot is11,200.
        c.Currency.CurrencyAddAmount(AccountCurrencyType.CosmicReward, 35200); Save(c);
        string group;
        using (var context = Context()) group = context.AccountPendingItem.Single(x => x.AccountItemId == 169).GroupName;
        Assert.Equal("loyalty:1:12:0", group);
        Assert.Equal(AccountOperationResult.Ok, c.Inventory.ClaimPendingItemGroup(c.Player, group)); Save(c);
        var source = Assert.Single(c.Inventory);
        Assert.Equal(169u, source.AccountItemId);
        Assert.Equal(AccountOperationResult.Ok, c.Inventory.TakeItem(null, source.Id));
        Assert.Equal(1u, c.Entitlements.GetEntitlement(EntitlementType.LoyaltyChallengeBonus).Amount);
        return c;
    }

    private void SeedEarned()
    {
        using var context = Context(); context.AccountEntitlement.Add(new AccountEntitlementModel { Id = 1, EntitlementId = 44, Amount = 1 }); context.SaveChanges();
    }

    private Client ClientFor(bool signature, ulong character = 100)
    {
        using var context = Context();
        var model = context.Account.AsNoTracking().Include(x => x.AccountCurrency).Include(x => x.AccountPendingItem).Include(x => x.AccountInventory)
            .Include(x => x.AccountEntitlement).Include(x => x.Loyalty).Include(x => x.LoyaltySlots).Single();
        var account = new NexusForever.Game.Account.Account();
        Set(account, nameof(IAccount.Id), 1u);
        Set(account, nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out var session));
        var rbac = RecordingDispatchProxy<IAccountRBACManager>.Create(out var rp); rp.SetMethodReturn(nameof(IAccountRBACManager.HasPermission), signature);
        Set(account, nameof(IAccount.RbacManager), rbac);
        Set(account, nameof(IAccount.GenericUnlockManager), RecordingDispatchProxy<IGenericUnlockManager>.Create(out _));
        Set(account, nameof(IAccount.CostumeManager), RecordingDispatchProxy<IAccountCostumeManager>.Create(out _));
        Set(account, nameof(IAccount.KeybindingManager), RecordingDispatchProxy<IAccountKeybindingManager>.Create(out _));
        Set(account, nameof(IAccount.RewardRotationGrantManager), new AccountRewardRotationGrantManager(account, model));
        var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap);
        ap.SetMethodReturn(nameof(IAssetManager.GetRewardPropertiesForTier), signature ? new[] { new RewardPropertyPremiumModifierEntry
        { Id = 223, PremiumSystemEnum = 1, Tier = 1, RewardPropertyId = 4, RewardPropertyData = 3, ModifierValueFloat = 0.25f } }.ToImmutableList() : ImmutableList<RewardPropertyPremiumModifierEntry>.Empty);
        var entitlements = new AccountEntitlementManager(account, model, assets, tables); Set(account, nameof(IAccount.EntitlementManager), entitlements);
        var currency = new AccountCurrencyManager(account, model, tables); Set(account, nameof(IAccount.CurrencyManager), currency);
        var inventory = new AccountInventoryManager(account, model, null, gameTableManager: tables); Set(account, nameof(IAccount.InventoryManager), inventory);
        var database = new AuthDatabase(); database.Initialise(connection);
        Set(account, nameof(IAccount.LoyaltyManager), new AccountLoyaltyManager(account, tables, database));
        var properties = new RewardPropertyManager(account, assets, tables); Set(account, nameof(IAccount.RewardPropertyManager), properties);
        var player = PlayerFor(account, character); properties.Initialise(player);
        return new(account, player, properties, currency, entitlements, inventory, session);
    }

    private static IPlayer PlayerFor(IAccount account, ulong character)
    {
        var player = RecordingDispatchProxy<IPlayer>.Create(out var pp); pp.SetProperty(nameof(IPlayer.Account), account);
        pp.SetProperty(nameof(IPlayer.CharacterId), character); pp.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 1, Id = character });
        return player;
    }

    private static ServerPremiumRewards.RewardProperty ReadChallenge(ServerPremiumRewards packet)
    {
        using var stream = new MemoryStream(); using (var writer = new GamePacketWriter(stream)) { packet.Write(writer); writer.FlushBits(); }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var rows = new List<ServerPremiumRewards.RewardProperty>();
        int count = reader.ReadByte();
        for (int i = 0; i < count; i++)
        {
            var row = new ServerPremiumRewards.RewardProperty { RewardPropertyId = reader.ReadEnum<RewardPropertyType>(6), Data = reader.ReadUInt(), ModifierType = reader.ReadEnum<RewardModifierValueType>(2) };
            row.Value = row.ModifierType == RewardModifierValueType.Discrete ? reader.ReadUInt() : reader.ReadSingle();
            uint owners = reader.ReadUInt(8);
            for (int j = 0; j < owners; j++)
            {
                var owner = new ServerPremiumRewards.RewardProperty.RewardOwnerMultiplier { OwnerType = reader.ReadEnum<RewardModifierOwner>(4), OwnerId = reader.ReadUInt(), ModifierType = reader.ReadEnum<RewardModifierValueType>(2) };
                owner.Value = owner.ModifierType == RewardModifierValueType.Discrete ? reader.ReadUInt() : reader.ReadSingle(); row.OwnerMultipliers.Add(owner);
            }
            rows.Add(row);
        }
        return Assert.Single(rows, row => row.RewardPropertyId == RewardPropertyType.RewardTrackType && row.Data == 3);
    }

    private static void Set(object instance, string property, object value) => Field(instance, $"<{property}>k__BackingField", value);
    private static void Field(object instance, string field, object value) => instance.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(instance, value);
    private AuthContext Context() => new(connection);
    private void Save(Client c) { using var context = Context(); c.Account.Save(context); context.SaveChanges(); }
    private static void AssertDefaults(AuthContext context)
    {
        Assert.Equal(1u, context.AccountEntitlement.Single(x => x.EntitlementId == 15).Amount);
        Assert.Equal(1u, context.AccountEntitlement.Single(x => x.EntitlementId == 17).Amount);
    }
    private sealed record Client(IAccount Account, IPlayer Player, RewardPropertyManager Properties, AccountCurrencyManager Currency,
        AccountEntitlementManager Entitlements, AccountInventoryManager Inventory, RecordingDispatchProxy<IGameSession> Session)
    {
        public List<ServerPremiumRewards> Packets => Session.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(x => x.Arguments[0]).OfType<ServerPremiumRewards>().ToList();
    }
    public void Dispose()
    {
        using var pool = new Microsoft.Data.Sqlite.SqliteConnection(connection.ConnectionString); Microsoft.Data.Sqlite.SqliteConnection.ClearPool(pool);
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
