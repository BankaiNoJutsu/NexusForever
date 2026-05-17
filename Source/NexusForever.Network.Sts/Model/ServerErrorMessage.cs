using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    public class ServerErrorMessage : IWritable
    {
        public int Code { get; }
        public string Server { get; }
        public string Module { get; }
        public int Line { get; }
        public string Text { get; }

        public ServerErrorMessage(int code, string server = "sts", string module = "authentication", int line = 0, string text = null)
        {
            Code   = code;
            Server = server;
            Module = module;
            Line   = line;
            Text   = text ?? code.ToString();
        }

        public void Write(XmlWriter writer)
        {   
            writer.WriteStartElement("Error");

            writer.WriteStartAttribute("code");
            writer.WriteValue(Code);
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("server");
            writer.WriteValue(Server);
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("module");
            writer.WriteValue(Module);
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("line");
            writer.WriteValue(Line);
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("text");
            writer.WriteValue(Text);
            writer.WriteEndAttribute();

            writer.WriteEndElement();
        }
    }
}
