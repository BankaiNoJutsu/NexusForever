using Microsoft.Extensions.Options;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;

namespace NexusForever.Game.Tests.TableContracts;

[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class MissingGameDataDiagnosticsTests
{
    [Fact]
    public void ValidateRequiredTables_WithUnloadedTablesReportsMissingTables()
    {
        MissingGameDataDiagnostics.ResetForTests();
        try
        {
            var manager = new GameTableManager(Options.Create(new GameTableConfig()));

            GameTableValidationReport report = manager.ValidateRequiredTables();

            Assert.True(report.HasMissingRequiredTables);
            Assert.Contains("Achievement.tbl", report.MissingRequiredTables);
            Assert.Contains("Item2.tbl", report.MissingRequiredTables);

            IReadOnlyList<MissingGameDataDiagnostic> snapshot = MissingGameDataDiagnostics.GetSnapshot();
            Assert.Contains(snapshot, d =>
                d.Kind == MissingGameDataDiagnosticKind.MissingTable
                && d.Severity == MissingGameDataSeverity.Required
                && d.TableName == "Achievement.tbl"
                && d.Context == "GameTableManager.ValidateRequiredTables"
                && d.Count == 1);
        }
        finally
        {
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void ValidateRequiredTables_WithStrictModeThrows()
    {
        MissingGameDataDiagnostics.ResetForTests();
        try
        {
            var manager = new GameTableManager(Options.Create(new GameTableConfig()));

            GameTableException exception = Assert.Throws<GameTableException>(() => manager.ValidateRequiredTables(true));

            Assert.Contains("Achievement.tbl", exception.Message);
            Assert.Contains("Item2.tbl", exception.Message);
        }
        finally
        {
            MissingGameDataDiagnostics.ResetForTests();
        }
    }

    [Fact]
    public void MissingGameDataDiagnostics_WithRepeatedReportsAggregatesCounts()
    {
        MissingGameDataDiagnostics.ResetForTests();
        try
        {
            const string context = "missing-data-diagnostics-test-context";

            MissingGameDataDiagnostics.ReportMissingRow(
                "Spell4.tbl",
                44u,
                context,
                MissingGameDataSeverity.PlayerImpacting,
                "missing mount unlock spell");
            MissingGameDataDiagnostics.ReportMissingRow(
                "Spell4.tbl",
                44u,
                context,
                MissingGameDataSeverity.PlayerImpacting,
                "missing mount unlock spell");

            MissingGameDataDiagnostic diagnostic = Assert.Single(MissingGameDataDiagnostics.GetSnapshot(), d =>
                d.Context == context
                && d.TableName == "Spell4.tbl"
                && d.StaticId == "44");
            Assert.Equal(MissingGameDataDiagnosticKind.MissingRow, diagnostic.Kind);
            Assert.Equal(MissingGameDataSeverity.PlayerImpacting, diagnostic.Severity);
            Assert.Equal(2, diagnostic.Count);
        }
        finally
        {
            MissingGameDataDiagnostics.ResetForTests();
        }
    }
}
