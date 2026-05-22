using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class Client0x003DHandler : IMessageHandler<IWorldSession, Client0x003D>
    {
        private readonly ILogger<Client0x003DHandler> log;

        public Client0x003DHandler(ILogger<Client0x003DHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x003D message)
        {
            ClientUnresolvedDiagnosticLog.LogPayload(log, session, nameof(Client0x003D), message.Payload);
        }
    }

    public class Client0x00C8Handler : IMessageHandler<IWorldSession, Client0x00C8>
    {
        private readonly ILogger<Client0x00C8Handler> log;

        public Client0x00C8Handler(ILogger<Client0x00C8Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x00C8 message)
        {
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x00C8), message.Value);

            // Conservative decode attempt: treat the uint32 payload as a challenge id for share-with-target.
            // Native owner remains unverified; only active challenges are accepted.
            if (session.Player == null || message.Value == 0u || message.Value > ushort.MaxValue)
                return;

            ushort challengeId = (ushort)message.Value;
            session.Player.ChallengeManager.ShareWithTarget(challengeId);
        }
    }

    public class Client0x00EDHandler : IMessageHandler<IWorldSession, Client0x00ED>
    {
        private readonly ILogger<Client0x00EDHandler> log;

        public Client0x00EDHandler(ILogger<Client0x00EDHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x00ED message)
        {
            ClientUnresolvedDiagnosticLog.LogPayload(log, session, nameof(Client0x00ED), message.Payload);
        }
    }

    public class Client0x011BHandler : IMessageHandler<IWorldSession, Client0x011B>
    {
        private readonly ILogger<Client0x011BHandler> log;

        public Client0x011BHandler(ILogger<Client0x011BHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x011B message)
        {
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x011B), message.Value);
        }
    }

    public class Client0x011DHandler : IMessageHandler<IWorldSession, Client0x011D>
    {
        private readonly ILogger<Client0x011DHandler> log;

        public Client0x011DHandler(ILogger<Client0x011DHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x011D message)
        {
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x011D), message.Value);
        }
    }

    public class Client0x012DHandler : IMessageHandler<IWorldSession, Client0x012D>
    {
        private readonly ILogger<Client0x012DHandler> log;

        public Client0x012DHandler(ILogger<Client0x012DHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x012D message)
        {
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x012D), message.Value);
        }
    }

    public class Client0x0142Handler : IMessageHandler<IWorldSession, Client0x0142>
    {
        private readonly ILogger<Client0x0142Handler> log;

        public Client0x0142Handler(ILogger<Client0x0142Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x0142 message)
        {
            ClientUnresolvedDiagnosticLog.LogPayload(log, session, nameof(Client0x0142), message.Payload);
        }
    }

    public class Client0x0550Handler : IMessageHandler<IWorldSession, Client0x0550>
    {
        private readonly ILogger<Client0x0550Handler> log;

        public Client0x0550Handler(ILogger<Client0x0550Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x0550 message)
        {
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x0550), message.Value);
        }
    }

    public class Client0x062AHandler : IMessageHandler<IWorldSession, Client0x062A>
    {
        private readonly ILogger<Client0x062AHandler> log;

        public Client0x062AHandler(ILogger<Client0x062AHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x062A message)
        {
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x062A), message.Value);
        }
    }

    public class Client0x0634Handler : IMessageHandler<IWorldSession, Client0x0634>
    {
        private readonly ILogger<Client0x0634Handler> log;

        public Client0x0634Handler(ILogger<Client0x0634Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x0634 message)
        {
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x0634), message.Value);
        }
    }

    public class Client0x063EHandler : IMessageHandler<IWorldSession, Client0x063E>
    {
        private readonly ILogger<Client0x063EHandler> log;

        public Client0x063EHandler(ILogger<Client0x063EHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x063E message)
        {
            log.LogDebug("Captured unresolved client opcode {Opcode} from player {PlayerGuid}: text length {Length}, text {Text}.",
                nameof(Client0x063E), session.Player?.Guid, message.Text?.Length ?? 0, message.Text);
        }
    }

    public class Client0x0701Handler : IMessageHandler<IWorldSession, Client0x0701>
    {
        private readonly ILogger<Client0x0701Handler> log;

        public Client0x0701Handler(ILogger<Client0x0701Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x0701 message)
        {
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x0701), message.Value);
        }
    }

    public class Client0x0760Handler : IMessageHandler<IWorldSession, Client0x0760>
    {
        private readonly ILogger<Client0x0760Handler> log;

        public Client0x0760Handler(ILogger<Client0x0760Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x0760 message)
        {
            ClientUnresolvedDiagnosticLog.LogPayload(log, session, nameof(Client0x0760), message.Payload);
        }
    }

    public class Client0x0762Handler : IMessageHandler<IWorldSession, Client0x0762>
    {
        private readonly ILogger<Client0x0762Handler> log;

        public Client0x0762Handler(ILogger<Client0x0762Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x0762 message)
        {
            ClientUnresolvedDiagnosticLog.LogPayload(log, session, nameof(Client0x0762), message.Payload);
        }
    }

    public class Client0x07B6Handler : IMessageHandler<IWorldSession, Client0x07B6>
    {
        private readonly ILogger<Client0x07B6Handler> log;

        public Client0x07B6Handler(ILogger<Client0x07B6Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x07B6 message)
        {
            ClientUnresolvedDiagnosticLog.LogPayload(log, session, nameof(Client0x07B6), message.Payload);
        }
    }

    public class Client0x07E3Handler : IMessageHandler<IWorldSession, Client0x07E3>
    {
        private readonly ILogger<Client0x07E3Handler> log;

        public Client0x07E3Handler(ILogger<Client0x07E3Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x07E3 message)
        {
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x07E3), message.Value);
        }
    }

    public class Client0x0928Handler : IMessageHandler<IWorldSession, Client0x0928>
    {
        private readonly ILogger<Client0x0928Handler> log;

        public Client0x0928Handler(ILogger<Client0x0928Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, Client0x0928 message)
        {
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x0928), message.Value);
        }
    }

    internal static class ClientUnresolvedDiagnosticLog
    {
        public static void LogPayload(ILogger log, IWorldSession session, string opcode, byte[] payload)
        {
            log.LogDebug("Captured unresolved client opcode {Opcode} from player {PlayerGuid}: payload length {Length}.",
                opcode, session.Player?.Guid, payload?.Length ?? 0);
        }

        public static void LogValue(ILogger log, IWorldSession session, string opcode, uint value)
        {
            log.LogDebug("Captured unresolved client opcode {Opcode} from player {PlayerGuid}: value {Value}.",
                opcode, session.Player?.Guid, value);
        }

        public static void LogValue(ILogger log, IWorldSession session, string opcode, ulong value)
        {
            log.LogDebug("Captured unresolved client opcode {Opcode} from player {PlayerGuid}: value {Value}.",
                opcode, session.Player?.Guid, value);
        }
    }
}
