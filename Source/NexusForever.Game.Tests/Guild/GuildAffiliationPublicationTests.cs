using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;
using NexusForever.Game.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable.Model;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SourceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildAffiliationPublicationTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public GuildAffiliationPublicationTests() { }
    internal GuildAffiliationPublicationTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task PublicNameplateSelectionWaitsForCharacterCommitAndTheOriginalWorldQueue()
    {
        using var f = await Create();
        f.Manager.UpdateGuildAffiliation(8);
        Assert.Equal(8ul, f.Manager.GuildAffiliation.Id);
        Assert.Empty(f.Packets<ServerUnitGuildNameplateChanged>()); Assert.Empty(f.BrokerMessages());
        await f.Save();
        Assert.Empty(f.Packets<ServerUnitGuildNameplateChanged>()); Assert.Empty(f.BrokerMessages());
        f.Pump();
        Assert.Equal("Circle8", Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>()).GuildName);
        Assert.Equal("Circle8", Assert.Single(f.BrokerMessages()).GuildName);
        using var stored = f.Context(); Assert.Equal(8ul, stored.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task LeavingTheLastAffiliationDoesNotPublishRemovalBeforeItsCharacterCommit()
    {
        using var f = await Create(single: true);
        f.Manager.LeaveGuild(7);
        Assert.Null(f.Manager.GuildAffiliation);
        Assert.Empty(f.Packets<ServerUnitGuildNameplateChanged>()); Assert.Empty(f.BrokerMessages());
        await f.Save(); f.Pump();
        Assert.Equal("", Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>()).GuildName);
        Assert.Null(Assert.Single(f.BrokerMessages()).GuildName);
        using var stored = f.Context(); Assert.Null(stored.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task ARejectedNameplateEnqueueDoesNotPreventAcceptanceOrLoseItsCommittedReply()
    {
        using var f = await Create(); bool reject = true; int accepted = 0;
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerUnitGuildNameplateChanged)
            {
                if (reject) throw new IOException("Owned nameplate rejection.");
                accepted++;
            }
            return null;
        });
        Assert.Null(Record.Exception(() => f.Manager.UpdateGuildAffiliation(8)));
        Assert.Equal(8ul, f.Manager.GuildAffiliation.Id);
        await f.Save(); f.Pump(); Assert.Equal(0, accepted);
        reject = false; f.Pump(); Assert.Equal(1, accepted);
        Assert.Single(f.BrokerMessages());
    }

    [Fact]
    public async Task LoadedAffiliationStillInitialisesItsExistingNameplateSelection()
    {
        using var f = await Create(); f.Manager.OnLogin();
        var packet = Assert.Single(f.Packets<ServerGuildInit>());
        Assert.Equal(0u, packet.ShowNameplateIndex);
        Assert.Equal(1u, packet.Self[0].RecruitmentAvailability);
        Assert.Equal(0u, packet.Self[1].RecruitmentAvailability);
    }

    private async Task<Fixture> Create(bool single = false, bool guildTarget = false, bool missingLoaded = false)
    {
        var source = new SourceFixture(options); source.Seed();
        using (var seed = source.Context()) { seed.Character.Single().GuildAffiliation = missingLoaded ? null : (ulong?)7; await seed.SaveChangesAsync(); }
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var messages);
        var events = new EventQueue(); messages.SetProperty(nameof(IWorldSession.Events), events);
        var actor = new Actor(source, actorSession: session, actorEvents: events);
        messages.SetProperty(nameof(IWorldSession.Player), actor.Player);
        actor.Set(nameof(Player.Name), "Owner42"); actor.Set(nameof(Player.Guid), 4200u);
        await actor.Load();
        typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(actor.Player, new Dictionary<uint, IGridEntity>());
        typeof(WorldEntity).GetField("itemVisuals", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(actor.Player, new Dictionary<NexusForever.Game.Static.Entity.ItemSlot, IItemVisual>());
        ((RecordingDispatchProxy<IAppearanceManager>)(object)actor.Player.AppearanceManager)
            .SetMethodReturn(nameof(IAppearanceManager.GetBones), Array.Empty<IBone>());
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var broker);
        broker.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var guilds = (single ? new ulong[] { 7 } : [7ul, 8ul]).Select(id => Circle(id, guildTarget && (single || id == 8))).ToArray();
        var global = RecordingDispatchProxy<IGlobalGuildManager>.Create(out var cache);
        cache.SetMethodReturn(nameof(IGlobalGuildManager.GetCharacterGuilds), guilds);
        cache.SetMethodHandler(nameof(IGlobalGuildManager.GetGuild), args => guilds.SingleOrDefault(guild => guild.Id == Convert.ToUInt64(args[0])));
        var manager = new GuildManager(publisher, globalGuildManager: global);
        actor.Set(nameof(Player.GuildManager), manager);
        manager.Initialise(actor.Player, new CharacterModel { Id = 42, GuildAffiliation = missingLoaded ? null : (ulong?)7 });
        return new(source, actor, manager, messages, broker, events);
    }

    private static IGuildBase Circle(ulong id, bool isGuild = false)
    {
        RecordingDispatchProxy<IGuildBase> proxy = null;
        RecordingDispatchProxy<IGuild> guildProxy = null;
        IGuildBase guild = isGuild ? RecordingDispatchProxy<IGuild>.Create(out guildProxy) : RecordingDispatchProxy<IGuildBase>.Create(out proxy);
        void Property(string name, object value) { if (proxy != null) proxy.SetProperty(name, value); else guildProxy.SetProperty(name, value); }
        void Return(string name, object value) { if (proxy != null) proxy.SetMethodReturn(name, value); else guildProxy.SetMethodReturn(name, value); }
        void Handler(string name, Func<object[], object> value) { if (proxy != null) proxy.SetMethodHandler(name, value); else guildProxy.SetMethodHandler(name, value); }
        Property(nameof(IGuildBase.Id), id); Property(nameof(IGuildBase.Name), $"Circle{id}");
        Property(nameof(IGuildBase.Type), isGuild ? GuildType.Guild : GuildType.Circle);
        var member = RecordingDispatchProxy<IGuildMember>.Create(out var membership);
        membership.SetMethodHandler(nameof(IGuildMember.Build), _ => new NexusForever.Network.World.Message.Model.Guild.GuildMember());
        Return(nameof(IGuildBase.GetMember), member);
        Handler(nameof(IGuildBase.Build), _ => new GuildData());
        if (isGuild)
        {
            var standard = RecordingDispatchProxy<IGuildStandard>.Create(out var shape);
            static IGuildStandardPart Part(uint display)
            {
                var part = RecordingDispatchProxy<IGuildStandardPart>.Create(out var model);
                model.SetProperty(nameof(IGuildStandardPart.GuildStandardPartEntry), new GuildStandardPartEntry { ItemDisplayIdStandard = display });
                return part;
            }
            shape.SetProperty(nameof(IGuildStandard.ScanLines), Part(101));
            shape.SetProperty(nameof(IGuildStandard.BackgroundIcon), Part(102));
            shape.SetProperty(nameof(IGuildStandard.ForegroundIcon), Part(103));
            guildProxy.SetProperty(nameof(IGuild.Standard), standard);
        }
        return guild;
    }

    private sealed record Fixture(SourceFixture Source, Actor Actor, GuildManager Manager,
        RecordingDispatchProxy<IWorldSession> Messages, RecordingDispatchProxy<IInternalMessagePublisher> Broker,
        EventQueue Events) : IDisposable
    {
        internal CharacterContext Context(params IInterceptor[] interceptors) => Source.Context(interceptors);
        internal Task Save(params IInterceptor[] interceptors) => CharacterAtomicSave.Execute(() => Source.Context(interceptors), 42, Actor.Player.Save);
        internal IEnumerable<T> Packets<T>() => Messages.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(call => call.Arguments[0]).OfType<T>();
        internal IEnumerable<PlayerGuildAssociationUpdatedMessage> BrokerMessages()
            => Broker.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)).Select(call => call.Arguments[0]).OfType<PlayerGuildAssociationUpdatedMessage>();
        internal void Pump() { for (int i = 0; i < 6; i++) Events.Update(.01); }
        public void Dispose() { Actor.Dispose(); Source.Dispose(); }
    }
}
