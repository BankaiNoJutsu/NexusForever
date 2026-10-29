using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Configuration.Model;
using NexusForever.Database.Group;
using NexusForever.Database.Group.Model;
using NexusForever.Database.Group.Repository;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Server.GroupServer.Character;
using NexusForever.Server.GroupServer.Group;
using NexusForever.Server.GroupServer.Job;
using NexusForever.Server.GroupServer.Network.Internal;
using Quartz;

namespace NexusForever.Game.Tests.GroupJobs;

public class CharacterDirtyScheduledJobTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_RearmedCharacterDoesNotStarveOtherCharacters(bool stats)
    {
        await using var fixture = await Fixture.Create(stats, grouped: false);
        fixture.Observer.RearmAfterFirstSave = true;

        await fixture.Execute();

        await using var check = fixture.CreateContext();
        Assert.All(await check.Character.ToListAsync(), c => Assert.False(stats ? c.StatsDirty : c.RealmDirty));
        Assert.Equal(3, fixture.Observer.SaveCount);
        Assert.Equal(3, fixture.Observer.SavedContextIds.Count);
        Assert.False(fixture.Cancellation.IsCancellationRequested);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_CommitsOutboxMessagesWithClearedFlags(bool stats)
    {
        await using var fixture = await Fixture.Create(stats, grouped: true);

        await fixture.Execute();

        await using var check = fixture.CreateContext();
        Assert.All(await check.Character.ToListAsync(), c => Assert.False(stats ? c.StatsDirty : c.RealmDirty));
        var messages = await check.InternalMessage.ToListAsync();
        Assert.Equal(2, messages.Count);
        Type messageType = stats ? typeof(GroupMemberStatsUpdatedMessage) : typeof(GroupMemberRealmUpdatedMessage);
        Assert.All(messages, m => Assert.Equal(messageType.AssemblyQualifiedName, m.Type));
        Assert.Equal(new[] { 1, 1 }, fixture.Observer.OutboxRowsPerSave);
        Assert.Empty(fixture.Broker.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_SaveFailureLeavesFlagsDirtyAndDoesNotPublish(bool stats)
    {
        await using var fixture = await Fixture.Create(stats, grouped: true);
        fixture.Observer.FailSave = true;

        await Assert.ThrowsAsync<DbUpdateException>(fixture.Execute);

        await using var check = fixture.CreateContext();
        Assert.All(await check.Character.ToListAsync(), c => Assert.True(stats ? c.StatsDirty : c.RealmDirty));
        Assert.Empty(await check.InternalMessage.ToListAsync());
        Assert.Empty(fixture.Broker.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_AlreadyCancelledDoesNotProcessCharacters(bool stats)
    {
        await using var fixture = await Fixture.Create(stats, grouped: false);
        fixture.Cancellation.Cancel();

        await fixture.Execute();

        await using var check = fixture.CreateContext();
        Assert.All(await check.Character.ToListAsync(), c => Assert.True(stats ? c.StatsDirty : c.RealmDirty));
        Assert.Equal(0, fixture.Observer.SaveCount);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly bool stats;
        private readonly DatabaseConnectionString database;
        private readonly SqliteConnection keepAlive;
        private ServiceProvider services;
        public CancellationTokenSource Cancellation { get; } = new(TimeSpan.FromSeconds(15));
        public IterationObserver Observer { get; }
        public RecordingBroker Broker { get; } = new();

        private Fixture(bool stats)
        {
            this.stats = stats;
            database = new DatabaseConnectionString
            {
                Provider = DatabaseProvider.Sqlite,
                ConnectionString = $"Data Source=dirty-job-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False"
            };
            keepAlive = new SqliteConnection(database.ConnectionString);
            Observer = new IterationObserver(this, stats);
        }

        public static async Task<Fixture> Create(bool stats, bool grouped)
        {
            var fixture = new Fixture(stats);
            await fixture.keepAlive.OpenAsync();
            await using (var seed = fixture.CreateContext())
            {
                await seed.Database.EnsureCreatedAsync();
                var characters = new[] { 1ul, 2ul }.Select(id => new CharacterModel
                {
                    CharacterId = id, RealmId = 1, RealmName = "TestRealm", Name = $"Player{id}",
                    RealmDirty = !stats, StatsDirty = stats
                }).ToArray();
                seed.Character.AddRange(characters);
                if (grouped)
                {
                    var group = new GroupModel { GroupId = 100 };
                    for (int i = 0; i < characters.Length; i++)
                    {
                        var member = new GroupMemberModel
                        {
                            Group = group, Character = characters[i], GroupId = 100,
                            CharacterId = characters[i].CharacterId, RealmId = 1, Index = (uint)i
                        };
                        group.Members.Add(member);
                        characters[i].Groups.Add(new CharacterGroupModel
                        {
                            GroupId = 100, Character = characters[i], Member = member,
                            CharacterId = characters[i].CharacterId, RealmId = 1, Index = 0
                        });
                    }
                    seed.Group.Add(group);
                }
                await seed.SaveChangesAsync();
            }

            var services = new ServiceCollection();
            services.AddGroupDatabase(fixture.database);
            services.AddDbContext<GroupContext>(b => b.AddInterceptors(fixture.Observer));
            services.AddGroup();
            services.AddCharacter();
            services.AddScoped(sp => new CharacterManager(sp.GetRequiredService<CharacterRepository>(), null, sp));
            services.AddTransient<OutboxMessagePublisher>();
            services.AddSingleton<IInternalMessagePublisher>(fixture.Broker);
            fixture.services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            return fixture;
        }

        public GroupContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<GroupContext>();
            options.UseConfiguration(database);
            return new GroupContext(options.Options);
        }

        public async Task Execute()
        {
            IJobExecutionContext execution = RecordingDispatchProxy<IJobExecutionContext>.Create(out var proxy);
            proxy.SetProperty(nameof(IJobExecutionContext.CancellationToken), Cancellation.Token);
            await using AsyncServiceScope scope = services.CreateAsyncScope();
            IJob job = stats
                ? ActivatorUtilities.CreateInstance<CharacterDirtyStatsScheduledJob>(scope.ServiceProvider)
                : ActivatorUtilities.CreateInstance<CharacterDirtyRealmScheduledJob>(scope.ServiceProvider);
            await job.Execute(execution);
        }

        public async ValueTask DisposeAsync()
        {
            await services.DisposeAsync();
            await keepAlive.DisposeAsync();
            Cancellation.Dispose();
        }
    }

    private sealed class IterationObserver(Fixture fixture, bool stats) : SaveChangesInterceptor
    {
        public bool RearmAfterFirstSave { get; set; }
        public bool FailSave { get; set; }
        public int SaveCount { get; private set; }
        public HashSet<Guid> SavedContextIds { get; } = [];
        public List<int> OutboxRowsPerSave { get; } = [];

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            OutboxRowsPerSave.Add(eventData.Context.ChangeTracker.Entries<InternalMessageModel>()
                .Count(e => e.State == EntityState.Added));
            if (FailSave)
                throw new DbUpdateException("Simulated commit failure");
            return ValueTask.FromResult(result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            SavedContextIds.Add(eventData.Context.ContextId.InstanceId);
            if (RearmAfterFirstSave && SaveCount == 1)
            {
                ulong id = eventData.Context.ChangeTracker.Entries<CharacterModel>()
                    .Single(e => !(stats ? e.Entity.StatsDirty : e.Entity.RealmDirty)).Entity.CharacterId;
                await using var writer = fixture.CreateContext();
                var character = await writer.Character.SingleAsync(c => c.CharacterId == id);
                character.StatsDirty = stats;
                character.RealmDirty = !stats;
                await writer.SaveChangesAsync();
            }
            // Bound the broken implementation deterministically instead of spinning until a timeout.
            if (SaveCount >= 8)
                fixture.Cancellation.Cancel();
            return result;
        }
    }

    private sealed class RecordingBroker : IInternalMessagePublisher
    {
        public List<object> Messages { get; } = [];
        public Task PublishAsync(object message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
