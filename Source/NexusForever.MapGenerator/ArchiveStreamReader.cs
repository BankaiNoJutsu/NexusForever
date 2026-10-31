using System;
using System.IO;
using System.IO.Compression;
using Nexus.Archive;
using SharpCompress.Compressors.LZMA;

namespace NexusForever.MapGenerator
{
    internal static class ArchiveStreamReader
    {
        public static Stream OpenFileStream(Archive archive, IArchiveFileEntry fileEntry)
        {
            // Nexus.Archive 1.0.1's decompressor calls an obsolete LzmaStream
            // constructor. Keep its index/core/local-file routing, then decode here.
            return Decompress(fileEntry, archive.OpenFileStream(fileEntry, handleCompression: false));
        }

        internal static Stream Decompress(IArchiveFileEntry fileEntry, Stream stream)
        {
            try
            {
                switch (fileEntry.Flags & (ArchiveFileFlags.CompressedLzma | ArchiveFileFlags.CompressedDeflate))
                {
                    case ArchiveFileFlags.CompressedLzma:
                    {
                        const int propertyLength = 5;
                        if (fileEntry.CompressedSize < propertyLength)
                            throw new InvalidDataException("LZMA archive entry is shorter than its properties.");

                        byte[] properties = new byte[propertyLength];
                        stream.ReadExactly(properties);
                        return LzmaStream.Create(properties, stream, fileEntry.CompressedSize - propertyLength, fileEntry.UncompressedSize);
                    }
                    case ArchiveFileFlags.CompressedDeflate:
                        return new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: false);
                    default:
                        return stream;
                }
            }
            catch
            {
                stream?.Dispose();
                throw;
            }
        }
    }
}
