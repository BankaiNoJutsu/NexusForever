using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    [Message("/Auth/PageVerifiedIps")]
    public class AuthPageVerifiedIpsMessage : IReadable
    {
        public uint PageIndex { get; private set; }
        public uint PageSize { get; private set; }
        public string UserId { get; private set; }

        public void Read(XmlDocument document)
        {
            XmlNode rootNode = document["Request"];
            if (rootNode == null)
                return;

            PageIndex = rootNode.GetChildValue<uint>("PageIndex");
            PageSize  = rootNode.GetChildValue<uint>("PageSize");
            UserId    = rootNode.GetChildValue<string>("UserId");
        }
    }

    [Message("/Auth/UnregisterVerifiedIp")]
    public class AuthUnregisterVerifiedIpMessage : IReadable
    {
        public string UserId { get; private set; }
        public List<string> NetAddresses { get; } = [];

        public void Read(XmlDocument document)
        {
            XmlNode rootNode = document["Request"];
            if (rootNode == null)
                return;

            UserId = rootNode.GetChildValue<string>("UserId");

            XmlNode netAddressesNode = rootNode["NetAddresses"];
            if (netAddressesNode == null)
                return;

            foreach (XmlNode netAddressNode in netAddressesNode.SelectNodes("NetAddress"))
            {
                if (!string.IsNullOrWhiteSpace(netAddressNode.InnerText))
                    NetAddresses.Add(netAddressNode.InnerText);
            }
        }
    }

    public class AuthPageVerifiedIpsResponse : IWritable
    {
        public uint TotalPage { get; set; }
        public uint TotalCount { get; set; }

        public void Write(XmlWriter writer)
        {
            writer.WriteStartElement("Reply");

            writer.WriteStartElement("TotalPage");
            writer.WriteValue(TotalPage);
            writer.WriteEndElement();

            writer.WriteStartElement("TotalCount");
            writer.WriteValue(TotalCount);
            writer.WriteEndElement();

            writer.WriteStartElement("Items");
            writer.WriteAttributeString("type", "array");
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
