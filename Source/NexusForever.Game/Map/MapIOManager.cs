using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Configuration.Model;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.IO.Map;
using NexusForever.Shared.Configuration;
using NLog;

namespace NexusForever.Game.Map
{
    public sealed class MapIOManager : IMapIOManager
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, MapFile> mapFiles = new();
        private readonly object mapFileLock = new();
        private readonly ISharedConfiguration sharedConfiguration;
        private readonly IGameTableManager gameTableManager;

        public MapIOManager(
            ISharedConfiguration sharedConfiguration = null,
            IGameTableManager gameTableManager = null)
        {
            this.sharedConfiguration = sharedConfiguration;
            this.gameTableManager    = gameTableManager;
        }

        public void Initialise()
        {
            ValidateMapFiles();
            PreCacheMapFiles();
        }

        private void ValidateMapFiles()
        {
            log.Info("Validating map files...");

            string mapPath = GetMapConfig().MapPath;
            if (mapPath == null || !Directory.Exists(mapPath))
                throw new DirectoryNotFoundException("Invalid path to base maps! Make sure you have set it in the configuration file.");

            foreach (string fileName in Directory.EnumerateFiles(mapPath, "*.nfmap"))
            {
                using FileStream stream = File.OpenRead(fileName);
                using var reader = new BinaryReader(stream);
                var mapFile = new MapFile();
                mapFile.ReadHeader(reader);
            }
        }

        private void PreCacheMapFiles()
        {
            log.Info("Caching map files...");

            List<ushort> precachedBaseMaps = GetMapConfig().PrecacheBaseMaps;
            if (precachedBaseMaps == null)
                return;

            foreach (ushort worldId in precachedBaseMaps)
            {
                if (gameTableManager == null)
                    throw new InvalidOperationException("MapIOManager requires an IGameTableManager to precache base maps.");

                WorldEntry entry = gameTableManager.World.GetEntry(worldId);
                if (entry == null)
                    throw new ConfigurationException($"Invalid world id {worldId} supplied for precached base maps!");

                LoadBaseMap(entry.AssetPath);
            }
        }

        /// <summary>
        /// Returns an existing <see cref="MapFile"/> for the supplied asset, if it doesn't exist a new one will be created from disk.
        /// </summary>
        public MapFile GetBaseMap(string assetPath)
        {
            if (mapFiles.TryGetValue(assetPath, out MapFile mapFile))
                return mapFile;

            lock (mapFileLock)
            {
                if (mapFiles.TryGetValue(assetPath, out mapFile))
                    return mapFile;

                return LoadBaseMap(assetPath);
            }
        }

        private MapFile LoadBaseMap(string assetPath)
        {
            string mapPath  = GetMapConfig().MapPath;

            // replace backslashes with OS specific directory separator, for Linux it will replace backslashes with forward slashes
            // this will allow GetFileName to work correctly on Linux
            string asset    = Path.Combine(mapPath, Path.GetFileName(assetPath.Replace('\\', Path.DirectorySeparatorChar)));
            string filePath = Path.ChangeExtension(asset, ".nfmap");

            using FileStream stream = File.OpenRead(filePath);
            using var reader = new BinaryReader(stream);
            var mapFile = new MapFile();
            mapFiles.Add(assetPath, mapFile);
            mapFile.Read(reader);

            log.Trace($"Initialised base map file for asset {assetPath}.");
            return mapFile;
        }

        private MapConfig GetMapConfig()
        {
            if (sharedConfiguration == null)
                throw new InvalidOperationException("MapIOManager requires an ISharedConfiguration to load map files.");

            return sharedConfiguration.Get<MapConfig>();
        }
    }
}
