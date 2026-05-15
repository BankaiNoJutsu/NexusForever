using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Archive;
using NexusForever.Shared;
using NLog;

namespace NexusForever.MapGenerator
{
    public sealed class ArchiveManager : Singleton<ArchiveManager>, IDisposable
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private static readonly string[] localisationIndexes =
        {
            "ClientDataEN.index",
            "ClientDataFR.index",
            "ClientDataDE.index"
        };

        /// <summary>
        /// Main client ClientData archive.
        /// </summary>
        public Archive MainArchive { get; private set; }

        /// <summary>
        /// Path to the WildStar patch directory.
        /// </summary>
        public string PatchPath { get; private set; }

        /// <summary>
        /// Collection of client localisation archives.
        /// </summary>
        public List<Archive> LocalisationArchives { get; } = new List<Archive>();

        private ArchiveFile coreDataArchive;

        public void Initialise(string patchPath)
        {
            DisposeArchives();

            PatchPath = Path.GetFullPath(patchPath);
            log.Info("Loading archives...");

            // CoreData archive only applicable to Steam client
            string coreDataPath = Path.Combine(PatchPath, "CoreData.archive");
            if (File.Exists(coreDataPath))
                coreDataArchive = ArchiveFileBase.FromFile(coreDataPath) as ArchiveFile;

            MainArchive = Archive.FromFile(Path.Combine(PatchPath, "ClientData.index"), coreDataArchive);

            foreach (string localisationArchivePath in localisationIndexes
                .Select(i => Path.Combine(PatchPath, i)))
            {
                if (!File.Exists(localisationArchivePath))
                    continue;

                LocalisationArchives.Add(Archive.FromFile(localisationArchivePath, coreDataArchive));
            }
        }

        public ArchiveManager CreateIsolatedInstance()
        {
            if (string.IsNullOrWhiteSpace(PatchPath))
                throw new InvalidOperationException("Archive manager has not been initialised with a patch path.");

            var archiveManager = new ArchiveManager();
            archiveManager.Initialise(PatchPath);
            return archiveManager;
        }

        public void Dispose()
        {
            DisposeArchives();
            GC.SuppressFinalize(this);
        }

        private void DisposeArchives()
        {
            foreach (Archive archive in LocalisationArchives)
                archive?.Dispose();

            LocalisationArchives.Clear();

            MainArchive?.Dispose();
            MainArchive = null;

            coreDataArchive?.Dispose();
            coreDataArchive = null;
        }
    }
}
