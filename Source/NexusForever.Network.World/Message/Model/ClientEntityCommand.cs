using NexusForever.Game.Static.Entity.Movement.Command;
using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;

namespace NexusForever.Network.World.Message.Model
{
    [Message(GameMessageOpcode.ClientEntityCommand)]
    public class ClientEntityCommand : IReadable
    {
        private readonly IEntityCommandManager entityCommandManager;

        public ClientEntityCommand(
            IEntityCommandManager entityCommandManager = null)
        {
            this.entityCommandManager = entityCommandManager;
        }

        /// <summary>
        /// Runtime movement input envelope consumed by <c>MovementManager.HandleClientEntityCommands</c>.
        /// The packet carries one client clock value, one command count, and then repeated
        /// 5-bit command ids followed by command-specific payloads.
        /// </summary>
        public uint Time { get; set; }
        public List<INetworkEntityCommand> Commands { get; } = new();

        public void Read(GamePacketReader reader)
        {
            if (entityCommandManager == null)
                throw new InvalidOperationException("ClientEntityCommand requires an IEntityCommandManager to read command payloads.");

            Time = reader.ReadUInt();

            uint commandCount = reader.ReadUInt();
            for (uint i = 0u; i < commandCount; i++)
            {
                EntityCommand command     = reader.ReadEnum<EntityCommand>(5);
                IEntityCommandModel model = entityCommandManager.NewEntityCommand(command);
                if (model == null)
                    throw new InvalidPacketValueException($"Unsupported entity command {command}.");

                model.Read(reader);
                Commands.Add(new NetworkEntityCommand
                {
                    Command = command,
                    Model   = model
                });
            }
        }
    }
}
