using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Cryptography;
using NexusForever.Network.Diagnostics;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Shared;

using NexusForever.Shared.Diagnostics;

namespace NexusForever.Network.Session
{
    public abstract class GameSession : NetworkSession, IGameSession
    {
        /// <summary>
        /// Determines if queued incoming packets can be processed during a world update.
        /// </summary>
        public bool CanProcessIncomingPackets { get; set; } = true;

        /// <summary>
        /// Determines if queued outgoing packets can be processed during a world update.
        /// </summary>
        public bool CanProcessOutgoingPackets { get; set; } = true;

        protected PacketCrypt encryption;

        private FragmentedBuffer onDeck;
        private readonly ConcurrentQueue<ClientGamePacket> incomingPackets = new();
        private readonly ConcurrentQueue<ServerGamePacket> outgoingPackets = new();

        #region Dependency Injection

        private readonly IMessageManager messageManager;
        private readonly IServiceProvider serviceProvider;

        public GameSession(
            IMessageManager messageManager,
            IServiceProvider serviceProvider)
        {
            this.messageManager = messageManager;
            this.serviceProvider = serviceProvider;
        }

        #endregion

        /// <summary>
        /// Enqueue <see cref="IWritable"/> to be sent to the client.
        /// </summary>
        public void EnqueueMessage(IWritable message)
        {
            GameMessageOpcode? opcode = messageManager.GetOpcode(message);
            if (opcode == null)
            {
                log.Warn("Failed to send message with no attribute!");
                return;
            }

            if (opcode != GameMessageOpcode.ServerAuthEncrypted
                && opcode != GameMessageOpcode.ServerRealmEncrypted)
                log.Trace($"Sent packet {opcode}(0x{opcode:X}).");

            var packet = new ServerGamePacket(opcode.Value, message);
            outgoingPackets.Enqueue(packet);
            NexusForeverDiagnostics.RecordPacketQueueLength("world", "outgoing", outgoingPackets.Count);
            LogSpellPacketBoundary("outgoing-queued", opcode.Value, false, message.GetType().Name, packet.Data.Length);
        }

        /// <summary>
        /// Enqueue <see cref="IWritable"/> to be sent encrypted to the client.
        /// </summary>
        public void EnqueueMessageEncrypted(IWritable message)
        {
            GameMessageOpcode? opcode = messageManager.GetOpcode(message);
            if (opcode == null)
            {
                log.Warn("Failed to send message with no attribute!");
                return;
            }

            using (var stream = new MemoryStream())
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write(opcode.Value, 16);
                message.Write(writer);
                writer.FlushBits();

                byte[] data = stream.ToArray();
                PacketEvidenceRecorder.Record(Id, "outgoing-plaintext", opcode.Value, message.GetType().Name, data, true);
                byte[] encrypted = encryption.Encrypt(data, data.Length);
                EnqueueMessage(BuildEncryptedMessage(encrypted));
                LogSpellPacketBoundary("outgoing-encrypted-body", opcode.Value, true, message.GetType().Name, encrypted.Length);
            }

            log.Trace($"Sent packet {opcode}(0x{opcode:X}).");
        }

        public void EnqueueMessageEncrypted(uint opcode, string hex)
        {
            using (var stream = new MemoryStream())
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write(opcode, 16);

                byte[] body = Enumerable.Range(0, hex.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                    .ToArray();
                writer.WriteBytes(body);

                writer.FlushBits();

                byte[] data = stream.ToArray();
                PacketEvidenceRecorder.Record(Id, "outgoing-plaintext-raw", opcode, $"0x{opcode:X4}", "RawHexPayload", data, true);
                byte[] encrypted = encryption.Encrypt(data, data.Length);
                EnqueueMessage(BuildEncryptedMessage(encrypted));
            }
        }

        protected abstract IWritable BuildEncryptedMessage(byte[] data);

        public override void OnAccept(Socket newSocket)
        {
            base.OnAccept(newSocket);

            ulong key = PacketCrypt.GetKeyFromAuthBuildAndMessage();
            encryption = new PacketCrypt(key);
        }

