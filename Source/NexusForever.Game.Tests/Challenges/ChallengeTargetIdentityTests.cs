using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using static NexusForever.Game.Tests.Challenges.ChallengeRewardPersistenceOwnershipTests;

namespace NexusForever.Game.Tests.Challenges;

public sealed class ChallengeTargetIdentityTests
{
    [Theory]
    [InlineData(ChallengeType.Combat, false)] [InlineData(ChallengeType.Combat, true)]
    [InlineData(ChallengeType.ChecklistActivate, false)] [InlineData(ChallengeType.ChecklistActivate, true)]
    public void CreatureProgressCannotUseAGroupIdentityOrAnEmptySlot(ChallengeType type, bool zero)
    {
        var definition = new ChallengeEntry { Id = 1001, ChallengeTypeEnum = (uint)type, Target = 7000, ChallengeTierId00 = 1 };
        using var f = new Fixture(definition);
        f.Manager.HandleChoice(1001, ChallengeChoice.Activate);
        f.Emit(zero ? 0u : 7000u);
        Assert.Equal(0u, Runtime(f.Manager, 1001).CurrentCount);
        f.Emit(42);
        Assert.Equal(1u, Runtime(f.Manager, 1001).CurrentCount);
    }

    [Theory]
    [InlineData(ChallengeType.Combat)] [InlineData(ChallengeType.ChecklistActivate)]
    public void ExistingDirectTargetsWithoutAGroupKeepTheirRoute(ChallengeType type)
    {
        using var f = new Fixture(new ChallengeEntry { Id = 1001, ChallengeTypeEnum = (uint)type, Target = 42, ChallengeTierId00 = 1 });
        f.Manager.HandleChoice(1001, ChallengeChoice.Activate); f.Emit(42);
        Assert.Equal(1u, Runtime(f.Manager, 1001).CurrentCount);
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
                .AddGameTable<TargetGroupEntry>().AddGameTable<Creature2Entry>().AddGameTable<WorldZoneEntry>().AddGameTable<GameFormulaEntry>());
            Assert.Equal(643, tables.Challenge.Entries.Length);
            File.WriteAllText(Path.Combine(outputDirectory, "inputs.json"), JsonSerializer.Serialize(new
            {
                ManifestSha256 = Hash(manifest), Files = files,
                ReaderSha256 = Hash(File.ReadAllBytes(typeof(GameTable<>).Assembly.Location)),
                RuntimeSha256 = Hash(File.ReadAllBytes(typeof(ChallengeManager).Assembly.Location)),
                ProbeSha256 = Hash(File.ReadAllBytes(typeof(ChallengeTargetIdentityTests).Assembly.Location))
            }, json));
            var results = new List<object>(); int failures = 0;
            foreach (var source in new[] { (Id: (ushort)103, Good: 11576u), (Id: (ushort)532, Good: 34191u) })
            foreach (bool zero in new[] { false, true })
            {
                var definition = tables.Challenge.GetEntry(source.Id);
                Assert.NotNull(tables.TargetGroup.GetEntry(definition.Target));
                Assert.NotNull(tables.Creature2.GetEntry(definition.Target));
                Assert.NotNull(tables.Creature2.GetEntry(source.Good));
                using var f = new Fixture(definition, tables);
                f.Manager.HandleChoice(source.Id, ChallengeChoice.Activate);
                f.Emit(zero ? 0u : definition.Target);
                uint badCount = Runtime(f.Manager, source.Id).CurrentCount;
                f.Emit(source.Good);
                var after = Runtime(f.Manager, source.Id);
                bool passed = badCount == 0 && after.CurrentCount == 1 && after.CurrentTier == 0 && after.CompletionCount == 0;
                if (!passed) ++failures;
                results.Add(new { source.Id, InvalidCreature = zero ? 0u : definition.Target, BadCount = badCount,
                    GoodCreature = source.Good, after.CurrentCount, after.CurrentTier, after.CompletionCount, Passed = passed });
            }
            using (var f = new Fixture(tables.Challenge.GetEntry(103), tables))
            {
                f.Emit(8851);
                bool wrongActivation = f.Manager.IsChallengeActivated(103);
                f.Emit(11576);
                bool passed = !wrongActivation && f.Manager.IsChallengeActivated(103) && Runtime(f.Manager, 103).CurrentCount == 1;
                if (!passed) ++failures;
                results.Add(new { Id = 103, AutoActivation = true, WrongActivation = wrongActivation,
                    Runtime(f.Manager, 103).CurrentCount, Passed = passed });
            }
            File.WriteAllText(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(new
            {
                Cases = results, Failures = failures,
                Limit = "Selected actual definitions and full group/tier tables through real Combat/Activation hooks and ChallengeManager; prepared proxy Player/map/session. Legacy count path; no durable medal/claim/provider/socket/native/retail qualification or new unsupported producer semantics."
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
        private readonly ChallengeEntry definition;

        internal Fixture(ChallengeEntry definition, IGameTableManager actual = null)
        {
            this.definition = definition;
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var data);
            data.SetProperty(nameof(IGameTableManager.Challenge), PlayerBindPointTests.Table(definition));
            data.SetProperty(nameof(IGameTableManager.ChallengeTier), actual?.ChallengeTier ?? PlayerBindPointTests.Table(new ChallengeTierEntry { Id = 1, Count = 5 }));
            data.SetProperty(nameof(IGameTableManager.TargetGroup), actual?.TargetGroup ?? PlayerBindPointTests.Table(
                new TargetGroupEntry { Id = 7000, Type = (uint)TargetGroupType.OtherTargetGroupCreatures, DataEntries = [7001, 0] },
                new TargetGroupEntry { Id = 7001, Type = (uint)TargetGroupType.CreatureIdListGroup, DataEntries = [42, 0] }));
            data.SetProperty(nameof(IGameTableManager.WorldZone), actual?.WorldZone);
            Player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
            var session = RecordingDispatchProxy<IGameSession>.Create(out _);
            actor.SetProperty(nameof(IPlayer.CharacterId), 42ul); actor.SetProperty(nameof(IPlayer.Guid), 9001u);
            actor.SetProperty(nameof(IPlayer.Session), session);
            actor.SetProperty(nameof(IPlayer.Inventory), RecordingDispatchProxy<IInventory>.Create(out _));
            actor.SetProperty(nameof(IPlayer.QuestManager), RecordingDispatchProxy<IQuestManager>.Create(out _));
            uint zone = definition.WorldZoneIdRestriction != 0 ? definition.WorldZoneIdRestriction : definition.WorldZoneId;
            actor.SetProperty(nameof(IPlayer.Zone), actual?.WorldZone.GetEntry(zone) ?? new WorldZoneEntry { Id = zone });
            var map = RecordingDispatchProxy<IBaseMap>.Create(out var world);
            world.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 1 }); actor.SetProperty(nameof(IPlayer.Map), map);
            Manager = new ChallengeManager(Player, tables); actor.SetProperty(nameof(IPlayer.ChallengeManager), Manager);
        }
        internal void Emit(uint creature)
        {
            if (definition.ChallengeTypeEnum == (uint)ChallengeType.Combat) ChallengeCombatHooks.OnCreatureKilled(Player, creature);
            else ChallengeActivationHooks.OnTargetActivated(Player, creature);
        }
        public void Dispose() => Manager.InvalidateRewardOperations(true);
    }
}

public static class ChallengeTargetIdentityActualQualification
{
    public static Task RunAsync(string recoveryRoot, string outputDirectory)
        => ChallengeTargetIdentityTests.RunActualCatalogAsync(recoveryRoot, outputDirectory);
}
