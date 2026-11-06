using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

// Explicit offline entry point; shipped table bodies stay outside Git/xUnit discovery.
public static class QuestAccountRewardActualQualification
{
    public static async Task RunAsync(string assetRoot, string outputDirectory)
    {
        assetRoot = Path.GetFullPath(assetRoot); outputDirectory = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(outputDirectory)) throw new IOException("Preserve prior qualification; choose a fresh output directory.");
        Directory.CreateDirectory(outputDirectory);
        var pins = new Dictionary<string, string>
        {
            ["Quest2Reward.tbl"] = "52B60741C14E79B6516F39BC0EFD6BEF434E16E885E24E88CD14C959A4BF13E7",
            ["AccountCurrencyType.tbl"] = "51B1AA86D32EDD02CCFF95CE81DECB93396239516CCEE738E0FAA5DE26624417"
        };
        foreach (var (name, hash) in pins)
            Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(assetRoot, "tbl", name)))));
        var tables = new GameTableManager(Options.Create(new GameTableConfig { GameTablePath = Path.Combine(assetRoot, "tbl") }));
        await tables.Initialise(new GameTableLoader().AddGameTable<Quest2RewardEntry>().AddGameTable<AccountCurrencyTypeEntry>());
        var rewardRows = new uint[] { 9494, 9849, 9850, 9851 }.Select(id => tables.Quest2Reward.GetEntry(id)).ToArray();
        foreach (var row in rewardRows)
        {
            Assert.NotNull(row); Assert.Equal(7u, row.Quest2RewardTypeId); Assert.Equal(0u, row.Flags);
            Assert.NotNull(tables.AccountCurrencyType.GetEntry(row.ObjectId));
        }
        Assert.Equal(10525u, rewardRows[0].Quest2Id); Assert.Equal(6u, rewardRows[0].ObjectId); Assert.Equal(1u, rewardRows[0].ObjectAmount);
        Assert.All(rewardRows.Skip(1), row => { Assert.Equal(10600u, row.Quest2Id); Assert.Equal(100u, row.ObjectAmount); });
        Assert.Equal(new uint[] { 15, 16, 17 }, rewardRows.Skip(1).Select(row => row.ObjectId));
        var results = new List<object>();
        foreach (var rows in rewardRows.GroupBy(row => row.Quest2Id))
            foreach (string variant in new[] { "ordinary", "direct" })
            foreach (string outcome in new[] { "commit", "rollback", "auth-refusal", "replacement" })
            {
                await QuestAccountRewardSettlementTests.Run(outcome, tables, rows.ToArray(), variant);
                results.Add(new { Quest = rows.Key, Variant = variant, Outcome = outcome, Result = "Passed" });
            }
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(new
        {
            Pins = pins, Rows = rewardRows.Select(row => new { row.Id, row.Quest2Id, row.Quest2RewardTypeId, row.ObjectId, row.ObjectAmount, row.Flags }),
            Results = results,
            Boundary = "Actual account-currency reward rows and currency definitions through parsed public turn-in or concrete direct completion and real Auth/Character settlement. Quest eligibility, objectives, remaining rewards and world/socket/native flows are fixture adapters."
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
