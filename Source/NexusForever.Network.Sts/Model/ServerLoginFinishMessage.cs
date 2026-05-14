using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    public class ServerLoginFinishMessage : IWritable
    {
        public uint AuthType { get; set; }
        public string LocationId { get; set; }
        public string UserId { get; set; }
        public uint UserCenter { get; set; }
        public string UserName { get; set; }
        public long AccessMask { get; set; }
        public List<uint> RoleIds { get; } = new();
        public List<string> Aliases { get; } = new();

        // Status
        // ExternalAccount
        // PcCafe

        public void Write(XmlWriter writer)
        {
            writer.WriteStartElement("Reply");

            writer.WriteStartElement("AuthType");
            writer.WriteValue(AuthType);
            writer.WriteEndElement();

            writer.WriteStartElement("LocationId");
            writer.WriteString(LocationId ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("UserId");
            writer.WriteString(UserId ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("UserCenter");
            writer.WriteValue(UserCenter);
            writer.WriteEndElement();

            writer.WriteStartElement("UserName");
            writer.WriteString(UserName ?? "");
            writer.WriteEndElement();

            writer.WriteStartElement("AccessMask");
            writer.WriteValue(AccessMask);
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

            if (Aliases.Count != 0)
            {
                writer.WriteStartElement("Aliases");

                foreach (string alias in Aliases)
                {
                    writer.WriteStartElement("Alias");
                    writer.WriteString(alias ?? "");
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}
