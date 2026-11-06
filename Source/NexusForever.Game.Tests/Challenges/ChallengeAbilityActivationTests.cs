using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Game.Challenges;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Challenges;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity;
using static NexusForever.Game.Tests.Challenges.ChallengeRewardPersistenceOwnershipTests;

namespace NexusForever.Game.Tests.Challenges;

public sealed class ChallengeAbilityActivationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InteractionCannotInterpretAnAbilitySpellAsACreatureOrGroup(bool groupAlias)
    {
        var definition = new ChallengeEntry { Id = 1001, ChallengeTypeEnum = (uint)ChallengeType.Ability,
            Target = groupAlias ? 7000u : 42u, ChallengeTierId00 = 1 };
        using var f = new Fixture(definition);
        f.Manager.HandleChoice(1001, ChallengeChoice.Activate);
        f.Interact(42);
        Assert.Equal(0u, Runtime(f.Manager, 1001).CurrentCount);
        Assert.True(ChallengeProgressHooks.OnChallengeProgress(f.Player, 1001));
        Assert.Equal(1u, Runtime(f.Manager, 1001).CurrentCount);
    }

    [Fact]
    public void RewardPaneMembershipDoesNotSubstituteForAbilityUse()
    {
        using var f = new Fixture(new ChallengeEntry { Id = 1001, ChallengeTypeEnum = (uint)ChallengeType.Ability,
            Target = 8000, TargetGroupIdRewardPane = 7000, ChallengeTierId00 = 1 });
        f.Manager.HandleChoice(1001, ChallengeChoice.Activate);
        f.Interact(42);
        Assert.Equal(0u, Runtime(f.Manager, 1001).CurrentCount);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InteractionStillCreditsChecklistCreatureAndGroupTargets(bool group)
    {
        using var f = new Fixture(new ChallengeEntry { Id = 1001, ChallengeTypeEnum = (uint)ChallengeType.ChecklistActivate,
            Target = group ? 7000u : 42u, ChallengeTierId00 = 1 });
        f.Manager.HandleChoice(1001, ChallengeChoice.Activate);
        f.Interact(42);
        Assert.Equal(1u, Runtime(f.Manager, 1001).CurrentCount);
    }

    [Fact]
    public void InteractionDoesNotStartAnInactiveAbilityChallenge()
    {
        using var f = new Fixture(new ChallengeEntry { Id = 1001, ChallengeTypeEnum = (uint)ChallengeType.Ability,
            Target = 42, ChallengeFlags = 0x20, ChallengeTierId00 = 1 });
        f.Interact(42);
        Assert.False(f.Manager.IsChallengeActivated(1001));
    }

    internal static async Task RunActualCatalogAsync(string recoveryRoot, string outputDirectory)
    {
        recoveryRoot = Path.GetFullPath(recoveryRoot); outputDirectory = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(outputDirectory)) throw new IOException("Retain prior evidence; choose a new output directory.");
        Directory.CreateDirectory(outputDirectory);
        var json = new JsonSerializerOptions { WriteIndented = true };
        try
        {
            var (manifest, files) = Quest.ContractActualCatalogQualification.VerifyInputs(recoveryRoot);
            var tables = new GameTableManager(Options.Create(new GameTableConfig { GameTablePath = Path.Combine(recoveryRoot, "recovered-tables") }));
            await tables.Initialise(new GameTableLoader().AddGameTable<ChallengeEntry>().AddGameTable<ChallengeTierEntry>()
                .AddGameTable<TargetGroupEntry>().AddGameTable<Creature2Entry>().AddGameTable<WorldZoneEntry>()
                .AddGameTable<Spell4Entry>().AddGameTable<Spell4BaseEntry>().AddGameTable<Spell4EffectsEntry>());
            var definition = tables.Challenge.GetEntry(182);
            Assert.Equal((uint)ChallengeType.Ability, definition.ChallengeTypeEnum);
            Assert.Equal(28780u, definition.Target); Assert.Null(tables.TargetGroup.GetEntry(definition.Target));
            Assert.Equal(15260u, tables.Spell4.GetEntry(definition.Target).Spell4BaseIdBaseSpell);
            Assert.Equal(5913u, definition.TargetGroupIdRewardPane);
            Assert.Contains(20775u, tables.TargetGroup.GetEntry(5913).DataEntries);
            Assert.Equal(8u, tables.ChallengeTier.GetEntry(definition.ChallengeTierId00).Count);
            var abilities = tables.Challenge.Entries.Where(row => row.ChallengeTypeEnum == (uint)ChallengeType.Ability).ToArray();
            Assert.Equal(47, abilities.Length);
            Assert.Equal(45, abilities.Count(row => row.Target != 0 && tables.Spell4.GetEntry(row.Target) != null));
            Assert.Equal(2, abilities.Count(row => row.Target == 0));
            Assert.All(abilities, row => Assert.Null(tables.TargetGroup.GetEntry(row.Target)));
            File.WriteAllText(Path.Combine(outputDirectory, "inputs.json"), JsonSerializer.Serialize(new
            {
                ManifestSha256 = Hash(manifest), Files = files,
                ReaderSha256 = Hash(File.ReadAllBytes(typeof(GameTable<>).Assembly.Location)),
                RuntimeSha256 = Hash(File.ReadAllBytes(typeof(ChallengeManager).Assembly.Location)),
                HandlerSha256 = Hash(File.ReadAllBytes(typeof(ClientActivateUnitHandler).Assembly.Location)),
                ProbeSha256 = Hash(File.ReadAllBytes(typeof(ChallengeAbilityActivationTests).Assembly.Location))
            }, json));
            var results = new List<object>(); int failures = 0;
            foreach (var source in new[] { (Creature: 28780u, Repeats: 1), (Creature: 28780u, Repeats: 8), (Creature: 20775u, Repeats: 1) })
            {
                Assert.NotNull(tables.Creature2.GetEntry(source.Creature));
                using var f = new Fixture(definition, tables);
                f.Manager.HandleChoice(182, ChallengeChoice.Activate);
                Assert.True(f.Manager.IsChallengeActivated(182));
                for (int i = 0; i < source.Repeats; ++i) f.Interact(source.Creature);
                var state = Runtime(f.Manager, 182);
                bool passed = state.CurrentCount == 0 && state.CurrentTier == 0 && state.CompletionCount == 0 && state.Activated
                    && !f.Results.Any(row => row.Result is ChallengeResult.TierAchieved or ChallengeResult.Completed);
                if (!passed) ++failures;
                results.Add(new { Id = 182, source.Creature, source.Repeats, state.CurrentCount, state.CurrentTier, state.CompletionCount, Passed = passed });
            }
            using (var f = new Fixture(tables.Challenge.GetEntry(309), tables))
            {
                f.Manager.HandleChoice(309, ChallengeChoice.Activate); f.Interact(25640);
                var state = Runtime(f.Manager, 309);
                bool passed = state.Activated && state.CurrentCount == 1 && state.CurrentTier == 0 && state.CompletionCount == 0;
                if (!passed) ++failures;
                results.Add(new { Id = 309, Creature = 25640, state.CurrentCount, state.CurrentTier, state.CompletionCount, Passed = passed });
            }
            File.WriteAllText(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(new
            {
                Cases = results, Failures = failures, AbilityRows = abilities.Length, SpellTargets = 45, ZeroTargets = new[] { 2, 1117 },
                Limit = "Parsed ClientActivateUnit and real interaction updater/ChallengeManager with actual definitions, full tier/group/creature tables and proxy visible entity/Player/session. No positive Ability spell producer, physical placement, durable award, provider, socket, native or retail qualification."
            }, json));
            Assert.Equal(0, failures);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(outputDirectory, "failure.json"), JsonSerializer.Serialize(new { Error = error.ToString() }, json));
            throw;
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private sealed class Fixture : IDisposable
    {
        internal readonly IPlayer Player;
        internal readonly ChallengeManager Manager;
        private readonly IWorldSession session;
        private readonly RecordingDispatchProxy<IWorldSession> sessionProxy;
        private readonly RecordingDispatchProxy<IPlayer> actor;
        private readonly IGameTableManager tables;
        internal IEnumerable<ServerChallengeResult> Results => sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(row => row.Arguments[0]).OfType<ServerChallengeResult>();

        internal Fixture(ChallengeEntry definition, IGameTableManager actual = null)
        {
            tables = RecordingDispatchProxy<IGameTableManager>.Create(out var data);
            data.SetProperty(nameof(IGameTableManager.Challenge), PlayerBindPointTests.Table(definition));
            data.SetProperty(nameof(IGameTableManager.ChallengeTier), actual?.ChallengeTier ?? PlayerBindPointTests.Table(new ChallengeTierEntry { Id = 1, Count = 5 }));
            data.SetProperty(nameof(IGameTableManager.TargetGroup), actual?.TargetGroup ?? PlayerBindPointTests.Table(
                new TargetGroupEntry { Id = 7000, Type = (uint)TargetGroupType.CreatureIdGroup, DataEntries = [42, 0] }));
            data.SetProperty(nameof(IGameTableManager.Creature2), actual?.Creature2);
            data.SetProperty(nameof(IGameTableManager.WorldZone), actual?.WorldZone);
            Player = RecordingDispatchProxy<IPlayer>.Create(out actor);
            session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
            sessionProxy.SetProperty(nameof(IWorldSession.Player), Player);
            actor.SetProperty(nameof(IPlayer.CharacterId), 42ul); actor.SetProperty(nameof(IPlayer.Guid), 9001u);
            actor.SetProperty(nameof(IPlayer.Session), session);
            actor.SetProperty(nameof(IPlayer.Inventory), RecordingDispatchProxy<IInventory>.Create(out _));
            actor.SetProperty(nameof(IPlayer.QuestManager), RecordingDispatchProxy<IQuestManager>.Create(out _));
            actor.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
            uint zone = definition.WorldZoneIdRestriction != 0 ? definition.WorldZoneIdRestriction : definition.WorldZoneId;
            actor.SetProperty(nameof(IPlayer.Zone), actual?.WorldZone.GetEntry(zone) ?? new WorldZoneEntry { Id = zone });
            Manager = new ChallengeManager(Player, tables); actor.SetProperty(nameof(IPlayer.ChallengeManager), Manager);
        }

        internal void Interact(uint creature)
        {
            var entity = RecordingDispatchProxy<IWorldEntity>.Create(out var target);
            target.SetProperty(nameof(IWorldEntity.Guid), 77u);
            target.SetProperty(nameof(IWorldEntity.CreatureId), creature);
            // The visible, nonhostile interactable and its geometry are prepared.
            // Actual Creature2 still resolves above; its placement is not asserted.
            target.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { Id = creature });
            actor.SetMethodReturn("GetVisible", entity);
            using var bytes = new MemoryStream();
            using (var writer = new GamePacketWriter(bytes)) { writer.Write(77u); writer.FlushBits(); }
            using var reader = new GamePacketReader(new MemoryStream(bytes.ToArray()));
            var packet = new ClientActivateUnit(); packet.Read(reader);
            new ClientActivateUnitHandler(RecordingDispatchProxy<ITradeManager>.Create(out _),
                RecordingDispatchProxy<IAssetManager>.Create(out _), tables).HandleMessage(session, packet);
            Assert.Single(target.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
            Assert.Empty(target.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
        }

        public void Dispose() => Manager.InvalidateRewardOperations(true);
    }
}

// Explicit offline qualification; shipped payloads remain outside Git and xUnit discovery.
public static class ChallengeAbilityActivationActualQualification
{
    public static Task RunAsync(string recoveryRoot, string outputDirectory)
        => ChallengeAbilityActivationTests.RunActualCatalogAsync(recoveryRoot, outputDirectory);
}
