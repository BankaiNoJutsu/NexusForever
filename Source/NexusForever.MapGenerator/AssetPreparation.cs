using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using NexusForever.GameTable;
using NexusForever.IO.Map;

namespace NexusForever.MapGenerator
{
    internal static class AssetPreparation
    {
        internal const string InventoryFile = ".nexusforever-assets.json";
        internal const string IncompleteFile = ".nexusforever-extracting";

        private sealed record Inventory(int Version, Dictionary<string, string> Files);

        internal static string[] RequiredTables => typeof(NexusForever.GameTable.GameTableManager).GetProperties()
            .Where(p => p.GetCustomAttribute<GameDataAttribute>() != null && p.PropertyType != typeof(TextTable))
            .Select(p => p.GetCustomAttribute<GameDataAttribute>()!.FileName ?? p.Name + ".tbl")
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        public static void Run(Parameters parameters, Action<Parameters> generate)
            => Run(parameters, generate, Validate);

        // The validator boundary also lets preparation tests use a small real asset
        // contract without generating every shipped world/table.
        internal static void Run(Parameters parameters, Action<Parameters> generate, Action<string, bool> validate)
        {
            if (string.IsNullOrWhiteSpace(parameters.OutputDir))
                throw new ArgumentException("Asset preparation requires --output.");
            if (parameters.WorldId.HasValue || parameters.GridX.HasValue || parameters.GridY.HasValue)
                throw new ArgumentException("Asset preparation requires the complete asset set; omit world/grid filters.");
            if (parameters.MaxParallelism < 0)
                throw new ArgumentOutOfRangeException(nameof(parameters.MaxParallelism));

            string destination = Path.GetFullPath(parameters.OutputDir);
            Directory.CreateDirectory(destination);
            using FileStream directoryLock = File.Open(Path.Combine(destination, ".nexusforever-assets.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            string marker = Path.Combine(destination, IncompleteFile);
            if (!File.Exists(marker))
            {
                try
                {
                    validate(destination, true);
                    Console.WriteLine("Using verified existing game assets.");
                    return;
                }
                catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException)
                {
                    Console.WriteLine($"Game assets require preparation: {exception.Message}");
                }
            }

            string patch = parameters.PatchPath ?? string.Empty;
            if (Directory.Exists(Path.Combine(patch, "Patch")))
                patch = Path.Combine(patch, "Patch");
            if (!File.Exists(Path.Combine(patch, "ClientData.index")))
                throw new FileNotFoundException("Asset repair requires a WildStar installation or Patch directory with ClientData.index.");

            string staging = Path.Combine(destination, ".nexusforever-staging-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            try
            {
                File.WriteAllText(marker, "Asset preparation is incomplete; rerun --prepare before starting servers.\n");
                generate(new Parameters
                {
                    PatchPath = Path.GetFullPath(patch), OutputDir = staging,
                    Extract = true, Generate = true, MaxParallelism = parameters.MaxParallelism
                });
                validate(staging, false);
                WriteInventory(staging);
                foreach (string folder in new[] { "tbl", "map" })
                {
                    if ((File.GetAttributes(Path.Combine(staging, folder)) & FileAttributes.ReparsePoint) != 0)
                        throw new InvalidDataException("Generated asset folders cannot be directory links.");
                    Directory.CreateDirectory(Path.Combine(destination, folder));
                    foreach (string file in Directory.GetFiles(Path.Combine(staging, folder)))
                        File.Move(file, Path.Combine(destination, folder, Path.GetFileName(file)), overwrite: true);
                }
                File.Move(Path.Combine(staging, InventoryFile), Path.Combine(destination, InventoryFile), overwrite: true);
                // Retained custom maps are still runtime inputs. Keep the marker
                // until the resulting cache, including those files, is usable.
                ValidateMapsContent(Path.Combine(destination, "map"));
                ValidateInventory(destination);
                File.Delete(marker);
            }
            finally
            {
                // This fresh, uniquely named child belongs only to this operation.
                // Never delete an existing user .prepare folder or prune custom maps.
                if (Directory.Exists(staging) && (File.GetAttributes(staging) & FileAttributes.ReparsePoint) == 0)
                    Directory.Delete(staging, recursive: true);
            }
        }

        public static void Validate(string directory, bool inventory)
        {
            if (File.Exists(Path.Combine(directory, IncompleteFile)))
                throw new InvalidDataException("Game asset preparation did not finish.");
            ValidateTables(Path.Combine(directory, "tbl"));
            ValidateMaps(Path.Combine(directory, "map"));
            if (inventory)
                ValidateInventory(directory);
        }

        public static void ValidateTables(string directory)
        {
            RejectIncomplete(directory);
            ValidateTablesContent(directory);
        }

        private static void ValidateTablesContent(string directory)
        {
            foreach (var property in typeof(NexusForever.GameTable.GameTableManager).GetProperties())
            {
                GameDataAttribute attribute = property.GetCustomAttribute<GameDataAttribute>();
                if (attribute == null || property.PropertyType == typeof(TextTable))
                    continue;
                string path = Path.Combine(directory, attribute.FileName ?? property.Name + ".tbl");
                ValidateTableHeader(path);
                try
                {
                    _ = Activator.CreateInstance(property.PropertyType, path);
                }
                catch (TargetInvocationException exception) when (exception.InnerException is
                    IOException or GameTableException or ArgumentException or IndexOutOfRangeException)
                {
                    throw new InvalidDataException($"Cannot load runtime table: {path}", exception.InnerException);
                }
            }
            foreach (string locale in new[] { "en-US.bin", "en-GB.bin", "de-DE.bin", "fr-FR.bin" })
            {
                string path = Path.Combine(directory, locale);
                if (locale != "en-US.bin" && !File.Exists(path))
                    continue;
                ValidateTableHeader(path, text: true);
                try { _ = new TextTable(path); }
                catch (Exception exception) when (exception is ArgumentException or IndexOutOfRangeException)
                {
                    throw new InvalidDataException($"Cannot load runtime locale: {path}", exception);
                }
            }
        }

        public static void ValidateMaps(string directory)
        {
            RejectIncomplete(directory);
            ValidateMapsContent(directory);
        }

        private static void ValidateMapsContent(string directory)
        {
            string[] files = Directory.GetFiles(directory, "*.nfmap");
            if (files.Length == 0)
                throw new InvalidDataException("No generated map files found.");
            foreach (string path in files)
            {
                using var reader = new BinaryReader(File.OpenRead(path));
                new MapFile().Read(reader);
            }
        }

        internal static void ValidateTableHeader(string path, bool text = false)
        {
            using FileStream stream = File.OpenRead(path);
            if (text)
            {
                TextTableHeader header = ReadHeader<TextTableHeader>(stream);
                if (header.Signature != 0x4C544558 || header.RecordCount == 0)
                    throw new InvalidDataException($"Invalid text table: {path}");
                RequireRange(stream, header.RecordOffset, header.RecordCount, (ulong)Marshal.SizeOf<TextTableField>());
                RequireRange(stream, header.StringTableOffset, header.StringTableLength, 2);
            }
            else
            {
                GameTableHeader header = ReadHeader<GameTableHeader>(stream);
                if (header.Signature != 0x4454424C || (header.RecordCount > 0
                    && (header.RecordSize == 0 || header.RecordCount > header.TotalRecordSize / header.RecordSize)))
                    throw new InvalidDataException($"Invalid game table: {path}");
                RequireRange(stream, header.FieldOffset, header.FieldCount, (ulong)Marshal.SizeOf<GameTableField>());
                RequireRange(stream, header.RecordOffset, header.TotalRecordSize, 1);
                RequireRange(stream, header.LookupOffset, header.MaxId, sizeof(int));
            }
        }

        private static T ReadHeader<T>(Stream stream) where T : unmanaged
        {
            byte[] bytes = new byte[Marshal.SizeOf<T>()];
            stream.ReadExactly(bytes);
            return MemoryMarshal.Read<T>(bytes);
        }

        private static void RejectIncomplete(string directory)
        {
            string parent = Path.GetDirectoryName(Path.GetFullPath(directory));
            if (parent != null && File.Exists(Path.Combine(parent, IncompleteFile)))
                throw new InvalidDataException("Game asset preparation did not finish.");
        }

        private static void RequireRange(Stream stream, ulong offset, ulong count, ulong size)
        {
            ulong available = (ulong)(stream.Length - stream.Position);
            if (offset > available || count > (available - offset) / size)
                throw new InvalidDataException("Table data extends past the end of the file.");
        }

        internal static void WriteInventory(string directory)
        {
            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string folder in new[] { "tbl", "map" })
                foreach (string file in Directory.GetFiles(Path.Combine(directory, folder)))
                    files.Add(folder + "/" + Path.GetFileName(file), Hash(file));
            File.WriteAllText(Path.Combine(directory, InventoryFile), JsonSerializer.Serialize(new Inventory(1, files)));
        }

        internal static void ValidateInventory(string directory)
        {
            Inventory inventory = JsonSerializer.Deserialize<Inventory>(File.ReadAllText(Path.Combine(directory, InventoryFile)));
            if (inventory?.Version != 1 || inventory.Files == null || inventory.Files.Count == 0)
                throw new InvalidDataException("Missing or unsupported game asset inventory.");
            foreach ((string relative, string expected) in inventory.Files)
            {
                string[] parts = relative.Split('/');
                if (parts.Length != 2 || (parts[0] != "tbl" && parts[0] != "map")
                    || parts[1] != Path.GetFileName(parts[1]) || parts[1] is "." or ".." || string.IsNullOrEmpty(parts[1]))
                    throw new InvalidDataException("Invalid path in game asset inventory.");
                if (!string.Equals(Hash(Path.Combine(directory, parts[0], parts[1])), expected, StringComparison.Ordinal))
                    throw new InvalidDataException($"Game asset has changed: {relative}");
            }
        }

        private static string Hash(string path)
        {
            using FileStream stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
    }
}
