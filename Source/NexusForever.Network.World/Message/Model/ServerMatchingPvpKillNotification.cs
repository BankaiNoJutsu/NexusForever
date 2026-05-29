using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Matching;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Shared;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Native reader: <c>ServerMatchingPvpKillNotification_ReadPayload</c> (<c>140099440</c>).
    /// Reads a 2-bit killer opponent type, dispatches the matching opponent payload,
    /// then repeats the same 2-bit type + variant payload for the victim before one
    /// 2-bit victim team field and one 3-bit death reason.
    /// </summary>

    [Message(GameMessageOpcode.ServerMatchingPvpKillNotification)]
    public class ServerMatchingPvpKillNotification : IWritable
    {
        /// <summary>Player-variant opponent payload: identity plus 5-bit class.</summary>
        public class OpponentPlayer : IWritable
        {
            public Identity Identity { get; set; } = new();
            public Class Class { get; set; }

            public void Write(GamePacketWriter writer)
            {
                Identity.Write(writer);
                writer.Write(Class, 5u);
            }
        }

        /// <summary>Creature-variant opponent payload: one uint32 unit id and one 18-bit creature id.</summary>
        public class OpponentCreature : IWritable
        {
            public uint UnitId { get; set; }
            public uint Creature2Id { get; set; }

            public void Write(GamePacketWriter writer)
            {
                writer.Write(UnitId);
                writer.Write(Creature2Id, 18u);
            }
        }

        public OpponentType KillerType { get; set; }
        public OpponentPlayer KillerPlayer { get; set; }
        public OpponentCreature KillerCreature { get; set; }

        public OpponentType VictimType { get; set; }
        public OpponentPlayer VictimPlayer { get; set; }
        public OpponentCreature VictimCreature { get; set; }

        public MatchTeam VictimTeam { get; set; }
        public PvpDeathReason DeathReason { get; set; }

        public void Write(GamePacketWriter writer)
        {
            // Both sides must provide the payload variant implied by their 2-bit opponent type.
            if ( (KillerPlayer == null && KillerCreature == null) || (VictimPlayer == null && VictimCreature == null) )
            {
                throw new InvalidOperationException("Both a Killer and Victim must be created.");
            }

            writer.Write(KillerType, 2u);
            if (KillerType == OpponentType.Player)
            {
                KillerPlayer.Write(writer);
            }
            else if (KillerType == OpponentType.Creature)
            {
                KillerCreature.Write(writer);
            }
            
            writer.Write(VictimType, 2u);
            if (VictimType == OpponentType.Player)
            {
                VictimPlayer.Write(writer);
            }
            else if (VictimType == OpponentType.Creature)
            {
                VictimCreature.Write(writer);
            }

            writer.Write(VictimTeam, 2u);
            writer.Write(DeathReason, 3u);
        }
    }
}
