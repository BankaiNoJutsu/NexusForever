using System.Collections.Immutable;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Static.RBAC;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.WorldServer.Command;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Web.Middleware;

namespace NexusForever.Game.Tests.Network;

public class WebSocketCharacterContextTests
{
    [Fact]
    public async Task WebSocketRequestPassesCharacterToTheDeferredCommandContext()
    {
        var socket = new RequestSocket("{\"message\":\"help\",\"character\":\"Test Character\"}");
        var http = new DefaultHttpContext();
        http.Request.Path = "/ws/commands";
        http.Features.Set<IHttpWebSocketFeature>(new SocketFeature(socket));
        IPlayerManager players = RecordingDispatchProxy<IPlayerManager>.Create(out var playerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        playerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);
        ICommandManager commands = RecordingDispatchProxy<ICommandManager>.Create(out var commandProxy);
        ICommandContext queued = null;
        commandProxy.SetMethodHandler(nameof(ICommandManager.HandleCommandDelay), arguments =>
        {
            queued = Assert.IsType<WebSocketCommandContext>(arguments[0]);
            Assert.Equal("help", arguments[1]);
            return null;
        });
        var middleware = new WebSocketMiddleware(_ => Task.CompletedTask, commands, Rbac(), players);

        await middleware.InvokeAsync(http);

        Assert.NotNull(queued);
        Assert.Empty(playerProxy.Invocations);
        Assert.Same(player, queued.Invoker);
        Assert.Equal("Test Character", Assert.Single(playerProxy.Invocations).Arguments[0]);
    }

    [Fact]
    public void NamedCharacterIsResolvedAtInvocationAndRetainsWebSocketPermissions()
    {
        IRBACManager rbac = Rbac();
        IPlayerManager players = RecordingDispatchProxy<IPlayerManager>.Create(out var playerProxy);
        IPlayer first = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer replacement = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer current = first;
        playerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), arguments =>
        {
            Assert.Equal("Test Character", arguments[0]);
            return current;
        });

        var context = new WebSocketCommandContext(null, rbac, null,
            playerManager: players, characterName: "Test Character");
        Assert.Empty(playerProxy.Invocations);
        Assert.Equal(new[] { Permission.Help }, context.Permissions);
        Assert.Same(first, context.GetTargetOrInvoker<IPlayer>());
        current = replacement;
        Assert.Same(replacement, context.Invoker);
        current = null;
        Assert.Null(context.GetTargetOrInvoker<IPlayer>());
        Assert.Equal(3, playerProxy.Invocations.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UntargetedConsoleDoesNotLookUpAPlayer(string characterName)
    {
        IPlayerManager players = RecordingDispatchProxy<IPlayerManager>.Create(out var playerProxy);
        var context = new WebSocketCommandContext(null, Rbac(), null,
            playerManager: players, characterName: characterName);
        Assert.Null(context.Invoker);
        Assert.Empty(playerProxy.Invocations);
    }

    [Fact]
    public void ExplicitInvokerAndTargetRemainSupported()
    {
        IPlayer invoker = RecordingDispatchProxy<IPlayer>.Create(out _);
        IPlayer target = RecordingDispatchProxy<IPlayer>.Create(out _);
        var context = new WebSocketCommandContext(null, Rbac(), invoker, target);
        Assert.Same(invoker, context.Invoker);
        Assert.Same(target, context.GetTargetOrInvoker<IPlayer>());
    }

    private static IRBACManager Rbac()
    {
        IRBACRole role = RecordingDispatchProxy<IRBACRole>.Create(out var roleProxy);
        roleProxy.SetProperty(nameof(IRBACRole.Permissions),
            ImmutableDictionary<Permission, IRBACPermission>.Empty.Add(Permission.Help, null));
        IRBACManager rbac = RecordingDispatchProxy<IRBACManager>.Create(out var rbacProxy);
        rbacProxy.SetMethodHandler(nameof(IRBACManager.GetRole), arguments =>
        {
            Assert.Equal(Role.WebSocket, arguments[0]);
            return role;
        });
        return rbac;
    }

    private sealed class SocketFeature(WebSocket socket) : IHttpWebSocketFeature
    {
        public bool IsWebSocketRequest => true;
        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context) => Task.FromResult(socket);
    }

    private sealed class RequestSocket(string json) : WebSocket
    {
        private bool received;
        private WebSocketCloseStatus? closeStatus;
        public override WebSocketCloseStatus? CloseStatus => closeStatus;
        public override string CloseStatusDescription => null;
        public override string SubProtocol => null;
        public override WebSocketState State => closeStatus.HasValue ? WebSocketState.Closed : WebSocketState.Open;
        public override void Abort() { }
        public override void Dispose() { }
        public override Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken token)
            => CloseOutputAsync(status, description, token);
        public override Task CloseOutputAsync(WebSocketCloseStatus status, string description, CancellationToken token)
        {
            closeStatus = status;
            return Task.CompletedTask;
        }
        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType type, bool end, CancellationToken token)
            => Task.CompletedTask;
        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken token)
        {
            if (received)
            {
                closeStatus = WebSocketCloseStatus.NormalClosure;
                return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, closeStatus, null));
            }
            received = true;
            byte[] data = Encoding.UTF8.GetBytes(json);
            data.CopyTo(buffer.AsSpan());
            return Task.FromResult(new WebSocketReceiveResult(data.Length, WebSocketMessageType.Text, true));
        }
    }
}
