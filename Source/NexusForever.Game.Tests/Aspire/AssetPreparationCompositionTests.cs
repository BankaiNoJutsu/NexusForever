using System.Runtime.CompilerServices;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Aspire.AppHost;
using AppHostProgram = NexusForever.Aspire.AppHost.Program;

namespace NexusForever.Game.Tests.Aspire;

public sealed class AssetPreparationCompositionTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), "nf-aspire-assets-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("MySql")]
    [InlineData("Sqlite")]
    public async Task EnabledPreparationGatesOnlyAssetConsumersOnSuccessfulExit(string provider)
    {
        var builder = CreateBuilder(provider, true);
        AppHostProgram.Configure(builder);

        var preparation = Assert.Single(builder.Resources.OfType<ProjectResource>(), r => r.Name == "asset-preparation");
        string root = Path.Combine(temporaryDirectory, "shared assets");
        Assert.Equal(new[] { "--prepare", "--output", root, "--maxParallelism", "2" },
            await Arguments(preparation, builder));

        foreach (var resource in builder.Resources.OfType<ProjectResource>())
        {
            var wait = resource.Annotations.OfType<WaitAnnotation>().Where(w => w.Resource == preparation).ToArray();
            var environment = await Environment(resource, builder);
            if (resource.Name is "world-server" or "chat-server" or "friendship-server")
            {
                // A failed or still-running preparation cannot satisfy this completion dependency.
                var completion = Assert.Single(wait);
                Assert.Equal(WaitType.WaitForCompletion, completion.WaitType);
                Assert.Equal(0, completion.ExitCode);
                Assert.Equal(Path.Combine(root, "tbl"), environment["GameTable:GameTablePath"]);
                if (resource.Name == "world-server")
                    Assert.Equal(Path.Combine(root, "map"), environment["Realm:Map:MapPath"]);
                else
                    Assert.False(environment.ContainsKey("Realm:Map:MapPath"));
            }
            else
            {
                Assert.Empty(wait);
                Assert.False(environment.ContainsKey("GameTable:GameTablePath"));
                Assert.False(environment.ContainsKey("Realm:Map:MapPath"));
            }
        }

        Assert.Equal(provider == "MySql", builder.Resources.Any(r => r.Name == "mysql"));
    }

    [Theory]
    [InlineData("MySql")]
    [InlineData("Sqlite")]
    public async Task DefaultDisabledPreservesExistingAssetsAndMigrationWaits(string provider)
    {
        var builder = CreateBuilder(provider);
        builder.Configuration["Assets:MaxParallelism"] = "-10"; // Unused opt-in settings do not break old setups.
        AppHostProgram.Configure(builder);
        Assert.DoesNotContain(builder.Resources, r => r.Name == "asset-preparation");

        var migrations = Assert.Single(builder.Resources, r => r.Name == "database-migrations");
        foreach (var resource in builder.Resources.OfType<ProjectResource>())
        {
            var environment = await Environment(resource, builder);
            Assert.False(environment.ContainsKey("GameTable:GameTablePath"));
            Assert.False(environment.ContainsKey("Realm:Map:MapPath"));
            if (resource.Name is "world-server" or "chat-server" or "friendship-server" or "auth-server" or "sts-server")
                Assert.Contains(resource.Annotations.OfType<WaitAnnotation>(),
                    w => w.Resource == migrations && w.WaitType == WaitType.WaitForCompletion && w.ExitCode == 0);
        }
    }

    [Fact]
    public async Task ConfiguredPathsWithSpacesRemainSeparateArguments()
    {
        var builder = CreateBuilder("MySql", true);
        string relativeCache = "relative cache " + Guid.NewGuid().ToString("N");
        builder.Configuration["Assets:Root"] = relativeCache;
        builder.Configuration["Assets:ClientPath"] = Path.Combine(temporaryDirectory, "WildStar Client");
        var context = AssetPreparationContext.Create(builder);
        Assert.NotNull(context);
        string repository = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
        Assert.Equal(Path.Combine(repository, relativeCache), context.Root);
        Assert.Equal(new[] { "--prepare", "--output", context.Root, "--maxParallelism", "2", "--patchPath",
                Path.Combine(temporaryDirectory, "WildStar Client") },
            await Arguments(context.Resource.Resource, builder));
        Assert.False(Directory.Exists(context.Root)); // Composition never writes to the cache.
    }

    [Fact]
    public async Task DefaultRootIsRepositorySharedAndValidCacheCanRunWithoutClient()
    {
        var builder = CreateBuilder("MySql", true);
        builder.Configuration["Assets:Root"] = null;
        builder.Configuration["Assets:MaxParallelism"] = null;
        var context = AssetPreparationContext.Create(builder);
        string repository = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
        Assert.Equal(Path.Combine(repository, ".nexusforever-runtime", "assets"), context.Root);
        Assert.Equal(new[] { "--prepare", "--output", context.Root, "--maxParallelism", "0" },
            await Arguments(context.Resource.Resource, builder));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    [InlineData(int.MaxValue)]
    public void InvalidParallelismFailsBeforeAddingPreparationResource(int parallelism)
    {
        var builder = CreateBuilder("MySql", true);
        builder.Configuration["Assets:MaxParallelism"] = parallelism.ToString();
        Assert.Throws<InvalidOperationException>(() => AssetPreparationContext.Create(builder));
        Assert.Empty(builder.Resources);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    public async Task ValidParallelismIsPassedExactly(int parallelism)
    {
        var builder = CreateBuilder("MySql", true);
        builder.Configuration["Assets:MaxParallelism"] = parallelism.ToString();
        var context = AssetPreparationContext.Create(builder);
        var arguments = await Arguments(context.Resource.Resource, builder);
        Assert.Equal(parallelism.ToString(), arguments.Last());
    }

    [Fact]
    public void SeparateBuildersNeverCaptureAnotherBuildersPreparationResource()
    {
        var first = CreateBuilder("MySql", true);
        var second = CreateBuilder("Sqlite", true);
        AppHostProgram.Configure(first);
        AppHostProgram.Configure(second);
        var firstPreparation = Assert.Single(first.Resources, r => r.Name == "asset-preparation");
        var secondPreparation = Assert.Single(second.Resources, r => r.Name == "asset-preparation");
        Assert.NotSame(firstPreparation, secondPreparation);
        foreach (var resource in second.Resources.OfType<ProjectResource>())
            Assert.DoesNotContain(resource.Annotations.OfType<WaitAnnotation>(), w => w.Resource == firstPreparation);
        Assert.Contains(second.Resources.Single(r => r.Name == "world-server").Annotations.OfType<WaitAnnotation>(),
            w => w.Resource == secondPreparation && w.ExitCode == 0);
    }

    private static async Task<IReadOnlyList<string>> Arguments(IResource resource, IDistributedApplicationBuilder builder)
    {
        using var services = builder.Services.BuildServiceProvider();
        var result = await ExecutionConfigurationBuilder.Create(resource).WithArgumentsConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish) { ServiceProvider = services }));
        Assert.Null(result.Exception);
        return result.Arguments.Select(argument => argument.Value).ToArray();
    }

    private static async Task<IReadOnlyDictionary<string, string>> Environment(IResource resource, IDistributedApplicationBuilder builder)
    {
        using var services = builder.Services.BuildServiceProvider();
        var result = await ExecutionConfigurationBuilder.Create(resource).WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish) { ServiceProvider = services }));
        Assert.Null(result.Exception);
        return result.EnvironmentVariables.ToDictionary(p => p.Key, p => p.Value);
    }

    private IDistributedApplicationBuilder CreateBuilder(string provider, bool? prepare = null, [CallerFilePath] string testFile = "")
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = [],
            AssemblyName = typeof(AppHostProgram).Assembly.FullName,
            ProjectDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testFile), "..", "..", "NexusForever.Aspire.AppHost")),
            DisableDashboard = true
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["AppHost:DatabaseProvider"] = provider,
            ["AppHost:SqliteDirectory"] = Path.Combine(temporaryDirectory, "sqlite"),
            ["Assets:Root"] = Path.Combine(temporaryDirectory, "shared assets"),
            ["Assets:MaxParallelism"] = "2"
        });
        if (prepare.HasValue)
            builder.Configuration["Assets:Prepare"] = prepare.Value.ToString();
        return builder;
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
            Directory.Delete(temporaryDirectory, recursive: true);
    }
}
