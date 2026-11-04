using Newtonsoft.Json;
using NexusForever.GameTable.Configuration.Model;
using NLog;

namespace NexusForever.GameTable
{
    public static class FileCache
    {
        private static Lazy<string> lazyModuleVersion = new(CreateModuleVersionString, LazyThreadSafetyMode.ExecutionAndPublication);
        private static ILogger log = LogManager.GetCurrentClassLogger();
        private static CacheConfig cacheConfig = new();
        private static readonly object cacheLock = new();
        private static string initializedCachePath;

        public static void Configure(CacheConfig config)
        {
            lock (cacheLock)
            {
                cacheConfig = config ?? new CacheConfig();
                initializedCachePath = null;
            }
        }

        private static string CreateModuleVersionString()
        {
            return Convert.ToHexString(typeof(FileCache).Assembly.ManifestModule.ModuleVersionId.ToByteArray());
        }

        private static string PrepareCacheDirectory()
        {
            lock (cacheLock)
            {
                if (!cacheConfig.UseCache)
                    return null;

                DirectoryInfo cacheDirectory = Directory.CreateDirectory(cacheConfig.CachePath);
                string directory = cacheDirectory.FullName;
                string versionFile = Path.Combine(directory, "cacheInfo.txt");
                if (File.Exists(versionFile))
                {
                    if (initializedCachePath == directory || File.ReadAllText(versionFile) == lazyModuleVersion.Value)
                    {
                        initializedCachePath = directory;
                        return directory;
                    }
                }

                log.Info("Cache files are out of date, removing them.");
                FileInfo[] allFiles = cacheDirectory.GetFiles();

                foreach (FileInfo file in allFiles)
                {
                    try
                    {
                        log.Debug($"Deleting cache file {file.Name}");
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex, "Failed to delete cache file {0}.", file.Name);
                    }
                }

                File.WriteAllText(versionFile, lazyModuleVersion.Value);
                initializedCachePath = directory;
                return directory;
            }
        }

        public static T LoadWithCache<T>(string fileName, Func<string, T> creator)
        {
            string directory = PrepareCacheDirectory();
            if (directory == null)
                return creator(fileName);
            string cacheName = GetCacheFileName(directory, fileName);
            if (File.Exists(cacheName))
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(cacheName));

            T obj = creator(fileName);
            File.WriteAllText(cacheName, JsonConvert.SerializeObject(obj));
            return obj;
        }

        private static string GetCacheFileName(string cacheFolder, string fileName)
        {
            string ext = Path.GetExtension(fileName).TrimStart('.');
            string fileHash = Hasher.HashFile(fileName);

            string versionId = lazyModuleVersion.Value;
            string hash = $"{fileHash}-{versionId}".Hash().Substring(0, 7);
            return Path.Combine(cacheFolder, $"{Path.GetFileNameWithoutExtension(fileName)}.{hash}.{versionId}.{ext}.cache");
        }
    }
}
