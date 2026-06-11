using Newtonsoft.Json;
using NexusForever.GameTable.Configuration.Model;
using NLog;

namespace NexusForever.GameTable
{
    public static class FileCache
    {
        private static Lazy<string> lazyModuleVersion = new(CreateModuleVersionString, LazyThreadSafetyMode.ExecutionAndPublication);
        private static Lazy<DirectoryInfo> lazyCacheDirectory => new(CreateCacheDirectory, LazyThreadSafetyMode.ExecutionAndPublication);
        private static ILogger log = LogManager.GetCurrentClassLogger();
        private static CacheConfig cacheConfig = new();

        public static void Configure(CacheConfig config)
        {
            cacheConfig = config ?? new CacheConfig();
        }

        private static DirectoryInfo CreateCacheDirectory()
        {
            return Directory.CreateDirectory(cacheConfig.CachePath);
        }

        private static readonly object cacheLock = new();
        private static bool cacheInitialized;
        private static string CreateModuleVersionString()
        {
            return Convert.ToHexString(typeof(FileCache).Assembly.ManifestModule.ModuleVersionId.ToByteArray());
        }

        private static void CheckAndCleanupCache()
        {
            lock (cacheLock)
            {
                if (cacheInitialized)
                    return;

                DirectoryInfo cacheDirectory = lazyCacheDirectory.Value;
                FileInfo cacheInfoFile = cacheDirectory.EnumerateFiles("cacheInfo.txt").FirstOrDefault();
                if (cacheInfoFile != null && cacheInfoFile.Exists)
                {
                    string cacheInfo = File.ReadAllText(cacheInfoFile.FullName);
                    if (cacheInfo == lazyModuleVersion.Value)
                    {
                        cacheInitialized = true;
                        return;
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

                File.WriteAllText(Path.Combine(cacheDirectory.FullName, "cacheInfo.txt"), lazyModuleVersion.Value);
                cacheInitialized = true;
            }
        }

        public static T LoadWithCache<T>(string fileName, Func<string, T> creator)
        {
            if (!cacheConfig.UseCache)
                return creator(fileName);
            CheckAndCleanupCache();
            string cacheName = GetCacheFileName(fileName);
            if (File.Exists(cacheName))
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(cacheName));

            T obj = creator(fileName);
            File.WriteAllText(cacheName, JsonConvert.SerializeObject(obj));
            return obj;
        }

        private static string GetCacheFileName(string fileName)
        {
            string cacheFolder = lazyCacheDirectory.Value.FullName;

            string ext = Path.GetExtension(fileName).TrimStart('.');
            string fileHash = Hasher.HashFile(fileName);

            string versionId = lazyModuleVersion.Value;
            string hash = $"{fileHash}-{versionId}".Hash().Substring(0, 7);
            return Path.Combine(cacheFolder, $"{Path.GetFileNameWithoutExtension(fileName)}.{hash}.{versionId}.{ext}.cache");
        }
    }
}
