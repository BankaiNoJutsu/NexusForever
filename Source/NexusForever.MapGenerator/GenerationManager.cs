using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexus.Archive;
using NexusForever.GameTable.Model;
using NexusForever.IO;
using NexusForever.IO.Area;
using NexusForever.IO.Map;
using NexusForever.MapGenerator.GameTable;
using NexusForever.Shared;
using NLog;

namespace NexusForever.MapGenerator
{
    public sealed class GenerationManager : Singleton<GenerationManager>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly Regex gridFilePattern = new(@"[\w]+\.([A-Fa-f0-9]{2})([A-Fa-f0-9]{2})\.area", RegexOptions.Compiled);
        private string outputDir;

        private readonly struct GridRequest
        {
            public GridRequest(string asset, string archivePath, byte x, byte y)
            {
                Asset = asset;
                ArchivePath = archivePath;
                X = x;
                Y = y;
            }

            public string Asset { get; }
            public string ArchivePath { get; }
            public byte X { get; }
            public byte Y { get; }
        }

        public void Initialise(string outputDir)
        {
            log.Info("Generatring base map files...");

            this.outputDir = Path.Combine(outputDir, "map");

            Directory.CreateDirectory(this.outputDir);
        }

        /// <summary>
        /// Generate a base map (.nfmap) file for a single world optionally specifying a single grid.
        /// </summary>
        public void GenerateWorld(ushort worldId, byte? gridX = null, byte? gridY = null, int maxDegreeOfParallelism = 1)
        {
            WorldEntry entry = GameTableManager.Instance.World.GetEntry(worldId);
            if (entry != null)
                ProcessWorld(entry, ArchiveManager.Instance, maxDegreeOfParallelism, gridX, gridY);
        }

        /// <summary>
        /// Generate base map (.nfmap) files for all worlds.
        /// </summary>
        public void GenerateWorlds(int maxDegreeOfParallelism = 1)
        {
            List<WorldEntry> entries = GameTableManager.Instance.World.Entries
                .Where(e => e.AssetPath != string.Empty)
                .GroupBy(e => e.AssetPath)
                .Select(g => g.First())
                .ToList();

            int effectiveMaxDegreeOfParallelism = ParallelismHelper.GetEffectiveMaxDegreeOfParallelism(maxDegreeOfParallelism, entries.Count);
            if (effectiveMaxDegreeOfParallelism == 1)
            {
                foreach (WorldEntry entry in entries)
                    ProcessWorld(entry, ArchiveManager.Instance);

                return;
            }

            log.Info($"Generating {entries.Count} base maps with up to {effectiveMaxDegreeOfParallelism} workers...");

            Parallel.ForEach(
                entries,
                new ParallelOptions { MaxDegreeOfParallelism = effectiveMaxDegreeOfParallelism },
                () => ArchiveManager.Instance.CreateIsolatedInstance(),
                (entry, _, localArchiveManager) =>
                {
                    ProcessWorld(entry, localArchiveManager);
                    return localArchiveManager;
                },
                localArchiveManager => localArchiveManager.Dispose());
        }

