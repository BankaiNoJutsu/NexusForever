using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using NexusForever.Script.Loader;
using NexusForever.Script.Static;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Collection;
using NexusForever.Script.Template.Filter;
using NexusForever.Script.Watcher;
using NexusForever.Shared;

namespace NexusForever.Script
{
    public class ScriptAssemblyInfo : IScriptAssemblyInfo
    {
        public string Name { get; private set; }
        public string AssemblyPath { get; private set; }
        public string SourcePath { get; private set; }

        /// <summary>
        /// Determines what type of reload is required for <see cref="IScriptAssemblyInfo"/>.
        /// </summary>
        public ReloadType? Reload { get; set; }

        private DateTime lastReload;

        private AssemblyLoadContext context;
        private readonly List<IScriptInfo> scripts = new();

        #region Dependency Injection

        private readonly ILogger log;

        private readonly ILoader assemblyLoader;
        private readonly ILoader sourceLoader;
        private readonly IWatcher assemblyWatcher;
        private readonly IWatcher sourceWatcher;

        private readonly IFactory<IScriptInfo> scriptFactory;

        public ScriptAssemblyInfo(
            ILogger<IScriptAssemblyInfo> log,
            IAssemblyLoader assemblyLoader,
            ISourceLoader sourceLoader,
            IAssemblyWatcher assemblyWatcher,
            ISourceWatcher sourceWatcher,
            IFactory<IScriptInfo> scriptFactory)
        {
            this.log             = log;
            this.assemblyLoader  = assemblyLoader;
            this.sourceLoader    = sourceLoader;
            this.assemblyWatcher = assemblyWatcher;
            this.sourceWatcher   = sourceWatcher;
            this.scriptFactory   = scriptFactory;
        }

        #endregion

        /// <summary>
        /// Initialise a new <see cref="IScriptAssemblyInfo"/> with supplied assembly and source locations.
        /// </summary>
        public void Initialise(string name, string assemblyLocation, string sourceLocation)
        {
            Name         = name;
            AssemblyPath = assemblyLocation;
            SourcePath   = sourceLocation;

            if (AssemblyPath != null)
                assemblyWatcher.Initialise(AssemblyPath);
            if (SourcePath != null)
                sourceWatcher.Initialise(SourcePath);

            log.LogInformation("Initialised script assembly {Name}.", Name);
        }

        /// <summary>
        /// Load <see cref="IScriptAssemblyInfo"/> from assembly at supplied location.
        /// </summary>
        public void LoadFromAssembly()
        {
            log.LogTrace("Loading script assembly {Name} from assembly.", Name);
            LoadFromLoader(assemblyLoader, AssemblyPath);
        }

        /// <summary>
        /// Load <see cref="IScriptAssemblyInfo"/> from source at supplied location.
        /// </summary>
        public void LoadFromSource()
        {
            log.LogTrace("Loading script assembly {Name} from source.", Name);
            LoadFromLoader(sourceLoader, SourcePath);
        }

        private void LoadFromLoader(ILoader loader, string path)
        {
            if (context != null)
                throw new InvalidOperationException();

            try
            {
                using Stream stream = loader.Load(path);
                context = CreateLoadContext();
                Assembly assembly = context.LoadFromStream(stream);

                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsAssignableTo(typeof(IScript)))
                        continue;

                    if (type.IsAbstract)
                        continue;

                    if (type.GetCustomAttribute<ScriptFilterIgnoreAttribute>() != null)
                    {
                        log.LogTrace("Script type {type} was ignoed due to attribute.", type.Name);
                        continue;
                    }

                    IScriptInfo scriptInfo = scriptFactory.Resolve();
                    scriptInfo.Initialise(type);
                    scripts.Add(scriptInfo);

                    log.LogTrace("Added script type {TypeName} to script assembly {Name}.", type.Name, Name);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An exception occured during loading of script assembly {Name}!", Name);
            }


            if (AssemblyPath != null)
            {
                assemblyWatcher.OnEvent += () => RaiseEvent(ReloadType.Assembly);
                assemblyWatcher.Start();
            }

            if (SourcePath != null)
            {
                sourceWatcher.OnEvent += () => RaiseEvent(ReloadType.Source);
                sourceWatcher.Start();
            }
        }

        private AssemblyLoadContext CreateLoadContext()
        {
            string assemblyPath = !string.IsNullOrWhiteSpace(AssemblyPath)
                ? Path.GetFullPath(AssemblyPath)
                : null;

            var probeDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (assemblyPath != null)
                probeDirectories.Add(Path.GetDirectoryName(assemblyPath));

            string hostDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrWhiteSpace(hostDirectory))
                probeDirectories.Add(hostDirectory);

            var loadContext = new AssemblyLoadContext(Name, true);
            AssemblyDependencyResolver resolver = assemblyPath != null && File.Exists(assemblyPath)
                ? new AssemblyDependencyResolver(assemblyPath)
                : null;

