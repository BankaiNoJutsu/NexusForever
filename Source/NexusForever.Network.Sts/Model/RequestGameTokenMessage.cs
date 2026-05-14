using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    [Message("/Auth/RequestGameToken")]
    public class RequestGameTokenMessage : IReadable
    {
        public string UserId { get; private set; }
        public string GameCode { get; private set; }
        public string AccountAlias { get; private set; }

        public void Read(XmlDocument document)
        {
            XmlNode rootNode = document["Request"];

            UserId       = rootNode.GetChildValue<string>("UserId");
            GameCode     = rootNode.GetChildValue<string>("GameCode");
            AccountAlias = rootNode.GetChildValue<string>("AccountAlias");
        }
    }
}
