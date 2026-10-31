using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace NexusForever.Aspire.AppHost;

internal sealed record AssetPreparationContext(string Root, IResourceBuilder<ProjectResource> Resource)
{
    internal const int MaximumParallelism = 8;

    internal static AssetPreparationContext? Create(IDistributedApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue<bool>("Assets:Prepare"))
            return null;

        int parallelism = builder.Configuration.GetValue<int>("Assets:MaxParallelism");
        if (parallelism < 0 || parallelism > MaximumParallelism)
            throw new InvalidOperationException($"Assets:MaxParallelism must be between 0 (automatic) and {MaximumParallelism}.");

        string repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", ".."));
        string? configuredRoot = builder.Configuration["Assets:Root"];
        string root = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(".nexusforever-runtime", "assets") : configuredRoot, repositoryRoot);

        var resource = builder.AddProject<Projects.NexusForever_MapGenerator>("asset-preparation")
            .WithArgs("--prepare", "--output", root,
                "--maxParallelism", parallelism.ToString(CultureInfo.InvariantCulture));

        string? clientPath = builder.Configuration["Assets:ClientPath"];
        if (!string.IsNullOrWhiteSpace(clientPath))
            resource.WithArgs("--patchPath", Path.GetFullPath(clientPath, repositoryRoot));

        return new AssetPreparationContext(root, resource);
    }
}

internal static class AssetPreparationResourceExtensions
{
    internal static IResourceBuilder<ProjectResource> WithPreparedAssets(this IResourceBuilder<ProjectResource> builder,
        AssetPreparationContext? assets, bool includeMaps = false)
    {
        if (assets is null)
            return builder;

        builder.WaitForCompletion(assets.Resource, exitCode: 0)
            .WithEnvironment("GameTable:GameTablePath", Path.Combine(assets.Root, "tbl"));
        if (includeMaps)
            builder.WithEnvironment("Realm:Map:MapPath", Path.Combine(assets.Root, "map"));

        return builder;
    }
}
