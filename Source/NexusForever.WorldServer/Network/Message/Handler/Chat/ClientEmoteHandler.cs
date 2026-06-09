using NexusForever.Game.Static.Entity;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Chat
{
    public class ClientEmoteHandler : IMessageHandler<IWorldSession, ClientEmote>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientEmoteHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientEmote emote)
        {
            StandState standState = StandState.Stand;
            if (emote.EmoteId != 0)
            {
                EmotesEntry entry = gameTableManager.Emotes?.GetEntry(emote.EmoteId);
                if (entry == null)
                    throw new InvalidPacketValueException("HandleEmote: Invalid EmoteId");

                standState = entry.StandState;
            }

            if (emote.EmoteId == 0 && session.Player.IsSitting)
                session.Player.Unsit();

            session.Player.EnqueueToVisible(new ServerEmote
            {
                Guid       = session.Player.Guid,
                StandState = standState,
                EmoteId    = emote.EmoteId
            });

            UpdateTargetedEmoteAchievements(session, emote);
        }

        private static void UpdateTargetedEmoteAchievements(IWorldSession session, ClientEmote emote)
        {
            if (!emote.Targeted || emote.TargetUnitId == 0u)
                return;

            IWorldEntity target = session.Player.GetVisible<IWorldEntity>(emote.TargetUnitId);
            if (target == null)
                return;

            session.Player.AchievementManager.CheckAchievements(session.Player, AchievementType.EmoteTargetCreature, target.CreatureId, emote.EmoteId);
        }
    }
}
