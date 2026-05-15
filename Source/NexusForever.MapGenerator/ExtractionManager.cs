using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Archive;
using NexusForever.Shared;
using NLog;

namespace NexusForever.MapGenerator
{
    public sealed class ExtractionManager : Singleton<ExtractionManager>
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private static readonly string[] languageFiles =
        {
            "en-US.bin",
            "de-DE.bin",
            "fr-FR.bin",
            "en-GB.bin"
        };

        private string outputDir;

        public void Initialise(string outputDir, int maxDegreeOfParallelism = 1)
        {
            log.Info("Extracting GameTables...");

            this.outputDir = Path.Combine(outputDir, "tbl");

            Directory.CreateDirectory(this.outputDir);

            ExtractGameTables(maxDegreeOfParallelism);
            ExtractLanguageFiles();
        }

        /// <summary>
        /// Extract all GameTables (*.tbl) from main client archive.
        /// </summary>
        private void ExtractGameTables(int maxDegreeOfParallelism)
        {
            string searchPattern = Path.Combine("DB", "*.tbl");
            List<string> tablePaths = ArchiveManager.Instance.MainArchive.IndexFile.GetFiles(searchPattern)
                .Select(fileEntry => Path.Combine("DB", fileEntry.FileName))
                .ToList();

            int effectiveMaxDegreeOfParallelism = ParallelismHelper.GetEffectiveMaxDegreeOfParallelism(maxDegreeOfParallelism, tablePaths.Count);
            if (effectiveMaxDegreeOfParallelism == 1)
            {
                foreach (string tablePath in tablePaths)
                    ExtractFile(ArchiveManager.Instance.MainArchive, tablePath);

                return;
            }

            log.Info($"Extracting {tablePaths.Count} game tables with up to {effectiveMaxDegreeOfParallelism} workers...");

            Parallel.ForEach(
                tablePaths,
                new ParallelOptions { MaxDegreeOfParallelism = effectiveMaxDegreeOfParallelism },
                () => ArchiveManager.Instance.CreateIsolatedInstance(),
                (tablePath, _, localArchiveManager) =>
                {
                    ExtractFile(localArchiveManager.MainArchive, tablePath);
                    return localArchiveManager;
                },
                localArchiveManager => localArchiveManager.Dispose());
        }

        /// <summary>
        /// Extract all language files (*.bin) from all present localisation client archives.
        /// </summary>
        private void ExtractLanguageFiles()
        {
            foreach (Archive archive in ArchiveManager.Instance.LocalisationArchives)
            {
                foreach (IArchiveFileEntry fileEntry in languageFiles
                    .Select(archive.IndexFile.FindEntry)
                    .OfType<IArchiveFileEntry>())
                {
                    ExtractFile(archive, fileEntry.FileName);
                }
            }
        }

        /// <summary>
        /// Extract supplied archive file path from <see cref="Archive"/>.
        /// </summary>
        private void ExtractFile(Archive archive, string archiveFilePath)
        {
            if (!(archive.IndexFile.FindEntry(archiveFilePath) is IArchiveFileEntry fileEntry))
                throw new FileNotFoundException($"Archive entry was not found: {archiveFilePath}");

            string filePath = Path.Combine(outputDir, fileEntry.FileName);

            using (Stream archiveStream = archive.OpenFileStream(fileEntry))
            using (FileStream fileStream = File.Create(filePath))
            {
                archiveStream.CopyTo(fileStream);
            }

            log.Info($"Extracted {fileEntry.FileName}...");
        }
    }
}
