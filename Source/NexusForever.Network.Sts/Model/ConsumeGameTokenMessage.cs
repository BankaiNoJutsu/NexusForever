using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    [Message("/Auth/ConsumeGameToken")]
    public class ConsumeGameTokenMessage : IReadable
    {
        public string GameCode { get; private set; }
        public string Token { get; private set; }
        public string ClientNetAddress { get; private set; }

        public void Read(XmlDocument document)
        {
            XmlNode rootNode = document["Request"];

            GameCode         = rootNode.GetChildValue<string>("GameCode");
            Token            = rootNode.GetChildValue<string>("Token");
            ClientNetAddress = rootNode.GetChildValue<string>("ClientNetAddress");
        }
    }
}
