using NexusForever.Game.Static.Challenges;

namespace NexusForever.Game.Abstract.Challenges
{
    public interface IChallengeManager
    {
        void SendInitialPackets();

        void Update(double lastTick);

        void HandleChoice(ushort challengeId, ChallengeChoice choice);

        /// <summary>
        /// Share an active challenge with the player's current target, mirroring quest-share targeting.
        /// </summary>
        void ShareWithTarget(ushort challengeId);

        /// <summary>
        /// Receive a shared-challenge invitation from another player.
        /// </summary>
        void ReceiveShare(ushort challengeId, uint sharerUnitId);
    }
}
