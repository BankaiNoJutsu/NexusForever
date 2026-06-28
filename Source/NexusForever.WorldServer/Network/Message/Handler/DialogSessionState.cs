using System.Runtime.CompilerServices;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler
{
    internal static class DialogSessionState
    {
        private static readonly ConditionalWeakTable<IWorldSession, State> States = new();

        public static void SetActiveDialog(IWorldSession session, uint dialogUnitId)
        {
            States.GetOrCreateValue(session).ActiveDialogUnitId = dialogUnitId;
        }

        public static void EndActiveDialog(IWorldSession session)
        {
            uint dialogUnitId = ConsumeActiveDialog(session) ?? session.Player?.TargetGuid ?? 0u;
            session.EnqueueMessageEncrypted(new ServerDialogEnd
            {
                DialogUnitId = dialogUnitId
            });
        }

        private static uint? ConsumeActiveDialog(IWorldSession session)
        {
            if (!States.TryGetValue(session, out State state))
                return null;

            uint? dialogUnitId = state.ActiveDialogUnitId;
            state.ActiveDialogUnitId = null;
            return dialogUnitId;
        }

        private sealed class State
        {
            public uint? ActiveDialogUnitId { get; set; }
        }
    }
}
