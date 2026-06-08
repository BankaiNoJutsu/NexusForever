using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusForever.Aspire.Database.Migrations.Configuration.Model;
using NexusForever.Database;
using NexusForever.Database.World;
using NexusForever.Database.World.Model;

namespace NexusForever.Aspire.Database.Migrations.Service
{
    public class WorldDatabaseHostedService : IHostedService
    {
        private const string NewPlayerExperienceFileName = "New Player Experience.sql";
        private const ushort NewPlayerExperienceWorldId = 3460;
        private static readonly uint[] RequiredNewPlayerExperienceCreatureIds =
        [
            70939u,
            72051u,
            73416u,
            73419u,
            73461u,
            73463u,
            73595u,
            73610u,
            73619u,
            73667u,
            73668u,
            73707u,
            73735u,
            73736u,
            74767u,
            74768u,
            74769u,
            75094u,
            75096u
        ];

        private static readonly string[] RequiredNewPlayerExperienceScriptNames =
        [
            "DirectionArrowEntityScript",
            "DirectionArrowPt2EntityScript",
            "ObjectiveRingEntityScript",
            "ObjectiveRingPt2EntityScript"
        ];

        #region Dependency Injection

        private readonly ILogger<WorldDatabaseHostedService> _log;
        private readonly WorldDatabaseOptions _options;
        private readonly WorldContext _context;

        public WorldDatabaseHostedService(
            ILogger<WorldDatabaseHostedService> log,
            IOptions<WorldDatabaseOptions> options,
            WorldContext context)
        {
            _log     = log;
            _options = options.Value;
            _context = context;
        }

        #endregion

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_options.Path == null)
            {
                _log.LogWarning("World database options are not configured. Skipping migration.");
                return;
            }

            if (!Directory.Exists(_options.Path))
            {
                _log.LogWarning("World database migrations path does not exist. Skipping migration.");
                return;
            }

            foreach (string filePath in Directory.GetFiles(_options.Path, "*.sql", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                await ApplySqlFile(filePath, Path.GetFileName(filePath), cancellationToken);
            }

            await ValidateNewPlayerExperienceImport(cancellationToken);

            foreach (string runtimeSeedPath in _options.RuntimeSeedPaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                if (!File.Exists(runtimeSeedPath))
                {
                    _log.LogWarning("Runtime world seed path does not exist. Skipping seed import: {RuntimeSeedPath}", runtimeSeedPath);
                    continue;
                }

                await ApplySqlFile(runtimeSeedPath, $"DataMapping/{Path.GetFileName(runtimeSeedPath)}", cancellationToken);
            }
        }

        private async Task ApplySqlFile(string filePath, string markerName, CancellationToken cancellationToken)
        {
            string fileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            string fileHash    = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(fileContent)));

            if (await _context.Version.AnyAsync(v => v.FileName == markerName && v.FileHash == fileHash, cancellationToken))
            {
                _log.LogInformation("Skipping already applied world database migration: {FileName}", markerName);
                return;
            }

            _log.LogInformation("Applying world database migration: {FileName}", markerName);
            try
            {
                if (string.Equals(_context.Database.ProviderName, Extensions.SqliteProviderName, StringComparison.Ordinal))
                {
                    var importer = new SqliteWorldSqlImporter(_log, _context);
                    await importer.ImportFileAsync(filePath, cancellationToken);
                }
                else
                {
                    fileContent = Regex.Replace(fileContent, @"/\*.*?\*/", "", RegexOptions.Singleline);
                    fileContent = Regex.Replace(fileContent, @"--.*?$", "", RegexOptions.Multiline);
                    fileContent = fileContent.Trim();
                    await _context.Database.ExecuteSqlRawAsync(fileContent);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to apply world database migration: {FileName}", markerName);
                throw;
            }
            _log.LogInformation("Applied world database migration: {FileName}", markerName);

            _context.Version.Add(new VersionModel
            {
                FileName = markerName,
                FileHash = fileHash,
                AppliedOn = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task ValidateNewPlayerExperienceImport(CancellationToken cancellationToken)
        {
            if (!Directory
                .GetFiles(_options.Path, NewPlayerExperienceFileName, SearchOption.AllDirectories)
                .Any())
            {
                throw new InvalidOperationException($"WorldDatabase path does not contain required NPE import file: {NewPlayerExperienceFileName}");
            }

            if (!await _context.Version.AnyAsync(v => v.FileName == NewPlayerExperienceFileName, cancellationToken))
                throw new InvalidOperationException($"WorldDatabase import did not record required NPE import file: {NewPlayerExperienceFileName}");

            bool hasNpeEntities = await _context.Entity
                .AnyAsync(e => e.World == NewPlayerExperienceWorldId, cancellationToken);
            if (!hasNpeEntities)
                throw new InvalidOperationException($"WorldDatabase import did not create entities for world {NewPlayerExperienceWorldId}.");

            bool hasNpeEntityScripts = await _context.EntityScript
                .AnyAsync(s => _context.Entity.Any(e => e.Id == s.Id && e.World == NewPlayerExperienceWorldId), cancellationToken);
            if (!hasNpeEntityScripts)
                throw new InvalidOperationException($"WorldDatabase import did not create entity_script rows for world {NewPlayerExperienceWorldId}.");

            uint[] importedCreatureIds = await _context.Entity
                .Where(e => e.World == NewPlayerExperienceWorldId)
                .Select(e => e.Creature)
                .Distinct()
                .ToArrayAsync(cancellationToken);

            uint[] missingCreatureIds = RequiredNewPlayerExperienceCreatureIds
                .Except(importedCreatureIds)
                .ToArray();
            if (missingCreatureIds.Length != 0)
                throw new InvalidOperationException($"WorldDatabase import is missing required Rider's Reef creature rows for world {NewPlayerExperienceWorldId}: {string.Join(", ", missingCreatureIds)}.");

            string[] importedScriptNames = await _context.EntityScript
                .Where(s => _context.Entity.Any(e => e.Id == s.Id && e.World == NewPlayerExperienceWorldId))
                .Select(s => s.ScriptName)
                .Distinct()
                .ToArrayAsync(cancellationToken);

            string[] missingScriptNames = RequiredNewPlayerExperienceScriptNames
                .Except(importedScriptNames)
                .ToArray();
            if (missingScriptNames.Length != 0)
                throw new InvalidOperationException($"WorldDatabase import is missing required Rider's Reef entity_script rows for world {NewPlayerExperienceWorldId}: {string.Join(", ", missingScriptNames)}.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
