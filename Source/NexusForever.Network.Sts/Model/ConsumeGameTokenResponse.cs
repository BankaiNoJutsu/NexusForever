using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    public class ConsumeGameTokenResponse : IWritable
    {
        public string GameAccountId { get; set; }
        public string LoginName { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public uint UserCenter { get; set; }
        public List<uint> RoleIds { get; } = new();

        public void Write(XmlWriter writer)
        {
            writer.WriteStartElement("Reply");

            writer.WriteStartElement("GameAccountId");
            writer.WriteString(GameAccountId ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("LoginName");
            writer.WriteString(LoginName ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("UserId");
            writer.WriteString(UserId ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("UserName");
            writer.WriteString(UserName ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("UserCenter");
            writer.WriteValue(UserCenter);
            writer.WriteEndElement();

            if (RoleIds.Count != 0)
            {
                writer.WriteStartElement("Roles");

                foreach (uint roleId in RoleIds)
                {
                    writer.WriteStartElement("RoleId");
                    writer.WriteValue(roleId);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}
