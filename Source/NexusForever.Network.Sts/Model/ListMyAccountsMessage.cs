using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    [Message("/GameAccount/ListMyAccounts")]
    public class ListMyAccountsMessage : IReadable
    {
        public string UserId { get; private set; }
        public string GameCode { get; private set; }

        public void Read(XmlDocument document)
        {
            XmlNode rootNode = document["Request"];

            UserId   = rootNode.GetChildValue<string>("UserId");
            GameCode = rootNode.GetChildValue<string>("GameCode");
        }
    }
}
