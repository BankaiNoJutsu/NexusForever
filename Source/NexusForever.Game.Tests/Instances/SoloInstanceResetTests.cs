using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Map.Lock;
using NexusForever.Game.Static.Map.Lock;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Instance;
using NexusForever.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Instance;

namespace NexusForever.Game.Tests.Instances;

public class SoloInstanceResetTests
{
    private static readonly Identity Character = new() { RealmId = 1, Id = 42 };

    [Fact]
    public void ResetVacatedInstancesMakesNextEntryUseNewLocks()
    {
        var f = new Fixture();
        IMapLock old = f.Locks.CreateSoloLock(Character, 51);
        f.Locks.CreateSoloLock(Character, 52);
        f.Reset();
        Assert.Empty(f.Locks.GetSoloLocks(Character));
        Assert.Null(f.Locks.GetSoloLock(Character, 51));
        IMapLock replacement = f.Locks.CreateSoloLock(Character, 51);
        Assert.NotEqual(old.InstanceId, replacement.InstanceId);
        Assert.Equal(MapLockType.Solo, replacement.Type);
        Assert.Equal("Your instances have been reset.", f.Message);
        Assert.Empty(f.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void ResetKeepsCurrentInstanceAndOnlyRemovesOtherSoloLocks()
    {
        var f = new Fixture();
        IMapLock current = f.Locks.CreateSoloLock(Character, 51);
        f.Locks.CreateSoloLock(Character, 52);
        f.SetCurrent(current);
        f.Reset();
        Assert.Same(current, Assert.Single(f.Locks.GetSoloLocks(Character)));
        Assert.Same(current, f.Locks.CreateSoloLock(Character, 51));
        Assert.Equal("Your instances have been reset. The instance you are in was kept.", f.Message);
    }

    [Fact]
    public void CurrentInstanceAloneReportsNothingToReset()
    {
        var f = new Fixture();
        f.SetCurrent(f.Locks.CreateSoloLock(Character, 51));
        f.Reset();
        Assert.Equal("You have no instances to reset.", f.Message);
        Assert.Single(f.Locks.GetSoloLocks(Character));
    }

    [Fact]
    public void ResetDoesNotTouchGroupLocksOrOtherCharactersAndRealms()
    {
        var f = new Fixture();
        Identity otherCharacter = Character with { Id = 43 };
        Identity otherRealm = Character with { RealmId = 2 };
        IMapLock other = f.Locks.CreateSoloLock(otherCharacter, 51);
        IMapLock realm = f.Locks.CreateSoloLock(otherRealm, 51);
        // Group map locks live in a separate collection even for the same world.
        IMapLock groupLock = f.Locks.CreateGroupLock(new GroupLootState { GroupId = 7, Leader = Character }, 51);
        f.SetCurrent(groupLock);
        f.Locks.CreateSoloLock(Character, 51);
        f.Reset();
        Assert.Empty(f.Locks.GetSoloLocks(Character));
        Assert.Same(groupLock, f.Locks.GetGroupLock(7, 51));
        Assert.Same(other, f.Locks.GetSoloLock(otherCharacter, 51));
        Assert.Same(realm, f.Locks.GetSoloLock(otherRealm, 51));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetDuringTravelDoesNotReleaseAnyLock(bool loading)
    {
        var f = new Fixture();
        IMapLock original = f.Locks.CreateSoloLock(Character, 51);
        f.PlayerProxy.SetProperty(nameof(IPlayer.IsLoading), loading);
        f.PlayerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), loading);
        f.Reset();
        Assert.Same(original, f.Locks.GetSoloLock(Character, 51));
        Assert.Equal("Instances cannot be reset while travelling.", f.Message);
    }

    [Fact]
    public void RepeatedOrEmptyResetDoesNotSendDialogResult()
    {
        var f = new Fixture();
        f.Locks.CreateSoloLock(Character, 51);
        f.Reset();
        f.Reset();
        Assert.Equal("You have no instances to reset.", f.Message);
        Assert.Empty(f.SessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void ResetWithoutPlayerIsIgnored()
    {
        var f = new Fixture();
        f.SessionProxy.SetProperty(nameof(IWorldSession.Player), null);
        f.Reset();
        Assert.Empty(f.PlayerProxy.GetInvocations(nameof(IPlayer.SendSystemMessage)));
    }

    [Fact]
    public void StaleSnapshotCannotRemoveReplacementInstance()
    {
        var f = new Fixture();
        IMapLock original = f.Locks.CreateSoloLock(Character, 51);
        Assert.True(f.Locks.RemoveSoloLock(Character, 51, original.InstanceId));
        IMapLock replacement = f.Locks.CreateSoloLock(Character, 51);
        Assert.False(f.Locks.RemoveSoloLock(Character, 51, original.InstanceId));
        Assert.Same(replacement, f.Locks.GetSoloLock(Character, 51));
    }

    [Fact]
    public void RemovalDropsCharacterMembershipAndKeepsOtherWorlds()
    {
        var f = new Fixture();
        IMapLock original = f.Locks.CreateSoloLock(Character, 51);
        IMapLock retained = f.Locks.CreateSoloLock(Character, 52);
        Assert.True(f.Locks.RemoveSoloLock(Character, 51, original.InstanceId));
        var characters = (HashSet<Identity>)typeof(MapLock).GetField("characters", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(original)!;
        Assert.DoesNotContain(Character, characters);
        Assert.Same(retained, Assert.Single(f.Locks.GetSoloLocks(Character)));
        Assert.False(f.Locks.RemoveSoloLock(Character, 51, original.InstanceId));
    }

    [Fact]
    public void SnapshotIsStableWhileLocksAreCreatedAndRemoved()
    {
        var f = new Fixture();
        Assert.Empty(f.Locks.GetSoloLocks(Character));
        IMapLock original = f.Locks.CreateSoloLock(Character, 51);
        var snapshot = f.Locks.GetSoloLocks(Character);
        f.Locks.CreateSoloLock(Character, 52);
        f.Locks.RemoveSoloLock(Character, 51, original.InstanceId);
        Assert.Same(original, Assert.Single(snapshot));
        Assert.Equal(52u, Assert.Single(f.Locks.GetSoloLocks(Character)).WorldId);
    }

    [Fact]
    public void MissingOwnerWorldOrInstanceCannotRemoveLock()
    {
        var f = new Fixture();
        IMapLock original = f.Locks.CreateSoloLock(Character, 51);
        Assert.False(f.Locks.RemoveSoloLock(Character with { Id = 99 }, 51, original.InstanceId));
        Assert.False(f.Locks.RemoveSoloLock(Character, 52, original.InstanceId));
        Assert.False(f.Locks.RemoveSoloLock(Character, 51, Guid.NewGuid()));
        Assert.Same(original, f.Locks.GetSoloLock(Character, 51));
    }

    [Fact]
    public void ConcurrentCreationSharesOneSoloInstance()
    {
        var f = new Fixture();
        var results = new IMapLock[64];
        Parallel.For(0, results.Length, i => results[i] = f.Locks.CreateSoloLock(Character, 51));
        Assert.All(results, r => Assert.Same(results[0], r));
        Assert.Same(results[0], Assert.Single(f.Locks.GetSoloLocks(Character)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContentMapEntryAfterResetUsesNewSoloLockOrRetainsGroupLock(bool grouped)
    {
        var f = new Fixture();
        IMapLock oldSolo = f.Locks.CreateSoloLock(Character, 51);
        var groups = RecordingDispatchProxy<IGroupStateManager>.Create(out var gp);
        if (grouped)
        {
            var group = new GroupLootState { GroupId = 7, Leader = Character, Members = [new() { Identity = Character }] };
            gp.SetMethodHandler(nameof(IGroupStateManager.TryGetGroupForCharacter), args => { args[1] = group; return true; });
        }
        var matches = RecordingDispatchProxy<IMatchManager>.Create(out var mp);
        mp.SetMethodReturn(nameof(IMatchManager.GetMatchCharacter), RecordingDispatchProxy<IMatchCharacter>.Create(out _));
        var map = new TestContentMap(f.Locks, matches, groups);
        map.Initialise(new NexusForever.GameTable.Model.WorldEntry { Id = 51 });
        IMapLock before = map.Enter(f.Session.Player);
        f.Reset();
        IMapLock after = map.Enter(f.Session.Player);
        if (grouped)
        {
            Assert.Same(before, after);
            Assert.Equal(MapLockType.Group, after.Type);
            Assert.Empty(f.Locks.GetSoloLocks(Character));
        }
        else
        {
            Assert.Same(oldSolo, before);
            Assert.NotEqual(before.InstanceId, after.InstanceId);
            Assert.Equal(MapLockType.Solo, after.Type);
        }
    }

    private sealed class TestContentMap(IMapLockManager locks, IMatchManager matches, IGroupStateManager groups)
        : ContentInstancedMap<IContentMapInstance>(locks, matches, groups, null)
    {
        public IMapLock Enter(IPlayer player) => GetMapLock(player);
    }

    private sealed class Fixture
    {
        public MapLockManager Locks { get; }
        public IWorldSession Session { get; }
        public RecordingDispatchProxy<IWorldSession> SessionProxy { get; }
        public RecordingDispatchProxy<IPlayer> PlayerProxy { get; }
        public string Message => (string)PlayerProxy.GetInvocations(nameof(IPlayer.SendSystemMessage))[^1].Arguments[0];

        public Fixture()
        {
            var factory = RecordingDispatchProxy<IFactoryInterface<IMapLock>>.Create(out var fp);
            fp.SetMethodReturnFactory("Resolve", () => new MapLock(NullLogger<MapLock>.Instance));
            var collections = RecordingDispatchProxy<IFactory<IMapLockCollection>>.Create(out var cp);
            cp.SetMethodReturnFactory("Resolve", () => new MapLockCollection());
            Locks = new MapLockManager(factory, collections);
            Session = RecordingDispatchProxy<IWorldSession>.Create(out var sp);
            SessionProxy = sp;
            var player = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            PlayerProxy = pp;
            pp.SetProperty(nameof(IPlayer.Identity), Character);
            pp.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
            sp.SetProperty(nameof(IWorldSession.Player), player);
        }

        public void SetCurrent(IMapLock mapLock)
        {
            var map = RecordingDispatchProxy<IMapInstance>.Create(out var mp);
            mp.SetProperty(nameof(IMapInstance.MapLock), mapLock);
            PlayerProxy.SetProperty(nameof(IPlayer.Map), map);
        }

        public void Reset() => new ClientResetInstancesHandler(NullLogger<ClientResetInstancesHandler>.Instance, Locks)
            .HandleMessage(Session, new ClientResetInstances());
    }
}
