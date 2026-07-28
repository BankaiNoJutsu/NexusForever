using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Chat
{
    public class ClientEmoteHandler : IMessageHandler<IWorldSession, ClientEmote>
    {
        public void HandleMessage(IWorldSession session, ClientEmote emote)
        {
            if (emote.EmoteId == 0 && session.Player.IsSitting)
                session.Player.Unsit();

            session.Player.Emote(emote.EmoteId);

            session.Player.EnqueueToVisible(new ServerEmote
            {
                EmotesId     = emote.EmoteId,
                Seed         = emote.Seed,
                SourceUnitId = session.Player.Guid,
                TargetUnitId = emote.TargetUnitId,
                Targeted     = emote.Targeted,
                Silent       = emote.Silent
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
