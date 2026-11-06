using System.Net.Sockets;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Shared;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Cinematic;

// Existing cinematic actor/camera/keyframe writers accept IGameSession. This
// adapter keeps their individual packets bound to one original playback.
internal sealed class CinematicPlaybackSession(IGameSession session, Func<bool> canPublish) : IGameSession
{
    public string Id => session.Id;
    public EventQueue Events => session.Events;
    public SocketHeartbeat Heartbeat => session.Heartbeat;
    public bool CanProcessIncomingPackets { get => session.CanProcessIncomingPackets; set => session.CanProcessIncomingPackets = value; }
    public bool CanProcessOutgoingPackets { get => session.CanProcessOutgoingPackets; set => session.CanProcessOutgoingPackets = value; }
    public bool CanDispose() => session.CanDispose();
    public void ForceDisconnect() => session.ForceDisconnect();
    public void OnAccept(Socket socket) => session.OnAccept(socket);
    public void UpdateId(string id) => session.UpdateId(id);
    public void Update(double tick) => session.Update(tick);
    public void HandlePacket(ClientGamePacket packet) => session.HandlePacket(packet);
    public void FlushPackets() => session.FlushPackets();
    public void EnqueueMessage(IWritable message) { if (canPublish()) session.EnqueueMessage(message); }
    public void EnqueueMessageEncrypted(IWritable message) { if (canPublish()) session.EnqueueMessageEncrypted(message); }
    public void EnqueueMessageEncrypted(uint opcode, string hex) { if (canPublish()) session.EnqueueMessageEncrypted(opcode, hex); }
}
