using NexusForever.Game.Abstract.Entity;
using System.Collections.Immutable;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;

namespace NexusForever.Game.Abstract.Quest
{
    public interface ICommunicatorMessage
    {
        uint Id { get; }
        ushort QuestId { get; }
        bool DeliversQuest { get; }
        CommunicatorMessagesEntry Entry { get; }
        CommunicatorKind Kind { get; }
        bool ServerPushed { get; }
        bool Reachable { get; }
        bool Delivers { get; }
        ImmutableArray<CommunicatorCondition> Conditions { get; }
        ImmutableArray<CommunicatorTrigger> Triggers { get; }
        CommunicatorResult Evaluate(ICommunicatorPlayerView player, CommunicatorGates skip = CommunicatorGates.None);

        /// <summary>
        /// Checks if <see cref="IPlayer"/> meets the required conditions for this quest to be added to their communicator.
        /// </summary>
        bool Meets(IPlayer player);

        /// <summary>
        /// Send communicator message to <see cref="IGameSession"/>.
        /// </summary>
        void Send(IGameSession session);
        void Send(IGameSession session, bool checkConditions);
    }
}