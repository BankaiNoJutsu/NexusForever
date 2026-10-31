using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Message.Handler;
using NexusForever.Network.Message.Model;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Network;

public class PacketDispatchLifetimeTests
{
    [Fact]
    public void PacketMessageAndHandlerShareScopedDependencyAndDisposeBeforeNextPacket()
    {
        using var f = new Fixture();
        f.Session.HandlePacket(Packet());
        Assert.Equal(1, f.State.Handled);
        Assert.Equal(1, f.State.HandlerDisposals);
        Assert.Equal(1, f.State.MessageDisposals);
        Assert.Single(f.State.Dependencies);
        Assert.True(f.State.Dependencies[0].Disposed);
        Assert.False(f.State.Shared.Disposed);

        f.Session.HandlePacket(Packet());
        Assert.Equal(2, f.State.Handled);
        Assert.Equal(2, f.State.HandlerDisposals);
        Assert.Equal(2, f.State.MessageDisposals);
        Assert.Equal(2, f.State.Dependencies.Count);
        Assert.NotSame(f.State.Dependencies[0], f.State.Dependencies[1]);
        Assert.All(f.State.Dependencies, p => Assert.True(p.Disposed));
        Assert.Equal(2, f.Session.ScopeCount);
    }

    [Fact]
    public void ReadFailureDisposesResolvedMessageHandlerAndDependencyWithoutHandling()
    {
        using var f = new Fixture();
        f.Session.HandlePacket(Packet(includePayload: false));
        Assert.Equal(0, f.State.Handled);
        Assert.Equal(1, f.State.HandlerDisposals);
        Assert.Equal(1, f.State.MessageDisposals);
        Assert.True(Assert.Single(f.State.Dependencies).Disposed);
        Assert.False(f.State.Shared.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandlerFailureStillDisposesPacketScope(bool invalidPacket)
    {
        using var f = new Fixture();
        f.State.Failure = invalidPacket ? new InvalidPacketValueException() : new InvalidOperationException();
        f.Session.HandlePacket(Packet());
        Assert.Equal(1, f.State.Handled);
        Assert.Equal(1, f.State.HandlerDisposals);
        Assert.Equal(1, f.State.MessageDisposals);
        Assert.True(Assert.Single(f.State.Dependencies).Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnhandledOrUnregisteredMessageClosesScope(bool unregisteredMessage)
    {
        using var f = new Fixture(registerHandler: false);
        f.Session.HandlePacket(Packet(unregisteredMessage ? GameMessageOpcode.ClientPregameKeepAlive : GameMessageOpcode.ClientRequestInputKeySet));
        Assert.Equal(1, f.Session.ScopeCount);
        Assert.Equal(0, f.State.Handled);
        Assert.Equal(0, f.State.HandlerDisposals);
        Assert.Equal(unregisteredMessage ? 0 : 1, f.State.MessageDisposals);
        Assert.All(f.State.Dependencies, p => Assert.True(p.Disposed));
    }

    [Fact]
    public void PackedPacketCreatesIndependentInnerScopeAndRunsRealWrapperHandler()
    {
        using var f = new Fixture();
        ClientGamePacket inner = Packet();
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write((uint)GameMessageOpcode.ClientPacked, 16);
            writer.Write((uint)(inner.Data.Length + 4));
            writer.WriteBytes(inner.Data);
            writer.FlushBits();
        }
        f.Session.HandlePacket(new ClientGamePacket { Data = stream.ToArray() });
        Assert.Equal(1, f.State.Handled);
        Assert.Equal(2, f.Session.ScopeCount);
        Assert.Equal(1, f.State.HandlerDisposals);
        Assert.True(Assert.Single(f.State.Dependencies).Disposed);
    }

    [Fact]
    public void RootOwnedEntityFactoryAndCreatedEntitySurvivePacketScopeDisposal()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(RecordingDispatchProxy<IGameTableManager>.Create(out _));
        // Use the production registration, including movement and entity services.
        services.AddGameEntity();
        services.AddTransient<RetainedEntity>(provider => new RetainedEntity(RecordingDispatchProxy<IMovementManager>.Create(out _)));
        using ServiceProvider root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        // Negative control: the previous transient registration captured the packet
        // provider, prematurely disposed the entity, and stranded its runtime resolvers.
        EntityFactory scopedFactory;
        RetainedEntity packetOwnedEntity;
        using (IServiceScope packetScope = root.CreateScope())
        {
            scopedFactory = new EntityFactory(packetScope.ServiceProvider);
            packetOwnedEntity = scopedFactory.CreateEntity<RetainedEntity>();
        }
        Assert.True(packetOwnedEntity.Disposed);
        Assert.Throws<ObjectDisposedException>(() => packetOwnedEntity.ResolveTables());
        Assert.Throws<ObjectDisposedException>(() => scopedFactory.CreateEntity<RetainedEntity>());

        IEntityFactory factory;
        RetainedEntity entity;
        using (IServiceScope packetScope = root.CreateScope())
        {
            factory = packetScope.ServiceProvider.GetRequiredService<IEntityFactory>();
            entity = factory.CreateEntity<RetainedEntity>();
        }

        Assert.Same(root.GetRequiredService<IEntityFactory>(), factory);
        Assert.False(entity.Disposed);
        Assert.Same(root.GetRequiredService<IGameTableManager>(), entity.ResolveTables());
        Assert.False(factory.CreateEntity<RetainedEntity>().Disposed);
        root.Dispose();
        Assert.True(entity.Disposed);
    }

    [Fact]
    public void DeferredEntityWorkCanResolveDependenciesAfterDispatchCompletes()
    {
        using var f = new Fixture();
        f.State.DeferEntityWork = true;
        f.Session.HandlePacket(Packet());
        Assert.Equal(1, f.State.HandlerDisposals);
        Assert.False(f.State.Entity.Disposed);
        Assert.Null(f.State.DeferredTables);
        f.Session.Events.Update(0);
        Assert.Same(f.Root.GetRequiredService<IGameTableManager>(), f.State.DeferredTables);
        Assert.NotNull(f.State.DeferredEntity);
        Assert.False(f.State.DeferredEntity.Disposed);
    }

    private static ClientGamePacket Packet(GameMessageOpcode opcode = GameMessageOpcode.ClientRequestInputKeySet, bool includePayload = true)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write((uint)opcode, 16);
            if (includePayload) writer.Write((byte)7);
            writer.FlushBits();
        }
        return new ClientGamePacket { Data = stream.ToArray() };
    }