            loadContext.Resolving += (context, assemblyName) => ResolveAssembly(context, assemblyName, resolver, probeDirectories);
            return loadContext;
        }

        private static Assembly ResolveAssembly(
            AssemblyLoadContext context,
            AssemblyName assemblyName,
            AssemblyDependencyResolver resolver,
            IEnumerable<string> probeDirectories)
        {
            Assembly defaultAssembly = FindDefaultAssembly(assemblyName);
            if (!IsScriptImplementationAssembly(assemblyName.Name) && defaultAssembly != null)
                return defaultAssembly;

            string resolvedPath = resolver?.ResolveAssemblyToPath(assemblyName);
            if (resolvedPath == null)
            {
                resolvedPath = probeDirectories
                    .Select(directory => Path.Combine(directory, $"{assemblyName.Name}.dll"))
                    .FirstOrDefault(File.Exists);
            }

            if (resolvedPath != null)
                return LoadAssemblyFromPath(context, resolvedPath);

            return defaultAssembly;
        }

        private static Assembly FindDefaultAssembly(AssemblyName assemblyName)
        {
            return AssemblyLoadContext.Default.Assemblies
                .FirstOrDefault(assembly => AssemblyName.ReferenceMatchesDefinition(assemblyName, assembly.GetName()));
        }

        private static bool IsScriptImplementationAssembly(string assemblyName)
        {
            return !string.IsNullOrWhiteSpace(assemblyName)
                && assemblyName.StartsWith("NexusForever.Script.", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(assemblyName, "NexusForever.Script", StringComparison.OrdinalIgnoreCase);
        }

        private static Assembly LoadAssemblyFromPath(AssemblyLoadContext context, string path)
        {
            using FileStream assemblyStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            string symbolPath = Path.ChangeExtension(path, ".pdb");
            if (!File.Exists(symbolPath))
                return context.LoadFromStream(assemblyStream);

            using FileStream symbolStream = File.Open(symbolPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return context.LoadFromStream(assemblyStream, symbolStream);
        }

        private void RaiseEvent(ReloadType reloadType)
        {
            log.LogTrace("Reload of type {reloadType} has been requested for script assembly {Name}.", reloadType, Name);
         
            // prevent multiple reloads if more than 1 file event occurs in a short span of time
            // this can happen when saving multiple script files at once
            if (DateTime.UtcNow - lastReload < TimeSpan.FromSeconds(5))
            {
                log.LogWarning("Ignoring reload request!");
                return;
            }
            
            Reload     = reloadType;
            lastReload = DateTime.UtcNow;
        }

        /// <summary>
        /// Unload <see cref="IScriptAssemblyInfo"/>.
        /// </summary>
        /// <remarks>
        /// This also will unload <see cref="IScriptInfo"/> contained in <see cref="IScriptAssemblyInfo"/> and unload <see cref="IScriptInstanceInfo"/> from <see cref="IScriptCollection"/>'s.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public WeakReference Unload()
        {
            if (context == null)
                throw new InvalidOperationException();

            log.LogInformation("Starting unload for script assembly {Name}.", Name);

            if (assemblyWatcher?.IsWatching == true)
                assemblyWatcher.Stop();

            if (sourceWatcher?.IsWatching == true)
                sourceWatcher.Stop();

            foreach (IScriptInfo scriptInfo in scripts)
                scriptInfo.Unload();

            scripts.Clear();

            // AssemblyLoadContext is unloaded asynchronously, use a weak reference to track when unload is complete  
            var weakReference = new WeakReference(context);
            context.Unload();
            context = null;

            return weakReference;
        }

        /// <summary>
        /// Return a collection of <see cref="IScriptInfo"/>'s filtered with supplied <see cref="IScriptFilterSearch"/>.
        /// </summary>
        public IEnumerable<IScriptInfo> Filter(IScriptFilterSearch search)
        {
            foreach (IScriptInfo script in scripts)
                if (script.Match(search))
                    yield return script;
        }

        /// <summary>
        /// Return a collection of <see cref="IScriptCollection"/>'s which contain <see cref="IScriptInstanceInfo"/>'s created from this <see cref="IScriptAssemblyInfo"/>.
        /// </summary>
        public IEnumerable<IScriptCollection> GetScriptCollections()
        {
            foreach (IScriptInfo script in scripts)
                foreach (IScriptCollection collection in script.GetScriptCollections())
                    yield return collection;
        }

        /// <summary>
        /// Build information about <see cref="IScriptAssemblyInfo"/>.
        /// </summary>
        public void Information(IndentedStringBuilder sb)
        {
            sb.IncrementIndent();
            sb.AppendLine($"Assembly Name: {Name}");
            sb.AppendLine($"Assembly Assembly: {AssemblyPath}");
            sb.AppendLine($"Assembly Source: {SourcePath}");

            foreach (IScriptInfo scriptInfo in  scripts)
                scriptInfo.Information(sb);

            sb.DecrementIndent();
        }
    }
}
