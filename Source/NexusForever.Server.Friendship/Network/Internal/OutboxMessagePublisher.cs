using System.Text.Json;
using NexusForever.Database.Friendship.Model;
using NexusForever.Database.Friendship.Repository;
using NexusForever.Network.Internal;

namespace NexusForever.Server.Friendship.Network.Internal
{
    public class OutboxMessagePublisher : IInternalMessagePublisher
    {
        #region Dependency Injection

        private readonly InternalMessageRepository _repository;

        public OutboxMessagePublisher(
            InternalMessageRepository repository)
        {
            _repository = repository;
        }

        #endregion

        /// <summary>
        /// Publish a message to the internal message broker via an outbox table.
        /// </summary>
        /// <param name="message"></param>
        public async Task PublishAsync(object message)
        {
            InternalMessagePayload payload = await InternalMessagePayloadSerialiser.SerialiseAsync(message);

            _repository.AddMessage(new InternalMessageModel
            {
                Id        = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Type      = payload.Type,
                Payload   = payload.Payload,
            });
        }
    }
}
