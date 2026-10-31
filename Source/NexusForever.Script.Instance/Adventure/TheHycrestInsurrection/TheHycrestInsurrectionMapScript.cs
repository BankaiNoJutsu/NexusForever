using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Adventure.TheHycrestInsurrection
{
    [ScriptFilterOwnerId(1149)]
    public class TheHycrestInsurrectionMapScript : EventBaseContentMapScript, IUpdate
    {
        public override uint PublicEventId => 419;
        protected override IEnumerable<uint> AdditionalPublicEventIds => [418];
        protected override bool CreateLinkedChildEvents => false;

        public override void OnPublicEventFinish(IPublicEvent finished, IPublicEventTeam winner)
        {
            if (ReferenceEquals(finished.Map, map))
            {
                if (finished.Id == 418 && winner?.Team == PublicEventTeam.PublicTeam)
                    publicEvent?.InvokeScriptCollection<TheHycrestInsurrectionEventScript>(s => s.OnIntroComplete());
                else if (ReferenceEquals(finished, publicEvent))
                    publicEvent.InvokeScriptCollection<TheHycrestInsurrectionEventScript>(s => s.OnParentFinished());
            }
            base.OnPublicEventFinish(finished, winner);
        }

        public override void OnAddToMap(IGridEntity entity)
        {
            if (entity is not IPlayer player) return;
            if (publicEvent?.IsFinalised == false)
                publicEvent.JoinEvent(player, PublicEventTeam.PublicTeam);
            var intro = map.PublicEventManager.GetEvent(418);
            if (intro?.IsFinalised == false)
                intro.JoinEvent(player, PublicEventTeam.PublicTeam);
        }

        public void Update(double lastTick)
        {
            map.PublicEventManager.GetEvent(418)?.InvokeScriptCollection<TheHycrestInsurrectionIntroEventScript>(s => s.Update(lastTick));
            publicEvent?.InvokeScriptCollection<TheHycrestInsurrectionEventScript>(s => s.Update(lastTick));
        }
    }
}