        protected override uint OnData(byte[] buffer, int offset, int count)
        {
            using (var stream = new MemoryStream(buffer, offset, count))
            using (var reader = new GamePacketReader(stream))
            {
                while (stream.Remaining() != 0)
                {
                    // no packet on deck waiting for additional information, new data will be part of a new packet
                    if (onDeck == null)
                    {
                        if (stream.Remaining() < sizeof(uint))
                        {
                            // we don't have enough data to know the length of the next packet
                            // return the remaining buffer so new data can be appended
                            return stream.Remaining();
                        }

                        uint size = reader.ReadUInt();
                        onDeck = new FragmentedBuffer(size - sizeof(uint));
                    }

                    onDeck.Populate(reader);
                    if (onDeck.IsComplete)
                    {
                        incomingPackets.Enqueue(new ClientGamePacket
                        {
                            Data = onDeck.Data,
                            IsEncrypted = false,
                            QueuedTimestamp = NexusForeverDiagnostics.GetTimestamp()
                        });
                        NexusForeverDiagnostics.RecordPacketQueueLength("world", "incoming", incomingPackets.Count);
                        onDeck = null;
                    }
                }
            }

            return 0u;
        }

        protected override void OnDisconnect()
        {
            base.OnDisconnect();

            // clear any pending packets and prevent any new packets from being processed
            CanProcessIncomingPackets = false;
            CanProcessOutgoingPackets = false;
            incomingPackets.Clear();
            outgoingPackets.Clear();
        }

        public override void Update(double lastTick)
        {
            base.Update(lastTick);

            // process pending packet queue
            while (CanProcessIncomingPackets && incomingPackets.TryDequeue(out ClientGamePacket packet))
            {
                NexusForeverDiagnostics.RecordPacketQueueLength("world", "incoming", incomingPackets.Count);
                HandlePacket(packet);
            }

            // flush pending packet queue
            FlushPackets();
        }

        public void HandlePacket(ClientGamePacket packet)
        {
            try
            {
                //using IServiceScope serviceScope = CreateHandlePacketScope();

                using var reader = new ClientGamePacketReader();
                reader.Initialise(packet, encryption);
                GameMessageOpcode opcode = reader.ReadHeader();
                if (packet.QueuedTimestamp != 0)
                    NexusForeverDiagnostics.RecordPacketQueueWait("world", opcode.ToString(), NexusForeverDiagnostics.GetElapsedMilliseconds(packet.QueuedTimestamp));

                //IReadable message = serviceScope.ServiceProvider.GetKeyedService<IReadable>(opcode);
                IReadable message = serviceProvider.GetKeyedService<IReadable>(opcode);
                LogSpellPacketBoundary("incoming-read", opcode, packet.IsEncrypted, message?.GetType().Name, packet.Data?.Length ?? 0);
                PacketEvidenceRecorder.Record(
                    Id,
                    packet.IsEncrypted ? "incoming-decrypted" : "incoming-plaintext",
                    opcode,
                    message?.GetType().Name,
                    reader.PlaintextData,
                    packet.IsEncrypted);
                if (message == null)
                {
                    log.Warn($"Received unknown packet {opcode}(0x{opcode:X}).");
                    return;
                }

                Type handlerType = messageManager.GetMessageHandlerType(opcode);
                if (handlerType == null)
                {
                    log.Warn($"Received unhandled packet {opcode}(0x{opcode:X}).");
                    return;
                }

                //object handler = serviceScope.ServiceProvider.GetService(handlerType);
                object handler = serviceProvider.GetService(handlerType);
                if (handler == null)
                {
                    log.Warn($"Received unhandled packet {opcode}(0x{opcode:X}).");
                    return;
                }

                MessageHandlerDelegate handlerDelegate = messageManager.GetMessageHandlerDelegate(opcode);
                if (handlerDelegate == null)
                {
                    log.Warn($"Received unhandled packet {opcode}(0x{opcode:X}).");
                    return;
                }

                if (opcode != GameMessageOpcode.ClientEncrypted
                    && opcode != GameMessageOpcode.ClientPacked
                    && opcode != GameMessageOpcode.ClientPackedWorld
                    && opcode != GameMessageOpcode.ClientEntityCommand)
                    log.Trace($"Received packet {opcode}(0x{opcode:X}).");

                uint remaining = reader.ReadBody(message);
                if (remaining > 0)
                    log.Warn($"Failed to read entire contents of packet {opcode}");

                long handlerStart = NexusForeverDiagnostics.GetTimestamp();
                handlerDelegate.Invoke(handler, this, message);
                NexusForeverDiagnostics.RecordPacketHandler("world", opcode.ToString(), handlerType.Name, NexusForeverDiagnostics.GetElapsedMilliseconds(handlerStart));
            }
            catch (InvalidPacketValueException exception)
            {
                log.Error(exception);
                ForceDisconnect();
            }
            catch (Exception exception)
            {
                log.Error(exception);
                ForceDisconnect();
            }
        }

