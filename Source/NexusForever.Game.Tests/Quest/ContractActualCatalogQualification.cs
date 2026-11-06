using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest.Reward;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

// Explicit offline qualification entrypoint; no shipped data is committed or
// substituted with synthetic ItemInfo. Invoke from the retained audit runner.
public static class ContractActualCatalogQualification
{
    public static async Task RunAsync(string recoveryRoot, string outputDirectory)
    {
        recoveryRoot = Path.GetFullPath(recoveryRoot);
        outputDirectory = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(outputDirectory)) throw new IOException("Choose a new output directory to preserve prior evidence.");
        Directory.CreateDirectory(outputDirectory);
        var json = new JsonSerializerOptions { WriteIndented = true };
        string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
        byte[] manifestBytes = File.ReadAllBytes(Path.Combine(recoveryRoot, "recovered-payloads.json"));
        using var manifest = JsonDocument.Parse(manifestBytes);
        Assert.True(manifest.RootElement.GetProperty("complete").GetBoolean());
        var inputs = new List<object>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement file in manifest.RootElement.GetProperty("files").EnumerateArray())
        {
            string relative = file.GetProperty("recoveredFile").GetString();
            string path = Path.GetFullPath(Path.Combine(recoveryRoot, relative));
            Assert.StartsWith(recoveryRoot + Path.DirectorySeparatorChar, path, StringComparison.OrdinalIgnoreCase);
            Assert.True(seen.Add(path));
            byte[] bytes = File.ReadAllBytes(path);
            Assert.Equal(file.GetProperty("sha256").GetString(), Hash(bytes));
            Assert.Equal(file.GetProperty("uncompressedSize").GetInt64(), bytes.LongLength);
            inputs.Add(new { File = relative, Sha256 = Hash(bytes), Bytes = bytes.Length });
        }
        Assert.Equal(385, seen.Count);
        var tables = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = Path.Combine(recoveryRoot, "recovered-tables")
        }));
        await tables.Initialise(new GameTableLoader()
            .AddGameTable<RewardTrackEntry>().AddGameTable<RewardTrackRewardsEntry>()
            .AddGameTable<Item2Entry>().AddGameTable<AccountItemEntry>()
            .AddGameTable<CurrencyTypeEntry>().AddGameTable<AccountCurrencyTypeEntry>()
            .AddGameTable<Item2FamilyEntry>().AddGameTable<Item2CategoryEntry>().AddGameTable<Item2TypeEntry>()
            .AddGameTable<ItemSlotEntry>().AddGameTable<ItemBudgetEntry>().AddGameTable<ItemStatEntry>()
            .AddGameTable<ItemQualityEntry>().AddGameTable<ItemRandomStatEntry>()
            .AddGameTable<GameFormulaEntry>().AddGameTable<UnitProperty2Entry>());
        var catalog = new ContractRewardAwardCatalog(tables);
        Assert.Equal(726, catalog.Awards.Count);
        Assert.Equal(486, catalog.Awards.Count(award => award.Item != null));
        Assert.Equal(240, catalog.Awards.Count(award => award.AccountCurrency != null));
        Assert.Equal(6, catalog.Awards.Count(award => award.CharacterCurrency != null));
        var results = new List<object>();
        var started = DateTime.UtcNow;
        File.WriteAllText(Path.Combine(outputDirectory, "inputs.json"), JsonSerializer.Serialize(new
        {
            ManifestSha256 = Hash(manifestBytes), Files = inputs,
            ReaderSha256 = Hash(File.ReadAllBytes(typeof(GameTable<>).Assembly.Location)),
            RuntimeSha256 = Hash(File.ReadAllBytes(typeof(ItemInfo).Assembly.Location)),
            ProbeSha256 = Hash(File.ReadAllBytes(typeof(ContractActualCatalogQualification).Assembly.Location))
        }, json));
        foreach (ContractRewardAward award in catalog.Awards)
        {
            try
            {
                using var f = new ContractClaimTransactionTests.Fixture(null, null, award.RewardRowId, award.Choice, tables);
                using var held = f.Reserve();
                var result = await f.Execute(held);
                Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, result.Outcome);
                Assert.True(Assert.IsType<ContractClaimStage>(result.Operation).Applied);
                if (held != null)
                {
                    Assert.IsType<ItemInfo>(f.Inventory.Info);
                    Assert.True(award.Item.ForceSoulbind);
                    Assert.True(held.PublishCommitted());
                    Assert.False(held.PublishCommitted());
                    Assert.Equal((ulong)award.Item.Count, f.Inventory.Bag.Aggregate(0ul, (sum, item) => sum + item.StackCount));
                }
                else
                {
                    Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await f.Sender().DeliverAsync(f.Command.Identity)).Outcome);
                    Assert.Equal(ContractAccountDeliveryOutcome.Delivered, (await f.Sender().DeliverAsync(f.Command.Identity)).Outcome);
                    using var auth = f.Auth();
                    Assert.Equal(100ul + award.AccountCurrency.Amount, Assert.Single(auth.AccountCurrency).Amount);
                    Assert.Single(auth.AccountCurrencyOperation);
                }
                var replay = await f.Execute(held);
                Assert.Equal(CharacterChallengeRewardExecutionOutcome.Committed, replay.Outcome);
                Assert.False(Assert.IsType<ContractClaimStage>(replay.Operation).Applied);
                using var read = f.Character();
                var receipt = Assert.Single(read.CharacterContractRewardClaim);
                Assert.Equal(f.Quote.Json, receipt.QuoteJson);
                Assert.Empty(read.CharacterChallengeRewardClaim);
                Assert.Empty(read.CharacterChallengeMedalReceipt);
                Assert.Equal(award.CharacterCurrency == null ? 1234ul : 2501234ul, read.CharacterCurrency.Single().Amount);
                if (held != null)
                {
                    var items = read.Item.AsNoTracking().ToArray();
                    Assert.Equal((ulong)award.Item.Count, items.Aggregate(0ul, (sum, item) => sum + item.StackCount));
                    Assert.All(items, item =>
                    {
                        Assert.Equal(award.Item.ItemId, item.ItemId); Assert.True(item.Soulbound);
                        Assert.InRange(item.StackCount, 1u, Math.Max(1u, f.Inventory.Info.Entry.MaxStackCount));
                        // Fresh item instances use the same actual metadata after SQL reload.
                        var reloaded = new NexusForever.Game.Entity.Item(item, f.Inventory.Manager);
                        Assert.Equal(f.Inventory.Info, reloaded.Info);
                        Assert.Equal(item.StackCount, reloaded.StackCount);
                    });
                }
                else Assert.True(Assert.Single(read.CharacterContractAccountOutbox).Delivered);
                var loaded = await f.Load();
                Assert.True(loaded.ContractTracks.Single(track => track.TrackId == award.TrackId).Slots[award.Ordinal].Claimed);
                results.Add(new
                {
                    Reward = award.RewardRowId, award.Choice, award.ChoiceId, award.ChoiceCount,
                    Item = award.Item != null, Stackable = award.Item == null ? (bool?)null : f.Inventory.Info.IsStackable(),
                    MaxStack = award.Item == null ? (uint?)null : f.Inventory.Info.Entry.MaxStackCount,
                    Soulbind = award.Item?.ForceSoulbind, QuoteSha256 = Hash(System.Text.Encoding.UTF8.GetBytes(receipt.QuoteJson)),
                    DeliverySha256 = Hash(System.Text.Encoding.UTF8.GetBytes(receipt.DeliveryJson ?? ""))
                });
                if (results.Count % 30 == 0) Console.WriteLine($"Contract actual choices {results.Count}/726, elapsed {(DateTime.UtcNow - started).TotalSeconds:F1}s");
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(outputDirectory, "failure.json"), JsonSerializer.Serialize(new
                { award.RewardRowId, award.Choice, Passed = results.Count, Error = error.ToString() }, json));
                throw;
            }
        }
        File.WriteAllText(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(new
        {
            CompletedUtc = DateTime.UtcNow, Passed = results.Count, ElapsedSeconds = (DateTime.UtcNow - started).TotalSeconds,
            Limit = "Internal claim settlement/delivery/replay/reload with actual ItemInfo. Proxy actor and synthetic satisfied prerequisites/selected cycle; no public claim/reset, MySQL, deployment or retail qualification.",
            Results = results
        }, json));
        Console.WriteLine($"Contract actual delivery complete: {results.Count}/726");
    }
}
