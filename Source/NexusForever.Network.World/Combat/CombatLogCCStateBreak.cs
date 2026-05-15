using NexusForever.Game.Static.Combat;
using NexusForever.Game.Static.Combat.CrowdControl;

namespace NexusForever.Network.World.Combat
{
    public class CombatLogCCStateBreak : ICombatLog
    {
        public CombatLogType Type => CombatLogType.CCStateBreak;

        // Payload-backed via CombatLog_WriteCasterContext (unitCaster).
        public uint CasterId { get; set; }

        // Payload-backed eState (5-bit enum); strState is client-local display derivation.
        public CCState State { get; set; } // 5u

        public void Write(GamePacketWriter writer)
        {
            writer.Write(CasterId);
            writer.Write(State, 5u);
        }
    }
}
