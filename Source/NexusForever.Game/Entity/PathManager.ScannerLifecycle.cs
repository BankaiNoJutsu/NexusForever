using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Network.Session;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Shared.Game.Events;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Entity
{
    public partial class PathManager
    {
        private bool scientistScanbotDeploying;
        private ScientistScanbotContext scientistScanbotContext;
        private ulong scientistScanbotStateRevision;
        private static readonly NLog.ILogger scanbotLog = NLog.LogManager.GetCurrentClassLogger();

        // Keep the last context after removal so a rejected clear-state notice
        // can finish. A new deployment replaces it and supersedes every old tail.
        private sealed class ScientistScanbotContext
        {
            private readonly PathManager manager;
            private readonly IPlayer owner;
            private readonly ulong character;
            private readonly IAccount account;
            private readonly uint? accountId;
            internal readonly uint Guid;
            internal readonly IBaseMap Map;
            internal readonly IGameSession Session;
            internal readonly EventQueue Events;

            internal ScientistScanbotContext(PathManager manager, IPlayer owner)
            {
                this.manager = manager; this.owner = owner;
                character = owner.CharacterId; Guid = owner.Guid;
                account = owner.Account; accountId = account?.Id;
                Map = owner.Map; Session = owner.Session; Events = Session?.Events;
            }

            internal bool Current => Map != null && Session != null
                && ReferenceEquals(owner.PathManager, manager) && owner.Path == Path.Scientist
                && owner.CharacterId == character && owner.Guid == Guid
                && ReferenceEquals(owner.Account, account) && account?.Id == accountId
                && (account == null || ReferenceEquals(account.Session, Session))
                && Session is not NetworkSession { IsDisconnecting: true }
                && owner.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished)
                && ReferenceEquals(owner.Map, Map) && ReferenceEquals(owner.Session, Session)
                && ReferenceEquals(Session.Events, Events)
                && (Session is not IPlayerSession bound || ReferenceEquals(bound.Player, owner))
                && ReferenceEquals(Map.GetEntity<IWorldEntity>(Guid), owner);
        }

        private void SendScientistScanbotState(uint scanbotUnitId, uint scanbotCooldownMS)
        {
            var context = scientistScanbotContext;
            ulong revision = ++scientistScanbotStateRevision;
            bool Current() => ReferenceEquals(context, scientistScanbotContext)
                && context?.Current == true && revision == scientistScanbotStateRevision;
            void Publish()
            {
                if (!Current()) return;
                try
                {
                    context.Session.EnqueueMessageEncrypted(new ServerPathScientistScanbotState
                    {
                        ScanbotUnitId = scanbotUnitId,
                        ScanbotCooldownMS = scanbotCooldownMS
                    });
                }
                catch (Exception error) when (context.Events != null)
                {
                    scanbotLog.Warn(error, "Scientist scanner state failed; retrying on its original owner.");
                    var delay = Task.Delay(TimeSpan.FromSeconds(1));
                    context.Events.EnqueueThreadSafe(new PredicateEvent(() => !Current() || delay.IsCompleted, Publish));
                }
            }
            Publish();
        }
    }
}