        /// <summary>
        /// Generate a base map (.nfmap) file from supplied <see cref="WorldEntry"/>.
        /// </summary>
        private void ProcessWorld(WorldEntry entry, ArchiveManager archiveManager, int maxDegreeOfParallelism = 1, byte? gridX = null, byte? gridY = null)
        {
            var mapFile = new WritableMapFile(Path.GetFileName(entry.AssetPath.Replace('\\', Path.DirectorySeparatorChar)));

            log.Info($"Processing {mapFile.Asset}...");

            List<GridRequest> gridRequests = GetGridRequests(entry, archiveManager, mapFile.Asset, gridX, gridY);
            int effectiveMaxDegreeOfParallelism = ParallelismHelper.GetEffectiveMaxDegreeOfParallelism(maxDegreeOfParallelism, gridRequests.Count);
            if (effectiveMaxDegreeOfParallelism == 1)
            {
                foreach (GridRequest gridRequest in gridRequests)
                {
                    WritableMapFileGrid mapFileGrid = ProcessGrid(archiveManager, gridRequest);
                    if (mapFileGrid != null)
                        mapFile.SetGrid(mapFileGrid.X, mapFileGrid.Y, mapFileGrid);
                }
            }
            else
            {
                log.Info($"Processing {mapFile.Asset} with up to {effectiveMaxDegreeOfParallelism} grid workers...");

                var mapFileGrids = new ConcurrentBag<WritableMapFileGrid>();
                Parallel.ForEach(
                    gridRequests,
                    new ParallelOptions { MaxDegreeOfParallelism = effectiveMaxDegreeOfParallelism },
                    () => ArchiveManager.Instance.CreateIsolatedInstance(),
                    (gridRequest, _, localArchiveManager) =>
                    {
                        WritableMapFileGrid mapFileGrid = ProcessGrid(localArchiveManager, gridRequest);
                        if (mapFileGrid != null)
                            mapFileGrids.Add(mapFileGrid);

                        return localArchiveManager;
                    },
                    localArchiveManager => localArchiveManager.Dispose());

                foreach (WritableMapFileGrid mapFileGrid in mapFileGrids)
                {
                    mapFile.SetGrid(mapFileGrid.X, mapFileGrid.Y, mapFileGrid);
                }
            }

            // FIXME: this happens for worlds with no terrain information, this is usually an instance where props are used as terrain
            if (!mapFile.Any())
            {
                log.Info($"Map {mapFile.Asset} has no grid information, skipping");
                return;
            }

            // Path.ChangeExtension(mapFile.Asset, "nfmap")
            // ChangeExtension doesn't behave correctly on linux
            string filePath = Path.Combine(outputDir, $"{mapFile.Asset}.nfmap");

            using (FileStream stream = File.Create(filePath))
            using (var writer = new BinaryWriter(stream))
            {
                mapFile.Write(writer);
            }
        }

        private List<GridRequest> GetGridRequests(WorldEntry entry, ArchiveManager archiveManager, string asset, byte? gridX = null, byte? gridY = null)
        {
            var gridRequests = new List<GridRequest>();
            if (gridX.HasValue && gridY.HasValue)
            {
                string gridPath = Path.Combine(entry.AssetPath, $"{asset}.{gridX:x2}{gridY:x2}.area");
                gridRequests.Add(new GridRequest(asset, gridPath, gridX.Value, gridY.Value));
                return gridRequests;
            }

            string path = Path.Combine(entry.AssetPath, "*.*.area");
            foreach (IArchiveFileEntry grid in archiveManager.MainArchive.IndexFile.GetFiles(path))
            {
                if (grid.FileName.Contains("_low", StringComparison.OrdinalIgnoreCase))
                    continue;

                Match match = gridFilePattern.Match(grid.FileName);
                if (!match.Success)
                    continue;

                byte x = byte.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
                byte y = byte.Parse(match.Groups[2].Value, NumberStyles.HexNumber);
                gridRequests.Add(new GridRequest(asset, Path.Combine(entry.AssetPath, grid.FileName), x, y));
            }

            return gridRequests;
        }

        private WritableMapFileGrid ProcessGrid(ArchiveManager archiveManager, GridRequest gridRequest)
        {
            IArchiveFileEntry grid = archiveManager.MainArchive.GetFileInfoByPath(gridRequest.ArchivePath);
            if (grid == null)
                return null;

            log.Info($"Processing {gridRequest.Asset} grid {gridRequest.X},{gridRequest.Y}...");

            using (Stream stream = archiveManager.MainArchive.OpenFileStream(grid))
            {
                try
                {
                    var mapFileGrid = new WritableMapFileGrid(gridRequest.X, gridRequest.Y);
                    var areaFile = new AreaFile(stream);
                    foreach (IReadable areaChunk in areaFile.Chunks)
                    {
                        switch (areaChunk)
                        {
                            case Chnk chnk:
                            {
                                foreach (ChnkCell cell in chnk.Cells.Where(c => c != null))
                                    mapFileGrid.AddCell(new WritableMapFileCell(cell));
                                break;
                            }
                        }
                    }

                    return mapFileGrid;
                }
                catch (Exception e)
                {
                    log.Error(e);
                }
            }

            return null;
        }
    }
}
