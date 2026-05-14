using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    public class ListMyAccountsResponse : IWritable
    {
        public string Alias { get; set; }
        public string Created { get; set; }
        public string GameAccountId { get; set; }

        public void Write(XmlWriter writer)
        {
            writer.WriteStartElement("Reply");
            writer.WriteAttributeString("type", "array");

            writer.WriteStartElement("GameAccount");

            writer.WriteStartElement("Alias");
            writer.WriteString(Alias ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("Created");
            writer.WriteString(Created ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("GameAccountId");
            writer.WriteString(GameAccountId ?? "");
            writer.WriteEndElement();

            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
