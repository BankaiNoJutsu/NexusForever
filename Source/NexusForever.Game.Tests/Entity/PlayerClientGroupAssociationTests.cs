using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network.Internal.Handler.Player;
using GameIdentity = NexusForever.Game.Abstract.Identity;
using InternalIdentity = NexusForever.Network.Internal.Message.Shared.Identity;

namespace NexusForever.Game.Tests.Entity;

public class PlayerClientGroupAssociationTests
{
    [Fact]
    public void ClientGroupAssociation_WhenUngroupedUsesStableNonZeroSentinel()
    {
        Player player = CreatePlayer(42ul);

        Assert.Equal(0ul, player.GroupAssociation);
        Assert.Equal(0x800000000000002Aul, player.ClientGroupAssociation);
    }

    [Fact]
    public void ClientGroupAssociation_WhenGroupedUsesRealGroupId()
    {
        Player player = CreatePlayer(42ul);

        player.GroupAssociation = 9001ul;

        Assert.Equal(9001ul, player.ClientGroupAssociation);
    }

    [Fact]
    public async Task PlayerGroupAssociationUpdatedHandler_WhenUngroupedBroadcastsClientAssociation()
    {
        const ulong characterId = 42ul;
        const ulong clientGroupAssociation = 0x800000000000002Aul;

        IPlayer player = CreatePlayerProxy(characterId, 1234u, clientGroupAssociation, out RecordingDispatchProxy<IPlayer> playerProxy);
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), _ => player);
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out RecordingDispatchProxy<IGroupStateManager> groupStateManagerProxy);

        var handler = new PlayerGroupAssociationUpdatedHandler(playerManager, groupStateManager);

        await handler.Handle(new PlayerGroupAssociationUpdatedMessage
        {
            Identity = new InternalIdentity
            {
                RealmId = 1,
                Id      = characterId
            }
        });

        RecordingDispatchProxy<IGroupStateManager>.Invocation removeCharacter = Assert.Single(groupStateManagerProxy.GetInvocations(nameof(IGroupStateManager.RemoveCharacter)));
        GameIdentity removedIdentity = Assert.IsType<GameIdentity>(removeCharacter.Arguments[0]);
        Assert.Equal(characterId, removedIdentity.Id);

        RecordingDispatchProxy<IPlayer>.Invocation groupAssociationSet = Assert.Single(playerProxy.GetInvocations("set_GroupAssociation"));
        Assert.Equal(0ul, Assert.IsType<ulong>(groupAssociationSet.Arguments[0]));

        RecordingDispatchProxy<IPlayer>.Invocation enqueue = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        ServerEntityGroupAssociation message = Assert.IsType<ServerEntityGroupAssociation>(enqueue.Arguments[0]);
        Assert.Equal(1234u, message.UnitId);
        Assert.Equal(clientGroupAssociation, message.GroupId);
        Assert.True(Assert.IsType<bool>(enqueue.Arguments[1]));
    }

    private static Player CreatePlayer(ulong characterId)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        SetProperty(player, nameof(Player.Identity), new GameIdentity
        {
            RealmId = 1,
            Id      = characterId
        });
        return player;
    }

    private static IPlayer CreatePlayerProxy(
        ulong characterId,
        uint guid,
        ulong clientGroupAssociation,
        out RecordingDispatchProxy<IPlayer> proxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out proxy);
        proxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        proxy.SetProperty(nameof(IPlayer.Guid), guid);
        proxy.SetProperty(nameof(IPlayer.ClientGroupAssociation), clientGroupAssociation);
        proxy.SetMethodHandler("SynchroniseAsync", args =>
        {
            var callback = (Func<bool>)args[0];
            return Task.FromResult(callback());
        });
        return player;
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        PropertyInfo property = target.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property.SetValue(target, value);
    }
}