    private sealed class Fixture : IDisposable
    {
        public State State { get; } = new();
        public ServiceProvider Root { get; }
        public TestSession Session { get; }

        public Fixture(bool registerHandler = true)
        {
            var manager = new MessageManager(NullLogger<MessageManager>.Instance);
            manager.RegisterMessage(typeof(LifetimeMessage));
            if (registerHandler) manager.RegisterMessageHandler(typeof(LifetimeHandler));
            manager.RegisterMessage(typeof(ClientPacked));
            manager.RegisterMessageHandler(typeof(ClientPackedHandler));
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddGameEntity();
            services.AddSingleton(RecordingDispatchProxy<IGameTableManager>.Create(out _));
            services.AddTransient<RetainedEntity>(provider => new RetainedEntity(RecordingDispatchProxy<IMovementManager>.Create(out _)));
            services.AddSingleton(State);
            services.AddSingleton(State.Shared);
            services.AddScoped<Dependency>();
            services.AddTransient<LifetimeHandler>();
            services.AddKeyedTransient<IReadable, LifetimeMessage>(GameMessageOpcode.ClientRequestInputKeySet);
            services.AddTransient<ClientPackedHandler>();
            services.AddKeyedTransient<IReadable, ClientPacked>(GameMessageOpcode.ClientPacked);
            Root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            Session = new TestSession(manager, Root);
        }

        public void Dispose() => Root.Dispose();
    }

    private sealed class State
    {
        public List<Dependency> Dependencies { get; } = [];
        public Shared Shared { get; } = new();
        public int Handled { get; set; }
        public int MessageDisposals { get; set; }
        public int HandlerDisposals { get; set; }
        public Exception Failure { get; set; }
        public bool DeferEntityWork { get; set; }
        public RetainedEntity Entity { get; set; }
        public RetainedEntity DeferredEntity { get; set; }
        public IGameTableManager DeferredTables { get; set; }
    }

    private sealed class Dependency : IDisposable
    {
        public bool Disposed { get; private set; }
        public Dependency(State state) => state.Dependencies.Add(this);
        public void Dispose() { Assert.False(Disposed); Disposed = true; }
    }

    private sealed class Shared : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    [Message(GameMessageOpcode.ClientRequestInputKeySet)]
    private sealed class LifetimeMessage(State state, Dependency dependency) : IReadable, IDisposable
    {
        public Dependency Dependency { get; } = dependency;
        public void Read(GamePacketReader reader) => Assert.Equal(7, reader.ReadByte());
        public void Dispose() { Assert.False(Dependency.Disposed); state.MessageDisposals++; }
    }

    private sealed class LifetimeHandler(State state, Dependency dependency, Shared shared, IEntityFactory factory)
        : IMessageHandler<IGameSession, LifetimeMessage>, IDisposable
    {
        public void HandleMessage(IGameSession session, LifetimeMessage message)
        {
            Assert.False(dependency.Disposed);
            Assert.False(shared.Disposed);
            Assert.Same(dependency, message.Dependency);
            state.Handled++;
            if (state.Failure != null) throw state.Failure;
            if (state.DeferEntityWork)
            {
                RetainedEntity entity = state.Entity = factory.CreateEntity<RetainedEntity>();
                session.Events.EnqueueEvent(new PredicateEvent(() => true, () =>
                {
                    state.DeferredTables = entity.ResolveTables();
                    state.DeferredEntity = factory.CreateEntity<RetainedEntity>();
                }));
            }
        }

        public void Dispose() { Assert.False(dependency.Disposed); state.HandlerDisposals++; }
    }

    private sealed class RetainedEntity(IMovementManager movement) : WorldEntity(movement)
    {
        public override EntityType Type => EntityType.Simple;
        protected override IEntityModel BuildEntityModel() => throw new NotSupportedException();
        public bool Disposed { get; private set; }
        public IGameTableManager ResolveTables() => GetGameTableManager();
        public override void Dispose() { Disposed = true; base.Dispose(); }
    }

    private sealed class TestSession(IMessageManager manager, IServiceProvider provider) : GameSession(manager, provider)
    {
        public int ScopeCount { get; private set; }
        protected override IServiceScope CreateHandlePacketScope() { ScopeCount++; return base.CreateHandlePacketScope(); }
        protected override IWritable BuildEncryptedMessage(byte[] data) => throw new NotSupportedException();
    }
}
