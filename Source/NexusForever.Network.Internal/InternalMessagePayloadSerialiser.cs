using System.Text.Json;

namespace NexusForever.Network.Internal
{
    public readonly record struct InternalMessagePayload(string Type, string Payload);

    public static class InternalMessagePayloadSerialiser
    {
        public static async Task<InternalMessagePayload> SerialiseAsync(object message)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, message, message.GetType());

            stream.Position = 0;
            using var reader = new StreamReader(stream);
            string payload = await reader.ReadToEndAsync();

            return new InternalMessagePayload(message.GetType().AssemblyQualifiedName, payload);
        }
    }
}
