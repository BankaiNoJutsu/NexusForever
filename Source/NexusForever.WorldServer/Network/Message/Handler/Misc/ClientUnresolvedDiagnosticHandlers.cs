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
            log.LogDebug("Captured unresolved client opcode {Opcode} from player {PlayerGuid}: leadingValue {LeadingValue}, trailingValue {TrailingValue}, text {Text}, finalValue {FinalValue}.",
                nameof(Client0x003D), session.Player?.Guid, message.LeadingValue, message.TrailingValue,
                message.Text, message.FinalValue);
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
            ClientUnresolvedDiagnosticLog.LogValue(log, session, nameof(Client0x00C8), (uint)message.MatchType);
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
            log.LogDebug("Captured unresolved client opcode {Opcode} from player {PlayerGuid}: value0 {Value0}, value1 {Value1}, value2 {Value2}, value3 {Value3}, value4 {Value4}, value5 {Value5}.",
                nameof(Client0x00ED), session.Player?.Guid, message.Value0, message.Value1, message.Value2,
                message.Value3, message.Value4, message.Value5);
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
            log.LogDebug("Captured unresolved client opcode {Opcode} from player {PlayerGuid}: empty payload.",
                nameof(Client0x011B), session.Player?.Guid);
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
            log.LogDebug("Captured unresolved client opcode {Opcode} from player {PlayerGuid}: text length {Length}, text {Text}.",
                nameof(Client0x012D), session.Player?.Guid, message.Text?.Length ?? 0, message.Text);
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
            log.LogDebug("Client0x0701: player={PlayerGuid} leadingBits={LeadingBits} trailingValue={TrailingValue}.",
                session.Player?.Guid, message.LeadingBits, message.TrailingValue);
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
            log.LogDebug("Client0x0760: player={PlayerGuid} realmId={RealmId} realmName={RealmName}.",
                session.Player?.Guid, message.Realm.RealmId, message.Realm.RealmName);
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
            log.LogDebug("Client0x0762: player={PlayerGuid} index={Index} messageCount={MessageCount}.",
                session.Player?.Guid, message.MessageRow.Index, message.MessageRow.Messages.Count);
        }
    }

    public class ClientAddonModuleListHandler : IMessageHandler<IWorldSession, ClientAddonModuleList>
    {
        private readonly ILogger<ClientAddonModuleListHandler> log;

        public ClientAddonModuleListHandler(ILogger<ClientAddonModuleListHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAddonModuleList message)
        {
            log.LogDebug("Captured unresolved client opcode {Opcode} from player {PlayerGuid}: header0 {HeaderValue0}, header1 {HeaderValue1}, header2 {HeaderValue2}, header3 {HeaderValue3}, moduleCount {ModuleCount}, rowCount {RowCount}.",
                nameof(ClientAddonModuleList), session.Player?.Guid, message.HeaderValue0, message.HeaderValue1, message.HeaderValue2,
                message.HeaderValue3, message.ModuleCount, message.Modules.Count);
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
            log.LogDebug("Client0x0928: player={PlayerGuid} leadingValue={LeadingValue} trailingBits={TrailingBits}.",
                session.Player?.Guid, message.LeadingValue, message.TrailingBits);
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
