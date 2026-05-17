using System;
using NexusForever.Game;
using NexusForever.Game.Chat;
using NexusForever.Game.Static.Chat;
using NexusForever.Game.Static.RBAC;
using NexusForever.Network.Session;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Command.Handler
{
    [Command(Permission.Realm, "A collection of commands to manage the realm.", "realm")]
    public class RealmCommandCategory : CommandCategory
    {
        private readonly INetworkManager<IWorldSession> networkManager;
        private readonly ILoginQueueManager queueManager;

        public RealmCommandCategory(
            INetworkManager<IWorldSession> networkManager,
            ILoginQueueManager queueManager)
        {
            this.networkManager = networkManager;
            this.queueManager   = queueManager;
        }

        [Command(Permission.RealmShutdown, "A collection of commands to manage realm shutdown.", "shutdown")]
        public class RealmShutdownCommandCategory : CommandCategory
        {
            [Command(Permission.RealmShutdownStart, "Start a new realm shutdown.", "start")]
            public void HandleRealmShutdownStart(ICommandContext context,
                [Parameter("Time till shutdown. (Format: dd:hh:mm:ss)")]
                TimeSpan span)
            {
                if (ShutdownManager.Instance.IsShutdownPending)
                {
                    context.SendError("Realm already has a pending shutdown!");
                    return;
                }

                ShutdownManager.Instance.StartShutdown(span);
            }

            [Command(Permission.RealmShutdownCancel, "Cancel pending realm shutdown.", "cancel")]
            public void HandleRealmShutdownCancel(ICommandContext context)
            {
                if (!ShutdownManager.Instance.IsShutdownPending)
                {
                    context.SendError("Realm doesn't have a pending shutdown!");
                    return;
                }

                ShutdownManager.Instance.CancelShutdown();
            }
        }

        [Command(Permission.RealmMOTD, "Set the realm's message of the day and announce to the realm.", "motd")]
        public void HandleRealmMotd(ICommandContext context,
            [Parameter("New message of the day for the realm.")]
            string message)
        {
            RealmContext.Instance.Motd = message;

            foreach (IWorldSession session in networkManager)
                GlobalChatManager.Instance.SendMessage(session, RealmContext.Instance.Motd, "MOTD", ChatChannelType.Realm);
        }

        [Command(Permission.RealmMaxPlayers, "Set the maximum players allowed to connect.", "max")]
        public void HandleRealmMax(ICommandContext context,
            [Parameter("Max players allowed.")]
            uint maxPlayers)
        {
            queueManager.SetMaxPlayers(maxPlayers);
        }
    }
}
