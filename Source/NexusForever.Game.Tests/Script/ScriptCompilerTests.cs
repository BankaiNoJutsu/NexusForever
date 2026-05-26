using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Script;
using NexusForever.Script.Compile;
using NexusForever.Script.Loader;
using NexusForever.Script.Template;
using NexusForever.Script.Watcher;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Script;

public class ScriptCompilerTests
{
    [Fact]
    public void Compile_FailedEmit_DoesNotPoisonNextCompile()
    {
        var compiler = new CSharpCompiler(NullLogger<ICompiler>.Instance);
        compiler.Initialise("BrokenScript");
        compiler.AddSource("public class BrokenScript { public MissingType Value { get; set; } }");

        Assert.Throws<CompileException>(() => compiler.Compile(new MemoryStream()));

        compiler.Initialise("ValidScript");
        compiler.AddSource("public class ValidScript { public int Value { get; set; } }");
        using var stream = new MemoryStream();

        compiler.Compile(stream);

        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void SourceLoader_CompilesSourcesThatUseMicrosoftExtensionsLogging()
    {
        string sourcePath = Path.Combine(Path.GetTempPath(), $"NexusForeverScriptCompilerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourcePath);

        try
        {
            File.WriteAllText(
                Path.Combine(sourcePath, "LoggingScript.cs"),
                """
                using Microsoft.Extensions.Logging;

                public sealed class LoggingScript
                {
                    public LoggingScript(ILogger<LoggingScript> log)
                    {
                    }
                }
                """);

            var compiler = new CSharpCompiler(NullLogger<ICompiler>.Instance);
            var loader = new SourceLoader(compiler);
            using Stream stream = loader.Load(sourcePath);

            Assert.True(stream.Length > 0);
        }
        finally
        {
            Directory.Delete(sourcePath, recursive: true);
        }
    }

    [Fact]
    public void ScriptAssemblyInfo_Unload_AfterFailedSourceLoad_DoesNotThrow()
    {
        var assemblyWatcher = new RecordingWatcher();
        var sourceWatcher = new RecordingWatcher();
        var assemblyInfo = new ScriptAssemblyInfo(
            NullLogger<IScriptAssemblyInfo>.Instance,
            new ThrowingAssemblyLoader(),
            new ThrowingSourceLoader(),
            assemblyWatcher,
            sourceWatcher,
            new StubFactory<IScriptInfo>(() => throw new InvalidOperationException("Scripts should not be resolved.")));

        assemblyInfo.Initialise("BrokenScriptAssembly", null, Path.GetTempPath());
        assemblyInfo.LoadFromSource();

        Exception exception = Record.Exception(() => assemblyInfo.Unload());

        Assert.Null(exception);
        Assert.False(assemblyWatcher.IsWatching);
        Assert.False(sourceWatcher.IsWatching);
    }

    private sealed class ThrowingAssemblyLoader : IAssemblyLoader
    {
        public Stream Load(string path) => throw new InvalidOperationException("Assembly load failed.");
    }

    private sealed class ThrowingSourceLoader : ISourceLoader
    {
        public Stream Load(string path) => throw new CompileException("Source compile failed.");
    }

    private sealed class RecordingWatcher : IAssemblyWatcher, ISourceWatcher
    {
        public bool IsWatching { get; private set; }

        public event Action OnEvent
        {
            add { }
            remove { }
        }

        public void Initialise(string path)
        {
        }

        public void Start()
        {
            IsWatching = true;
        }

        public void Stop()
        {
            IsWatching = false;
        }
    }

    private sealed class StubFactory<T>(Func<T> create) : IFactory<T> where T : class
    {
        public T Resolve() => create();
    }
}