        private void LogSpellPacketBoundary(string direction, GameMessageOpcode opcode, bool encrypted, string messageType, int bodyBytes)
        {
            if (!log.IsTraceEnabled || !IsSpellEvidenceOpcode(opcode))
                return;

            log.Trace(
                "SpellDiagnostics packet-boundary session={0} direction={1} encrypted={2} opcode={3}(0x{4:X}) messageType={5} incomingQueue={6} outgoingQueue={7} bodyBytes={8}",
                Id,
                direction,
                encrypted,
                opcode,
                (uint)opcode,
                messageType,
                incomingPackets.Count,
                outgoingPackets.Count,
                bodyBytes);
        }

        private static bool IsSpellEvidenceOpcode(GameMessageOpcode opcode)
        {
            return opcode is GameMessageOpcode.ClientCastSpell
                or GameMessageOpcode.ClientCastSpellContinuous
                or GameMessageOpcode.ClientEntityCommand
                or GameMessageOpcode.ClientSpellStopCast
                or GameMessageOpcode.ClientCancelEffect
                or GameMessageOpcode.ServerCombatLog
                or GameMessageOpcode.ServerSpellGo
                or GameMessageOpcode.ServerSpellStart
                or GameMessageOpcode.ServerSpellCastResult
                or GameMessageOpcode.ServerSpellFinish
                or GameMessageOpcode.ServerSpellBuffRemove
                || ((uint)opcode >= 0x07F4u && (uint)opcode <= 0x0819u);
        }

        protected virtual IServiceScope CreateHandlePacketScope()
        {
            return serviceProvider.CreateScope();
        }

        /// <summary>
        /// Flush all pending packets to the client.
        /// </summary>
        public void FlushPackets()
        {
            while (CanProcessOutgoingPackets && outgoingPackets.TryDequeue(out ServerGamePacket packet))
            {
                NexusForeverDiagnostics.RecordPacketQueueLength("world", "outgoing", outgoingPackets.Count);
                if (!FlushPacket(packet))
                {
                    outgoingPackets.Clear();
                    break;
                }
            }
        }

        private bool FlushPacket(ServerGamePacket packet)
        {
            long start = NexusForeverDiagnostics.GetTimestamp();
            int wireLength = checked((int)packet.Size);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(wireLength);
            try
            {
                using (var stream = new MemoryStream(buffer, 0, wireLength, writable: true, publiclyVisible: true))
                using (var writer = new GamePacketWriter(stream))
                {
                    writer.Write(packet.Size);
                    writer.Write(packet.Opcode, 16);
                    writer.WriteBytes(packet.Data);
                    writer.FlushBits();

                    if (!SendRaw(buffer, 0, (int)stream.Position))
                        return false;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            NexusForeverDiagnostics.RecordPacketFlush("world", packet.Opcode.ToString(), packet.Data.Length, NexusForeverDiagnostics.GetElapsedMilliseconds(start));
            return true;
        }
    }
}
