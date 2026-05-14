using System.Xml;

namespace NexusForever.Network.Sts
{
    public static class Extensions
    {
        public static T GetValue<T>(this XmlNode node)
        {
            if (node == null)
                return default;

            if (node.NodeType != XmlNodeType.Element)
                return default;

            XmlNode valueNode = node.FirstChild;
            if (valueNode == null || string.IsNullOrEmpty(valueNode.Value))
                return default;

            Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (targetType == typeof(bool))
            {
                if (bool.TryParse(valueNode.Value, out bool boolValue))
                    return (T)(object)boolValue;

                if (int.TryParse(valueNode.Value, out int intValue))
                    return (T)(object)(intValue != 0);

                return default;
            }

            return (T)Convert.ChangeType(valueNode.Value, targetType);
        }

        public static T GetChildValue<T>(this XmlNode node, string name)
        {
            if (node == null)
                return default;

            return node[name].GetValue<T>();
        }

        public static T? GetOptionalChildValue<T>(this XmlNode node, string name) where T : struct
        {
            if (node == null || node[name] == null)
                return null;

            return node[name].GetValue<T>();
        }
    }
}
