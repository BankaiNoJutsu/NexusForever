using System.Collections.Immutable;
using System.Text.Json;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static;
using NexusForever.Game.Static.RBAC;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Command.Handler;

namespace NexusForever.Game.Tests.Spell;

public class ProcRuntimeEvidenceCollectorTests
{
    [Fact]
    public void ExportReport_IncludesUnsupportedTailSummaryAndRecentObservations()
    {
        using var output = new ProcEvidenceDirectoryScope();

        const uint holderGuid = 9101u;
        IUnitEntity holder = CreateHolder(
            holderGuid,
            new ProcRegistrationSnapshot(11u, 4046u, 77u, 99u, 4047u, 0.5f, 36u, 0u, 0d, 5u, 6u, 7u, 8u, 9u),
            new ProcRegistrationSnapshot(12u, 4876u, 88u, ProcTriggerEventCandidate.EnterCombat, 6000u, 0.25f, 1u, 0u, 0d, 0u, 0u, 0u, 0u, 0u));

        ProcRuntimeEvidenceCollector.RecordProcRegistration(
            holderGuid,
            4046u,
            77u,
            new SpellEffectProcSemantics(99u, 4047u, 0.5f, 36u, 0u, 5u, 6u, 7u, 8u, 9u),
            true,
            false,
            null);
        ProcRuntimeEvidenceCollector.RecordProcProbe(
            holderGuid,
            "damage-dealt",
            "after-calculate-before-apply",
            ProcTriggerEventCandidate.DealDamage,
            100u,
            200u,
            300u,
            400u,
            500u,
            600u,
            550u,
            25u,
            0u,
            50u,
            false,
            "Hit",
            11u,
            4046u,
            77u,
            99u,
            4047u,
            0.5f,
            36u,
            0u,
            5u,
            6u,
            7u,
            8u,
            9u);
        ProcRuntimeEvidenceCollector.RecordProcDispatch(
            holderGuid,
            "damage-dealt",
            "after-calculate-before-apply",
            ProcTriggerEventCandidate.DealDamage,
            100u,
            200u,
            0u,
            11u,
            4046u,
            77u,
            99u,
            4047u,
            0.5f,
            36u,
            0u,
            0d,
            "none",
            "unsupported-target-data");

        string outputPath = ProcRuntimeEvidenceCollector.ExportReport(holder, "manual-command", "test export");

        Assert.False(string.IsNullOrWhiteSpace(outputPath));
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(outputPath));
        JsonElement root = document.RootElement;

        Assert.Equal(holderGuid, root.GetProperty("HolderGuid").GetUInt32());
        Assert.Equal(2, root.GetProperty("ActiveRegistrationCount").GetInt32());
        Assert.Equal(1, root.GetProperty("UnsupportedRegistrationCount").GetInt32());
        Assert.Equal(3, root.GetProperty("RecentObservationCount").GetInt32());
        Assert.Equal(3, root.GetProperty("RecentUnsupportedObservationCount").GetInt32());

        JsonElement unsupportedTargetData = Assert.Single(root.GetProperty("UnsupportedActiveTargetData").EnumerateArray().ToArray());
        Assert.Equal(36u, unsupportedTargetData.GetProperty("Value").GetUInt32());

        JsonElement recentObservations = root.GetProperty("RecentObservations");
        Assert.Equal(3, recentObservations.GetArrayLength());
        Assert.Contains(recentObservations.EnumerateArray().Select(element => element.GetProperty("Kind").GetString()), kind => kind == "dispatch");
    }

    [Fact]
    public void HandleSpellProcReportAndUnsupported_ExportAndSummarizeUnsupportedTails()
    {
        using var output = new ProcEvidenceDirectoryScope();

        const uint holderGuid = 9102u;
        IUnitEntity holder = CreateHolder(
            holderGuid,
            new ProcRegistrationSnapshot(21u, 7116u, 91u, 55u, 7117u, 1f, 36u, 0u, 0d, 0u, 0u, 0u, 0u, 0u));

        ProcRuntimeEvidenceCollector.RecordProcDispatch(
            holderGuid,
            "target-killed",
            "after-apply",
            ProcTriggerEventCandidate.KillTarget,
            1000u,
            2000u,
            0u,
            21u,
            7116u,
            91u,
            55u,
            7117u,
            1f,
            36u,
            0u,
            0d,
            "none",
            "unsupported-target-data");

        var category = new SpellCommandCategory(
            RecordingDispatchProxy<IGlobalSpellManager>.Create(out _),
            RecordingDispatchProxy<IGameTableManager>.Create(out _));
        var context = new TestCommandContext(holder, null, [Permission.Spell]);

        category.HandleSpellProcUnsupported(context);
        category.HandleSpellProcReport(context);

        Assert.Equal(2, context.Messages.Count);
        Assert.Contains("unsupported proc evidence", context.Messages[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("36", context.Messages[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unsupported-target-data", context.Messages[0], StringComparison.OrdinalIgnoreCase);

        string artifactPath = Assert.Single(Directory.GetFiles(output.DirectoryPath, "*.json"));
        Assert.Contains("Proc evidence report exported", context.Messages[1], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(artifactPath, context.Messages[1], StringComparison.OrdinalIgnoreCase);
    }

    private static IUnitEntity CreateHolder(uint holderGuid, params ProcRegistrationSnapshot[] registrations)
    {
        IUnitEntity holder = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.Guid), holderGuid);
        proxy.SetMethodReturn(nameof(IUnitEntity.CreateProcRegistrationSnapshot), registrations);
        return holder;
    }

    private sealed class ProcEvidenceDirectoryScope : IDisposable
    {
        private const string EnvironmentVariableName = "NEXUSFOREVER_PROC_EVIDENCE_DIR";
        private static readonly object syncRoot = new();

        public string DirectoryPath { get; }

        public ProcEvidenceDirectoryScope()
        {
            Monitor.Enter(syncRoot);
            DirectoryPath = Path.Combine(AppContext.BaseDirectory, "proc-evidence-tests", Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable(EnvironmentVariableName, DirectoryPath);
        }

        public void Dispose()
        {
            try
            {
                Environment.SetEnvironmentVariable(EnvironmentVariableName, null);
                if (Directory.Exists(DirectoryPath))
                    Directory.Delete(DirectoryPath, recursive: true);
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }
    }

    private sealed class TestCommandContext(
        IWorldEntity invoker,
        IWorldEntity target,
        ImmutableHashSet<Permission> permissions) : ICommandContext
    {
        public List<string> Messages { get; } = [];
        public List<string> Errors { get; } = [];

        public IWorldEntity Invoker { get; } = invoker;
        public IWorldEntity Target { get; } = target;
        public Language Language => Language.English;
        public ImmutableHashSet<Permission> Permissions { get; } = permissions;

        public void SendMessage(string message)
        {
            Messages.Add(message);
        }

        public void SendError(string message)
        {
            Errors.Add(message);
        }

        public T GetTargetOrInvoker<T>() where T : IWorldEntity
        {
            if (Target is T targetEntity)
                return targetEntity;

            return (T)Invoker;
        }
    }
}
