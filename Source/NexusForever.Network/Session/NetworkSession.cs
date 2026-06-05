using System.Buffers;
using System.Net;
using System.Net.Sockets;
using NexusForever.Network.Session.Static;
using NexusForever.Shared.Game.Events;
using NLog;

namespace NexusForever.Network.Session
{
    public abstract class NetworkSession : INetworkSession
    {
        protected static readonly ILogger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Unique id for <see cref="NetworkSession"/>.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// <see cref="IEvent"/> queue that will be processed during <see cref="NetworkSession"/> update.
        /// </summary>
        public EventQueue Events { get; } = new();

        /// <summary>
        /// Heartbeat to check if <see cref="NetworkSession"/> is still alive.
        /// </summary>
        /// <remarks>
        /// If <see cref="SocketHeartbeat"/> flatlines the <see cref="NetworkSession"/> will be disconnected.
        /// </remarks>
        public SocketHeartbeat Heartbeat { get; } = new();

        private Socket socket;
        private readonly byte[] buffer = new byte[4096];
        private int bufferOffset;

        private DisconnectState? disconnectState;

        /// <summary>
        /// Initialise <see cref="NetworkSession"/> with new <see cref="Socket"/> and begin listening for data.
        /// </summary>
        public virtual void OnAccept(Socket newSocket)
        {
            if (socket != null)
                throw new InvalidOperationException();

            Id = Guid.NewGuid().ToString();

            Events.UnhandledExceptionHandler ??= _ => ForceDisconnect();

            socket = newSocket;
            socket.NoDelay = true;
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveDataCallback, null);

            log.Trace($"New client {Id} connected from {newSocket.RemoteEndPoint}.");
        }

        /// <summary>
        /// Update <see cref="NetworkSession"/> existing id with a new supplied id.
        /// </summary>
        /// <remarks>
        /// This should be used when the default session id can be replaced with a known unique id.
        /// </remarks>
        public void UpdateId(string id)
        {
            log.Trace($"Client {Id} updated id to {id}.");
            Id = id;
        }

        /// <summary>
        /// Invoked each world tick with the delta since the previous tick occurred.
        /// </summary>
        public virtual void Update(double lastTick)
        {
            Events.Update(lastTick);

            if (!disconnectState.HasValue)
                Heartbeat.Update(lastTick);

            // Prevents disconnection process happening again
            if (disconnectState == DisconnectState.Complete || disconnectState == DisconnectState.Processing)
                return;

            if (Heartbeat.Flatline || disconnectState == DisconnectState.Pending)
            {
                // no defibrillator is going to save this session
                if (Heartbeat.Flatline)
                {
                    log.Warn(
                        "Client {0} heartbeat flatlined after {1:R}s without socket activity; disconnecting.",
                        Id,
                        Heartbeat.TimeoutSeconds);
                }

                disconnectState = DisconnectState.Processing;
                OnDisconnect();
            }
        }

        protected virtual void OnDisconnect()
        {
            try
            {
                EndPoint remoteEndPoint = socket.RemoteEndPoint;
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();

                log.Trace($"Client {Id} disconnected. {remoteEndPoint}");
            }
            catch (Exception e)
            {
                log.Error(e, $"An exception occured for client {Id} during socket close!");
            }

            disconnectState = DisconnectState.Complete;
        }

        /// <summary>
        /// Returns if <see cref="NetworkSession"/> can be disposed.
        /// </summary>
        public virtual bool CanDispose()
        {
            return disconnectState == DisconnectState.Complete && !Events.PendingEvents;
        }

        /// <summary>
        /// Invoked with <see cref="IAsyncResult"/> when data from the <see cref="Socket"/> is received.
        /// </summary>
        private void ReceiveDataCallback(IAsyncResult ar)
        {
            try
            {
                int length = socket.EndReceive(ar);
                if (length == 0)
                {
                    log.Debug(
                        "Client {0} closed the socket receive stream; heartbeatRemaining={1:R}s.",
                        Id,
                        Heartbeat.SecondsUntilFlatline);
                    ForceDisconnect();
                    return;
                }

                Heartbeat.OnHeartbeat();

                int dataLength = length + bufferOffset;
                byte[] data = ArrayPool<byte>.Shared.Rent(dataLength);
                try
                {
                    Buffer.BlockCopy(buffer, 0, data, 0, dataLength);
                    bufferOffset = (int)OnData(data, 0, dataLength);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(data);
                }

                // if we have data that wasn't processed move it to the start of the buffer
                // any new data will be amended to it
                if (bufferOffset != 0)
                    Buffer.BlockCopy(buffer, dataLength - bufferOffset, buffer, 0, bufferOffset);

                socket.BeginReceive(buffer, bufferOffset, buffer.Length - bufferOffset, SocketFlags.None, ReceiveDataCallback, null);
            }
            catch (SocketException e) when (e.SocketErrorCode is SocketError.ConnectionAborted or SocketError.ConnectionReset or SocketError.Shutdown or SocketError.OperationAborted)
            {
                log.Debug(
                    e,
                    "Client {0} socket receive ended with {1}; disconnectState={2}, heartbeatRemaining={3:R}s.",
                    Id,
                    e.SocketErrorCode,
                    disconnectState?.ToString() ?? "None",
                    Heartbeat.SecondsUntilFlatline);
                ForceDisconnect();
            }
            catch (Exception e)
            {
                log.Error(e, $"An exception occured for client {Id} during socket read!");
                ForceDisconnect();
            }
        }

        protected abstract uint OnData(byte[] buffer, int offset, int count);

        /// <summary>
        /// Send supplied data to remote client on <see cref="Socket"/>.
        /// </summary>
        protected bool SendRaw(byte[] data)
        {
            return SendRaw(data, 0, data.Length);
        }

        /// <summary>
        /// Send a segment of supplied data to remote client on <see cref="Socket"/>.
        /// </summary>
        protected bool SendRaw(byte[] data, int offset, int count)
        {
            try
            {
                while (count > 0)
                {
                    int sent = socket.Send(data, offset, count, SocketFlags.None);
                    if (sent <= 0)
                        throw new SocketException((int)SocketError.ConnectionReset);

                    offset += sent;
                    count  -= sent;
                }

                return true;
            }
            catch (SocketException e) when (e.SocketErrorCode is SocketError.ConnectionAborted or SocketError.ConnectionReset or SocketError.Shutdown)
            {
                log.Debug(e, $"Client {Id} disconnected during socket send.");
                ForceDisconnect();
                return false;
            }
            catch (Exception e)
            {
                log.Error(e, $"An exception occured for client {Id} during socket send!");
                ForceDisconnect();
                return false;
            }
        }

        /// <summary>
        /// Forece disconnect of <see cref="NetworkSession"/>.
        /// </summary>
        public void ForceDisconnect()
        {
            if (disconnectState.HasValue)
                return;

            disconnectState = DisconnectState.Pending;
        }
    }
}
