using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Entitlement;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Account.Entitlement;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reward;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Reward;
using RuntimeSpell = NexusForever.Game.Spell.Spell;

namespace NexusForever.Game.Tests.Account.Reward;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public sealed class ContractRewardPropertyTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public ContractRewardPropertyTests() { }
    internal ContractRewardPropertyTests(DbContextOptions<CharacterContext> options) => this.options = options;
    public static IEnumerable<object[]> ActualEffects() => Effects().Select(row => new object[] { row.Id, row.SpellId, row.DataBits01 });
    public static IEnumerable<object[]> ActualSources() => ContractCompletionRepositoryTests.ActualSources();

    [Theory]
    [MemberData(nameof(ActualEffects))]
    public void EveryActualContractEffectUsesRealApplyAndCapturedIdempotentLifetimeCleanup(uint rowId, uint spellId, uint type)
    {
        var f = new Fixture(signature: true, earned: true); var before = f.Capture(type);
        var effect = new SpellTargetInfo.SpellTargetEffectInfo(700, Effects().Single(row => row.Id == rowId));
        var spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out var sp); sp.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = spellId });
        sp.SetProperty(nameof(ISpellInfo.BaseInfo), RecordingDispatchProxy<ISpellBaseInfo>.Create(out _));
        var spell = (RuntimeSpell)RuntimeHelpers.GetUninitializedObject(typeof(RuntimeSpell));
        Field(spell, "<Parameters>k__BackingField", new SpellParameters { SpellInfo = spellInfo }); Field(spell, "<Caster>k__BackingField", f.Player);
        using var services = new ServiceCollection().AddSingleton(f.Tables).BuildServiceProvider();
        var previous = SpellHandler.InitialiseDependencyResolver(new SpellEffectDependencyResolver(services));
        try
        {
            SpellHandler.HandleEffectRewardPropertyModifier(spell, f.Player, effect);
            var captured = f.Capture(type); Assert.Equal(1.5f, captured.Multiplier); Assert.True(captured.Revision > before.Revision);
            Assert.True(captured.TryGetPoints(50, out uint points)); Assert.Equal(75u, points);
            Assert.Equal(0.5f, f.Properties.GetRewardProperty(RewardPropertyType.RewardTrackType).GetValue(type));
            var wire = f.Row(type); Assert.Equal(1.5f, wire.Value);
            Assert.Contains(wire.OwnerMultipliers, owner => owner.OwnerType == RewardModifierOwner.Spell && owner.OwnerId == spellId && owner.Value == 0.5f);
            Assert.DoesNotContain(wire.OwnerMultipliers, owner => owner.OwnerType == RewardModifierOwner.Entitlement);
            Assert.Equal(1f, f.Capture(1 - type).Multiplier); Assert.Equal(1.5f, f.Challenge().Multiplier);
            var cleanup = (Action)typeof(RuntimeSpell).GetMethod("BuildLifetimeRemovalAction", BindingFlags.Instance | BindingFlags.NonPublic,
                [typeof(SpellEffectInterpretation), typeof(IUnitEntity), typeof(ISpellTargetEffectInfo)])!.Invoke(spell, [effect.Interpretation, f.Player, effect]);
            Assert.NotNull(cleanup);
            var replacement = RecordingDispatchProxy<ISpellInfo>.Create(out var rp); rp.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 99999 });
            rp.SetProperty(nameof(ISpellInfo.BaseInfo), RecordingDispatchProxy<ISpellBaseInfo>.Create(out _));
            spell.Parameters.SpellInfo = replacement; effect.Entry.Id = 99999; // Cleanup must retain original source identities.
            cleanup(); cleanup(); Assert.Equal(1f, f.Capture(type).Multiplier);
            Assert.Equal(1f, f.Row(type).Value); Assert.Equal(1.5f, captured.Multiplier); Assert.Equal(1.5f, f.Challenge().Multiplier);
        }
        finally { SpellHandler.InitialiseDependencyResolver(previous); }
    }

    [Theory]
    [MemberData(nameof(ActualSources))]
    public async Task EveryActualContractSourceFreezesItsOwnModifierAndProvenanceInTheAtomicReceipt(uint questId)
    {
        using var database = new ContractCompletionRepositoryTests.Fixture(options, questId); var f = new Fixture(signature: true, earned: true);
        uint type = database.Source.TrackType; var row = Effects().First(row => row.DataBits01 == type);
        f.Properties.UpdateContractSpellModifier(f.Player, 700, row.SpellId, row.Id, type, 0.5f, false);
        var modifier = f.Capture(type); var template = database.Command();
        var command = ContractCompletionCommand.FromModifier(42, template.AcceptanceId, template.Source, modifier, 0, template.CreatedUtc, template.Changes);
        f.Properties.UpdateContractSpellModifier(f.Player, 700, row.SpellId, row.Id, type, 0.5f, true);
        var result = await database.Execute(command); Assert.Equal(template.Source.BasePoints * 3 / 2, result.Track.Points);
        using var evaluation = JsonDocument.Parse(command.Evaluation);
        Assert.Equal(1.5f, evaluation.RootElement.GetProperty("Multiplier").GetSingle());
        Assert.Equal(row.Id, evaluation.RootElement.GetProperty("Sources")[0].GetProperty("SourceEffectId").GetUInt32());
        Assert.False((await database.Execute(command)).Applied); Assert.Equal(1f, f.Capture(type).Multiplier);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void SignatureAndEarnedChallengeEntitlementNeverBecomeContractBonuses(bool signature, bool earned)
    {
        var f = new Fixture(signature, earned);
        Assert.Equal(1f, f.Capture(0).Multiplier); Assert.Equal(1f, f.Capture(1).Multiplier);
        Assert.Equal(1f + (signature ? 0.25f : 0) + (earned ? 0.25f : 0), f.Challenge().Multiplier);
        Assert.Empty(f.Capture(0).Sources); Assert.Empty(f.Capture(1).Sources);
    }

    [Fact]
    public void IndependentInstancesStackAndSameActorRefreshPreservesContractSpellsWithoutDoublePremiumOrCrossTypeCredit()
    {
        var f = new Fixture(true, true); var effect = Effects()[0];
        f.Apply(effect, 10); f.Apply(effect, 10); f.Apply(effect, 11); Assert.Equal(2f, f.Capture(0).Multiplier);
        Assert.Equal(2, f.Capture(0).Sources.Count); Assert.Equal(1f, f.Capture(1).Multiplier);
        f.Properties.Initialise(f.Player); Assert.Equal(2f, f.Capture(0).Multiplier); Assert.Equal(1.5f, f.Challenge().Multiplier);
        Assert.Equal(1f, f.Row(0).OwnerMultipliers.Single(owner => owner.OwnerId == effect.SpellId).Value);
        f.Apply(effect, 10, true); f.Apply(effect, 10, true); Assert.Equal(1.5f, f.Capture(0).Multiplier);
        f.Apply(effect, 11, true); Assert.Equal(1f, f.Capture(0).Multiplier);
    }

    [Fact]
    public void PreviousActorCleanupCannotRemoveTheReplacementActorsIdenticalRuntimeToken()
    {
        var f = new Fixture(); var effect = Effects()[0]; var old = f.Player; f.Apply(effect, 10);
        f.BindReplacement(); f.Apply(effect, 10);
        int packets = f.Packets.Count;
        f.Properties.UpdateContractSpellModifier(old, 10, effect.SpellId, effect.Id, 0, 0.5f, true);
        Assert.Equal(packets, f.Packets.Count); Assert.Equal(1.5f, f.Capture(0).Multiplier);
        Assert.False(f.Properties.CaptureContractModifier(old, 4, 0, out _));
    }

    [Theory]
    [InlineData("unknown-effect")] [InlineData("altered-row")] [InlineData("anonymous")]
    [InlineData("specific-track")] [InlineData("premium")]
    public void UnmappedContributionsKeepTheirRawAndWireBehaviorButDoNotAuthorizeEvaluatedCredit(string kind)
    {
        var f = new Fixture(); var effect = Effects()[0];
        switch (kind)
        {
            case "unknown-effect": f.Properties.UpdateContractSpellModifier(f.Player, 10, 999, 999, 0, 0.5f, false); break;
            case "altered-row": f.Rows[0].DataBits04 = 1; f.Apply(effect, 10); break;
            case "anonymous": f.Properties.UpdateRewardProperty(RewardPropertyType.RewardTrackType, 0.5f, 0); break;
            case "specific-track": f.Properties.UpdateRewardProperty(RewardPropertyType.RewardTrackId, 0.5f, 4); break;
            case "premium": f.Premium.Add(new() { Id = 999, RewardPropertyId = 4, RewardPropertyData = 0, ModifierValueFloat = 0.5f }); f.Properties.Initialise(f.Player); break;
        }
        Assert.False(f.Properties.CaptureContractModifier(f.Player, 4, 0, out _));
        if (kind != "specific-track") { Assert.Equal(0.5f, f.Properties.GetRewardProperty(RewardPropertyType.RewardTrackType).GetValue(0)); Assert.Equal(1.5f, f.Row(0).Value); }
        Assert.Equal(1f, f.Capture(1).Multiplier);
    }

    [Theory]
    [InlineData("session")] [InlineData("account")] [InlineData("track-type")] [InlineData("track-owner")]
    [InlineData("missing-effects")] [InlineData("missing-id-property")] [InlineData("invalid-property")]
    public void CaptureRequiresTheCurrentActorAndMappedPropertyAndTrackOwners(string invalid)
    {
        var f = new Fixture(); IPlayer player = f.Player;
        switch (invalid)
        {
            case "session": f.AccountProxy.SetProperty(nameof(IAccount.Session), RecordingDispatchProxy<IGameSession>.Create(out _)); break;
            case "account": var replacement = RecordingDispatchProxy<IPlayer>.Create(out var rp); rp.SetProperty(nameof(IPlayer.Session), f.Session); rp.SetProperty(nameof(IPlayer.Account), RecordingDispatchProxy<IAccount>.Create(out _)); player = replacement; break;
            case "track-type": f.Tracks[0].RewardTrackTypeEnum = 3; break;
            case "track-owner": f.Tracks[0].RewardTrackIdParent = 9; break;
            case "missing-effects": f.TableProxy.SetProperty(nameof(IGameTableManager.Spell4Effects), null); break;
            case "missing-id-property": f.TableProxy.SetProperty(nameof(IGameTableManager.RewardProperty), PlayerBindPointTests.Table(new RewardPropertyEntry { Id = 4 })); break;
            case "invalid-property": f.PropertyRows[1].RewardModifierValueTypeEnum = 1; break;
        }
        Assert.False(f.Properties.CaptureContractModifier(player, 4, 0, out _));
    }

    [Fact]
    public void SnapshotSourcesAreCopiedAndOnlyUnambiguousIntegralAwardsAreEvaluated()
    {
        var f = new Fixture(); f.Apply(Effects()[0], 10); var frozen = f.Capture(0);
        Assert.Throws<NotSupportedException>(() => ((IList<ContractRewardModifierSource>)frozen.Sources).Clear());
        f.Apply(Effects()[0], 11); Assert.Single(frozen.Sources); Assert.Equal(1.5f, frozen.Multiplier);
        Assert.True(frozen.TryGetPoints(50, out uint points)); Assert.Equal(75u, points);
        Assert.False(frozen.TryGetPoints(1, out _)); Assert.False(frozen.TryGetPoints(uint.MaxValue, out _));
    }

    [Theory]
    [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1f)] [InlineData(0f)] [InlineData(1.239f)]
    public void UnmappedOrInvalidScalarArithmeticCannotBeConvertedIntoAContractCommand(float multiplier)
        => Assert.False(new ContractRewardModifierSnapshot(4, 0, multiplier, 1, []).TryGetPoints(50, out _));

    [Theory]
    [InlineData(0u)] [InlineData(1u)]
    public void ExistingCustomManagersRetainTheirLegacyDeltaHookWithoutImplicitContractCreditAuthority(uint type)
    {
        var legacy = new LegacyRewards(); IRewardPropertyManager manager = legacy;
        manager.UpdateContractSpellModifier(null, 10, 82014, 218043, type, 0.5f, false);
        Assert.Equal(0.5f, legacy.Total); Assert.Equal(type, legacy.Data);
        manager.UpdateContractSpellModifier(null, 10, 82014, 218043, type, 0.5f, true); Assert.Equal(0f, legacy.Total);
        Assert.False(manager.CaptureContractModifier(null, type == 0 ? 4u : 5u, type, out var captured)); Assert.Null(captured);
    }

    private sealed class LegacyRewards : IRewardPropertyManager
    {
        internal float Total; internal uint Data;
        public void Initialise(IPlayer actor) { }
        public void SendInitialPackets() { }
        public IRewardProperty GetRewardProperty(RewardPropertyType type) => null;
        public void UpdateRewardProperty(RewardPropertyType type, float value, uint data = 0) { Total += value; Data = data; }
        public void UpdateRewardProperty(RewardPropertyEntry entry, float value, uint data = 0) { Total += value; Data = data; }
    }

    internal static Spell4EffectsEntry[] Effects()
    {
        // Actual build16042 identities/property4 keys and +0.5 values, verified by
        // contract-modifiers/SourceAudit. Timing is not interpreted by this fixture.
        (uint Id, uint Spell, uint Type)[] rows = [(218043,82014,0),(218045,82014,1),(218047,82016,0),(218048,82016,1),
            (218050,82017,0),(218051,82017,1),(227800,87221,0),(227801,87221,1),(227804,87222,0),(227805,87222,1),(227808,87223,0),(227809,87223,1)];
        return rows.Select(row => new Spell4EffectsEntry { Id = row.Id, SpellId = row.Spell, EffectType = SpellEffectType.RewardPropertyModifier,
            DataBits00 = 4, DataBits01 = row.Type, DataBits02 = 0x3F000000, DurationTime = 1000, TargetFlags = 1,
            ParameterType = new SpellEffectParameterType[4], ParameterValue = new float[4] }).ToArray();
    }
    private static void Field(object owner, string name, object value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(owner, value);

    internal sealed class Fixture
    {
        internal readonly IGameTableManager Tables;
        internal readonly RecordingDispatchProxy<IGameTableManager> TableProxy;
        internal readonly IAccount Account;
        internal readonly RecordingDispatchProxy<IAccount> AccountProxy;
        internal readonly IGameSession Session;
        private readonly RecordingDispatchProxy<IGameSession> sessionProxy;
        internal IPlayer Player { get; private set; }
        internal readonly RewardPropertyManager Properties;
        internal readonly Spell4EffectsEntry[] Rows = Effects();
        internal readonly RewardTrackEntry[] Tracks = [new() { Id = 4, RewardTrackTypeEnum = 0 }, new() { Id = 5, RewardTrackTypeEnum = 1 }];
        internal readonly RewardPropertyEntry[] PropertyRows = [new() { Id = 3 }, new() { Id = 4 }];
        internal readonly List<RewardPropertyPremiumModifierEntry> Premium = [];
        internal List<ServerPremiumRewards> Packets => sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<ServerPremiumRewards>().ToList();
        internal Fixture(bool signature = false, bool earned = false)
        {
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out TableProxy);
            TableProxy.SetProperty(nameof(IGameTableManager.RewardProperty), PlayerBindPointTests.Table(PropertyRows));
            TableProxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(Tracks));
            TableProxy.SetProperty(nameof(IGameTableManager.Spell4Effects), PlayerBindPointTests.Table(Rows));
            Account = RecordingDispatchProxy<IAccount>.Create(out AccountProxy); Session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
            AccountProxy.SetProperty(nameof(IAccount.Session), Session);
            var entitlements = RecordingDispatchProxy<IAccountEntitlementManager>.Create(out var ep);
            if (earned) ep.SetMethodReturn(nameof(IAccountEntitlementManager.GetEntitlement), new AccountEntitlement(Account,
                new AccountEntitlementModel { Id = 1, EntitlementId = 44, Amount = 1 }, new EntitlementEntry { Id = 44, MaxCount = 1 }));
            AccountProxy.SetProperty(nameof(IAccount.EntitlementManager), entitlements);
            if (signature) Premium.Add(new() { Id = 223, PremiumSystemEnum = 1, Tier = 1, RewardPropertyId = 4, RewardPropertyData = 3, ModifierValueFloat = 0.25f });
            var assets = RecordingDispatchProxy<IAssetManager>.Create(out var ap); ap.SetMethodReturnFactory(nameof(IAssetManager.GetRewardPropertiesForTier), () => Premium.ToImmutableList());
            Properties = new(Account, assets, Tables); AccountProxy.SetProperty(nameof(IAccount.RewardPropertyManager), Properties); BindReplacement();
        }
        internal void BindReplacement()
        {
            Player = RecordingDispatchProxy<IPlayer>.Create(out var pp); pp.SetProperty(nameof(IPlayer.Account), Account); pp.SetProperty(nameof(IPlayer.Session), Session);
            pp.SetProperty(nameof(IPlayer.CharacterId), 42ul); Properties.Initialise(Player);
        }
        internal void Apply(Spell4EffectsEntry effect, uint token, bool removed = false)
            => Properties.UpdateContractSpellModifier(Player, token, effect.SpellId, effect.Id, effect.DataBits01, 0.5f, removed);
        internal ContractRewardModifierSnapshot Capture(uint type)
        { Assert.True(Properties.CaptureContractModifier(Player, type == 0 ? 4u : 5u, type, out var result)); return result; }
        internal ChallengeRewardModifierSnapshot Challenge() { Assert.True(Properties.CaptureChallengeModifier(out var result)); return result; }
        internal ServerPremiumRewards.RewardProperty Row(uint type) => Properties.GetRewardProperty(RewardPropertyType.RewardTrackType).Build().Single(row => row.Data == type);
    }
}
