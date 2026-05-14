using System.Xml;

namespace NexusForever.Network.Sts.Model
{
    [Message("/Sts/Connect")]
    public class ClientConnectMessage : IReadable
    {
        public uint ConnType { get; private set; }
        public string Address { get; private set; }
        public uint? ConnProductType { get; private set; }
        public uint? ConnAppIndex { get; private set; }
        public uint? ConnDeployment { get; private set; }
        public uint? ConnEpoch { get; private set; }
        public uint ProductType { get; private set; }
        public uint AppIndex { get; private set; }
        public uint? Deployment { get; private set; }
        public uint Epoch { get; private set; }
        public uint Program { get; private set; }
        public uint Build { get; private set; }
        public uint Process { get; private set; }
        public uint? NotifyFlags { get; private set; }
        public uint? VersionFlags { get; private set; }

        public void Read(XmlDocument document)
        {
            XmlNode rootNode = document["Connect"];

            ConnType        = rootNode.GetChildValue<uint>("ConnType");
            ConnProductType = rootNode.GetOptionalChildValue<uint>("ConnProductType");
            ConnAppIndex    = rootNode.GetOptionalChildValue<uint>("ConnAppIndex");
            ConnDeployment  = rootNode.GetOptionalChildValue<uint>("ConnDeployment");
            ConnEpoch       = rootNode.GetOptionalChildValue<uint>("ConnEpoch");
            Address         = rootNode.GetChildValue<string>("Address") ?? rootNode.GetChildValue<string>("Location");
            ProductType     = rootNode.GetChildValue<uint>("ProductType");
            AppIndex        = rootNode.GetChildValue<uint>("AppIndex");
            Deployment      = rootNode.GetOptionalChildValue<uint>("Deployment");
            Epoch           = rootNode.GetChildValue<uint>("Epoch");
            Program         = rootNode.GetChildValue<uint>("Program");
            Build           = rootNode.GetChildValue<uint>("Build");
            Process         = rootNode.GetChildValue<uint>("Process");
            NotifyFlags     = rootNode.GetOptionalChildValue<uint>("NotifyFlags");
            VersionFlags    = rootNode.GetOptionalChildValue<uint>("VersionFlags");
        }
    }
}
