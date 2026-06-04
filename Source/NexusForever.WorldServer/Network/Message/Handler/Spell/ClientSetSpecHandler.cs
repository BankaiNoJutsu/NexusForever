using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientSetSpecHandler : IMessageHandler<IWorldSession, ClientSetSpec>
    {
        public void HandleMessage(IWorldSession session, ClientSetSpec changeActiveActionSet)
        {
            SpecError specError = session.Player.SpellManager.SetActiveActionSet(changeActiveActionSet.SpecIndex);
            session.EnqueueMessageEncrypted(new ServerSpecChanged
            {
                SpecError      = specError,
                ActionSetIndex = session.Player.SpellManager.ActiveActionSet
            });

            session.Player.SpellManager.SendServerAbilityPoints();
            if (specError == SpecError.Ok)
                session.Player.RequestSave();
        }
    }
}
