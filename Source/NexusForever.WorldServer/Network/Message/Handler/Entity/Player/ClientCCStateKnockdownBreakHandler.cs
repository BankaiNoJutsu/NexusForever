using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Entity;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity.Player
{
    public class ClientCCStateKnockdownBreakHandler : IMessageHandler<IWorldSession, ClientCCStateKnockdownBreak>
    {
        public void HandleMessage(IWorldSession session, ClientCCStateKnockdownBreak _)
        {
            if (session.Player == null)
                return;

            IReadOnlyCollection<SpellStateRemoval> removals = session.Player.RemoveCCStates(1u << (int)CCState.Knockdown);
            foreach (SpellStateRemoval removal in removals)
            {
                if (removal.CCState != CCState.Knockdown)
                    continue;

                session.Player.EnqueueToVisible(new ServerEntityCCStateRemove
                {
                    UnitId              = session.Player.Guid,
                    CCType              = CCState.Knockdown,
                    SpellCastUniqueId   = removal.CastingId,
                    SpellEffectUniqueId = removal.EffectId,
                    Removed             = true
                }, true);
            }
        }
    }
}