using System.Text;

namespace NexusForever.GameTable
{
    public static class Extensions
    {
        public static string ReadWideString(this BinaryReader reader)
        {
            var bytes = new List<byte>();

            while (true)
            {
                // Read UTF-16LE code units independently of the reader's encoding,
                // then decode together so supplementary characters remain intact.
                ushort codeUnit = reader.ReadUInt16();
                if (codeUnit == 0)
                    return Encoding.Unicode.GetString(bytes.ToArray());

                bytes.Add((byte)codeUnit);
                bytes.Add((byte)(codeUnit >> 8));
            }
        }
    }
}
