using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    [Message("/Auth/LoginFinish")]
    public class ClientLoginFinishMessage : IReadable
    {
        public string LongTermSession { get; private set; }
        public string SecondaryAuthToken { get; private set; }
        public bool RegisterVerifiedIp { get; private set; }

        public void Read(XmlDocument document)
        {
            XmlNode rootNode = document["Request"];

            LongTermSession   = rootNode.GetChildValue<string>("LongTermSession");
            SecondaryAuthToken = rootNode.GetChildValue<string>("SecondaryAuthToken");
            RegisterVerifiedIp = rootNode.GetChildValue<bool>("RegisterVerifiedIp");
        }
    }
}
