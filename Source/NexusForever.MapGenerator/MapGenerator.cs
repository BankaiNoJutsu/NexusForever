using System;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.MapGenerator.GameTable;
using NLog;

namespace NexusForever.MapGenerator
{
    internal static class MapGenerator
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static ParserResult<Parameters> parserResult;

        #if DEBUG
        private const string Title = "NexusForever: Map Generator (DEBUG)";
        #else
        private const string Title = "NexusForever: Map Generator (RELEASE)";
        #endif

        private static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<ArchiveManager>();
            services.AddSingleton<GameTableManager>();
            services.AddSingleton<ExtractionManager>();
            services.AddSingleton<GenerationManager>();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();

            Console.Title = Title;

            parserResult = Parser.Default.ParseArguments<Parameters>(args);
            parserResult.WithParsed(parameters =>
            {
                if (!string.IsNullOrWhiteSpace(parameters.ValidateTables) || !string.IsNullOrWhiteSpace(parameters.ValidateMaps))
                {
                    if (parameters.Prepare || parameters.Extract || parameters.Generate)
                        throw new ArgumentException("Validation cannot be combined with asset generation.");
                    if (!string.IsNullOrWhiteSpace(parameters.ValidateTables)) AssetPreparation.ValidateTables(parameters.ValidateTables);
                    if (!string.IsNullOrWhiteSpace(parameters.ValidateMaps)) AssetPreparation.ValidateMaps(parameters.ValidateMaps);
                    return;
                }
                void Generate(Parameters selected) => ParameterOk(selected,
                    serviceProvider.GetRequiredService<ArchiveManager>(),
                    serviceProvider.GetRequiredService<GameTableManager>(),
                    serviceProvider.GetRequiredService<ExtractionManager>(),
                    serviceProvider.GetRequiredService<GenerationManager>());
                if (parameters.Prepare) AssetPreparation.Run(parameters, Generate);
                else Generate(parameters);
            }).WithNotParsed(errors => Environment.ExitCode = errors.All(error =>
                error.Tag is ErrorType.HelpRequestedError or ErrorType.VersionRequestedError) ? 0 : 1);

            log.Info("Finished!");
        }

        private static void ParameterOk(
            Parameters parameters,
            ArchiveManager archiveManager,
            GameTableManager gameTableManager,
            ExtractionManager extractionManager,
            GenerationManager generationManager)
        {
            if (string.IsNullOrWhiteSpace(parameters.PatchPath) || !Directory.Exists(parameters.PatchPath))
                throw new DirectoryNotFoundException();

            if (parameters.MaxParallelism < 0)
                throw new ArgumentOutOfRangeException(nameof(parameters.MaxParallelism), "maxParallelism must be 0 or greater.");

            if (!parameters.Extract && !parameters.Generate)
            {
                log.Warn("Please specify the Extract or Generate parameter");
                log.Info(GetHelp());
                return;
            }

            if ((parameters.Extract || parameters.Generate) && !string.IsNullOrEmpty(parameters.OutputDir))
            {
                if (!Directory.Exists(parameters.OutputDir))
                    throw new DirectoryNotFoundException(parameters.OutputDir);
            }

            archiveManager.Initialise(parameters.PatchPath);
            gameTableManager.Initialise();

            if (parameters.Extract)
                extractionManager.Initialise(parameters.OutputDir, parameters.MaxParallelism);
            if (parameters.Generate)
            {
                generationManager.Initialise(parameters.OutputDir);

                var start = DateTime.UtcNow;
                if (parameters.WorldId.HasValue)
                    generationManager.GenerateWorld(parameters.WorldId.Value, parameters.GridX, parameters.GridY, parameters.MaxParallelism);
                else
                    generationManager.GenerateWorlds(parameters.MaxParallelism);

                TimeSpan span = DateTime.UtcNow - start;
                log.Info($"Generated base maps in {span.TotalSeconds}s.");
            }
        }

        private static string GetHelp()
        {
            return HelpText.AutoBuild(parserResult, h => h, e => e);
        }
    }
}
