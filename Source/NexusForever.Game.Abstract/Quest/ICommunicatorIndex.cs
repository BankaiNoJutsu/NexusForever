using System.Collections.Immutable;
namespace NexusForever.Game.Abstract.Quest
{
    public interface ICommunicatorIndex
    {
        ImmutableDictionary<uint, ICommunicatorMessage> Messages { get; }
        ImmutableArray<ICommunicatorMessage> WorldArmed { get; }
        ImmutableArray<CommunicatorArm> GetTriggers(ushort questId);
    }
}
