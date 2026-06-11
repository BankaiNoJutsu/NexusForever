using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.WorldServer.Command.Context;

namespace NexusForever.WorldServer.Command
{
    internal static class CommandContextResolver
    {
        /// <summary>
        /// Resolve console or websocket command contexts that target an online player via a leading @PlayerName prefix.
        /// </summary>
        public static bool TryResolve(
            ICommandContext context,
            ref string commandText,
            IPlayerManager playerManager,
            IRBACManager rbacManager,
            out ICommandContext resolvedContext)
        {
            resolvedContext = context;
            if (context.Invoker != null)
                return true;

            string trimmed = commandText.TrimStart();
            if (!trimmed.StartsWith('@'))
                return true;

            int spaceIndex = trimmed.IndexOf(' ');
            string playerName = spaceIndex < 0 ? trimmed[1..] : trimmed[1..spaceIndex];
            commandText = spaceIndex < 0 ? string.Empty : trimmed[(spaceIndex + 1)..].TrimStart();

            if (string.IsNullOrWhiteSpace(playerName))
            {
                context.SendError("Player name is required after '@'.");
                return false;
            }

            if (playerManager == null)
                throw new System.InvalidOperationException("Command context resolver requires an IPlayerManager.");

            IPlayer player = playerManager.GetPlayer(playerName);
            if (player == null)
            {
                context.SendError($"Player '{playerName}' is not online.");
                return false;
            }

            resolvedContext = context switch
            {
                ConsoleCommandContext       => new ConsoleCommandContext(rbacManager, player),
                WebSocketCommandContext web => new WebSocketCommandContext(web.WebSocket, rbacManager, player),
                _                           => context
            };
            return true;
        }
    }
}
