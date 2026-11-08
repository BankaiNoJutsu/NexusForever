using System.Security.Cryptography;
using System.Text.Json;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Transport;

public partial class TransportHandlerTests
{
    // Explicit qualification only; private client assets are never a unit-test prerequisite.
    internal static void RunActualCatalog(string assetRoot, string outputDirectory)
    {
        assetRoot = Path.GetFullPath(assetRoot);
        outputDirectory = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(outputDirectory)) throw new IOException("Choose a new evidence directory.");
        Directory.CreateDirectory(outputDirectory);
        byte[] manifest = File.ReadAllBytes(Path.Combine(assetRoot, ".nexusforever-assets.json"));
        using var inventory = JsonDocument.Parse(manifest);
        var hashes = new Dictionary<string, string>();
        GameTable<T> Load<T>(string name) where T : class, new()
        {
            string relative = "tbl/" + name + ".tbl";
            byte[] bytes = File.ReadAllBytes(Path.Combine(assetRoot, relative));
            string hash = Convert.ToHexString(SHA256.HashData(bytes));
            Assert.Equal(inventory.RootElement.GetProperty("Files").GetProperty(relative).GetString(), hash);
            hashes.Add(relative, hash);
            return new GameTable<T>(new MemoryStream(bytes));
        }
        var routes = Load<TaxiRouteEntry>("TaxiRoute");
        var nodes = Load<TaxiNodeEntry>("TaxiNode");
        var locations = Load<WorldLocation2Entry>("WorldLocation2");
        var worlds = Load<WorldEntry>("World");
        var results = new List<object>();
        foreach (var route in routes.Entries.Where(row => row != null))
        foreach (var faction in new[] { Faction.Exile, Faction.Dominion, Faction.None })
        {
            var handler = CreateFlightPathHandler(out var tables, out var player, out var currency, out var owner);
            tables.SetProperty(nameof(IGameTableManager.TaxiRoute), routes);
            tables.SetProperty(nameof(IGameTableManager.TaxiNode), nodes);
            tables.SetProperty(nameof(IGameTableManager.WorldLocation2), locations);
            tables.SetProperty(nameof(IGameTableManager.World), worlds);
            tables.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry { Id = 952u, Datafloat0 = 132.75f }));
            owner.SetProperty(nameof(IPlayer.Faction1), faction);
            var session = CreateSession(player, out _);
            handler.HandleMessage(session, BuildFlightPathPurchase(route.Id));
            var destination = nodes.GetEntry(route.TaxiNodeIdDestination);
            var location = destination == null ? null : locations.GetEntry(destination.WorldLocation2Id);
            bool Allowed(TaxiNodeEntry node) => node is { Id: > 0 and <= 0x3fff }
                && (node.TaxiNodeFactionEnum == 0 || node.TaxiNodeFactionEnum == 1 && faction == Faction.Exile
                    || node.TaxiNodeFactionEnum == 2 && faction == Faction.Dominion);
            bool expected = Allowed(nodes.GetEntry(route.TaxiNodeIdSource)) && Allowed(destination)
                && location is { WorldId: > 0 and <= ushort.MaxValue } && worlds.GetEntry(location.WorldId) != null
                && float.IsFinite(location.Position0) && float.IsFinite(location.Position1) && float.IsFinite(location.Position2);
            var quotes = owner.GetInvocations(nameof(IPlayer.QueueTaxiFlight));
            Assert.Empty(currency.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            if (expected)
            {
                var quote = Assert.Single(quotes);
                Assert.Equal((ulong)(float)(destination.ContentTier * 132.75f * destination.ContentTier), quote.Arguments[4]); // retail fare, not the raw Price column
                Assert.Equal(destination.Id, quote.Arguments[3]);
                Assert.Equal(location.WorldId, Assert.IsType<WorldEntry>(quote.Arguments[0]).Id);
                Assert.Equal(new System.Numerics.Vector3(location.Position0, location.Position1, location.Position2), quote.Arguments[1]);
                Assert.Empty(owner.GetInvocations(nameof(IPlayer.SendGenericError)));
            }
            else
            {
                Assert.Empty(quotes);
                Assert.Single(owner.GetInvocations(nameof(IPlayer.SendGenericError)));
            }
            results.Add(new { Route = route.Id, Faction = faction.ToString(), Accepted = expected });
        }
        Assert.NotEmpty(results);
        File.WriteAllText(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(new
        {
            CompletedUtc = DateTime.UtcNow, Passed = results.Count, Inputs = hashes,
            InventorySha256 = Convert.ToHexString(SHA256.HashData(manifest)), Results = results,
            Limit = "Parsed public flight-path quotes with actual tables and prepared faction/funds/kiosk access; no physical travel, provider/socket/native or retail qualification."
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
