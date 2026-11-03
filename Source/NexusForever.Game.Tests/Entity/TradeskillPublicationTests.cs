using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SourceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class TradeskillPublicationTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public TradeskillPublicationTests() { }
    internal TradeskillPublicationTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task LearnedSkillsAndSchematicsWaitForTheirOriginalGraphAndWorldUpdate()
    {
        using var f = await Create();
        using (var setup = f.Context()) await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite()
            ? "CREATE TRIGGER reject_recipe BEFORE INSERT ON character_schematic BEGIN SELECT RAISE(ABORT, 'owned recipe rejection'); END"
            : "CREATE TRIGGER reject_recipe BEFORE INSERT ON character_schematic FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned recipe rejection'");
        Assert.True(f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0)); Assert.True(f.Actor.Player.LearnSchematic(501, true));
        Assert.Empty(f.Packets<ServerProfessionUpdate>()); Assert.Empty(f.Packets<ServerSchematicAddLearned>());
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Save()); f.Pump();
        using (var read = f.Context()) { Assert.Empty(read.CharacterTradeskill); Assert.Empty(read.CharacterSchematic); }
        Assert.Empty(f.Packets<ServerProfessionUpdate>()); Assert.Empty(f.Packets<ServerSchematicAddLearned>());
        using (var setup = f.Context()) await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_recipe");
        await f.Save(); Assert.Empty(f.Packets<ServerProfessionUpdate>()); f.Pump();
        Assert.Equal(TradeskillType.Armorer, Assert.Single(f.Packets<ServerProfessionUpdate>()).Tradeskill.TradeskillId);
        var recipe = Assert.Single(f.Packets<ServerSchematicAddLearned>()); Assert.Equal(501u, recipe.TradeskillSchematic2Id);
        Assert.Equal(18f, recipe.DiscoveryCoordinates.X); Assert.Equal(27f, recipe.DiscoveryCoordinates.Y);
        using var final = f.Context(); Assert.Single(final.CharacterTradeskill); Assert.True(Assert.Single(final.CharacterSchematic).Discovered); Assert.Empty(final.CharacterEntitlement);
    }

    [Fact]
    public async Task InitialListsHideUnsavedSkillsAndRecipesUntilCommittedPublication()
    {
        using var f = await Create();
        f.Actor.Player.LearnTradeskill(TradeskillType.Armorer, 0); f.Actor.Player.LearnSchematic(501, true);
        f.Actor.Player.SendTradeskillInitialPackets(); var initial = Assert.Single(f.Packets<ServerProfessionsLoad>());
        Assert.Empty(initial.Tradeskills); Assert.Empty(initial.LearnedSchematics); Assert.Empty(initial.DiscoveredSchematics); Assert.Empty(initial.LearnedSchematicDiscoveredFlags);
        await f.Save(); f.Pump(); f.Actor.Player.SendTradeskillInitialPackets(); var committed = f.Packets<ServerProfessionsLoad>().Last();
        Assert.Equal(TradeskillType.Armorer, Assert.Single(committed.Tradeskills).TradeskillId); Assert.Equal(new uint[] { 501 }, committed.LearnedSchematics);
        Assert.Equal(501u, Assert.Single(committed.DiscoveredSchematics).TradeskillSchematic2Id); Assert.Equal(new uint[] { 1 }, committed.LearnedSchematicDiscoveredFlags);
    }

    [Fact]
    public async Task ArchiveReactivationXpAndTalentNoticesRetainTheirAcceptedPayloads()
    {
        using var f = await Create(existing: true); var player = f.Actor.Player;
        player.LearnTradeskill(TradeskillType.Weaponsmith, TradeskillType.Armorer); player.LearnTradeskill(TradeskillType.Armorer, TradeskillType.Weaponsmith);
        Assert.Equal(20u, player.AddTradeskillXp(TradeskillType.Armorer, 20)); player.EnsureTradeskillTalentPointTotal(TradeskillType.Armorer, 10);
        Assert.True(player.PickTradeskillTalent(TradeskillType.Armorer, 1, 88)); Assert.True(player.ResetTradeskillTalents(TradeskillType.Armorer));
        Assert.Empty(f.Packets<ServerProfessionUpdate>()); Assert.Empty(f.Packets<ServerTradeskillRelearnCooldown>());
        await f.Save(); f.Pump(); var updates = f.Packets<ServerProfessionUpdate>().Select(row => row.Tradeskill).ToArray();
        Assert.Equal(new uint[] { 0, 0, 0, 180, 200, 200, 200, 200 }, updates.Select(row => row.TradeskillXp));
        Assert.Equal(new uint[] { 0, 0, 0, 7, 7, 9, 8, 0 }, updates.Select(row => row.TalentPoints));
        Assert.Equal(77u, updates[3].TradeskillTalentTierIds[0]); Assert.Equal(88u, updates[6].TradeskillTalentTierIds[1]);
        Assert.All(updates[^1].TradeskillTalentTierIds, value => Assert.Equal(0u, value)); Assert.Single(f.Packets<ServerTradeskillRelearnCooldown>());
        using var read = f.Context(); Assert.Equal(200u, read.CharacterTradeskill.Single(row => row.TradeskillId == (uint)TradeskillType.Armorer).TradeskillXp);
        Assert.Equal(0u, read.CharacterTradeskill.Single(row => row.TradeskillId == (uint)TradeskillType.Weaponsmith).IsActive);
    }

    private async Task<Fixture> Create(bool existing = false)
    {
        var source = new SourceFixture(options); source.Seed(); PlayerProgressionPersistenceTests.Seed(source, existing);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var messages); var events = new EventQueue(); messages.SetProperty("Events", events);
        var actor = new Actor(source, actorSession: session, actorEvents: events); messages.SetProperty("Player", actor.Player);
        await actor.Load(); PlayerProgressionPersistenceTests.Configure(source, actor); return new(source, actor, messages);
    }

    private sealed record Fixture(SourceFixture Source, Actor Actor, RecordingDispatchProxy<IWorldSession> Messages) : IDisposable
    {
        internal CharacterContext Context(params IInterceptor[] interceptors) => Source.Context(interceptors);
        internal Task Save(params IInterceptor[] interceptors) => CharacterAtomicSave.Execute(() => Context(interceptors), 42, Actor.Player.Save);
        internal IEnumerable<T> Packets<T>() => Messages.GetInvocations("EnqueueMessageEncrypted").Select(call => call.Arguments[0]).OfType<T>();
        internal void Pump() { Actor.Events.Update(.01); Actor.Events.Update(.01); }
        public void Dispose() { Actor.Dispose(); Source.Dispose(); }
    }
}
