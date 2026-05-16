namespace NexusForever.ClientConnector.Configuration
{
    public class ClientConfiguration
    {
        public string HostName { get; set; }
        public string Language { get; set; }
        public string[] ExtraArguments { get; set; } = System.Array.Empty<string>();
    }
}
