using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Prerequisite
{
    public sealed class PrerequisiteManager : IPrerequisiteManager
    {
        #region Dependency Injection

        private readonly ILogger<PrerequisiteManager> log;
        private readonly IServiceProvider serviceProvider;
        private readonly IGameTableManager gameTableManager;
        private readonly IFactory<IPrerequisiteParameters> prerequisiteParametersFactory;

        public PrerequisiteManager(
            ILogger<PrerequisiteManager> log,
            IServiceProvider serviceProvider,
            IGameTableManager gameTableManager,
            IFactory<IPrerequisiteParameters> prerequisiteParametersFactory)
        {
            this.log                           = log;
            this.serviceProvider               = serviceProvider;
            this.gameTableManager              = gameTableManager;
            this.prerequisiteParametersFactory = prerequisiteParametersFactory;
        }

        #endregion

        public bool MeetsUnit(IUnitEntity subject, uint prerequisiteId, IPrerequisiteParameters parameters = null)
        {
            if (subject == null)
                return false;

            parameters ??= prerequisiteParametersFactory.Resolve();
            PrerequisiteEntry entry = gameTableManager?.Prerequisite?.GetEntry(prerequisiteId);
            if (entry == null)
            {
                log.LogWarning("Missing prerequisite row {PrerequisiteId}; failing unit prerequisite check closed.", prerequisiteId);
                return false;
            }

            if (entry.Flags is not EvaluationMode.EvaluateAND and not EvaluationMode.EvaluateOR)
                return false;

            bool evaluateAnd = entry.Flags == EvaluationMode.EvaluateAND;
            for (int i = 0; i < entry.PrerequisiteTypeId.Length; i++)
            {
                PrerequisiteType type = entry.PrerequisiteTypeId[i];
                if (type == PrerequisiteType.None)
                    continue;

                IPrerequisiteCheck handler = serviceProvider.GetKeyedService<IPrerequisiteCheck>(type);
                bool result = handler != null && handler.MeetsUnit(subject, entry.PrerequisiteComparisonId[i], entry.Value[i], entry.ObjectId[i], parameters);
                if (handler == null)
                    log.LogWarning("Unhandled unit PrerequisiteType {Type}!", type);

                if (evaluateAnd != result)
                    return result;
            }

            return evaluateAnd;
        }

        /// <summary>
        /// Checks if <see cref="IPlayer"/> meets supplied prerequisite.
        /// </summary>
        public bool Meets(IPlayer player, uint prerequisiteId)
        {
            IPrerequisiteParameters parameters = prerequisiteParametersFactory.Resolve();
            return Meets(player, prerequisiteId, parameters);
        }

        /// <summary>
        /// Checks if <see cref="IPlayer"/> meets supplied prerequisite.
        /// </summary>
        public bool Meets(IPlayer player, uint prerequisiteId, IPrerequisiteParameters parameters)
        {
            PrerequisiteEntry entry = gameTableManager?.Prerequisite?.GetEntry(prerequisiteId);
            if (entry == null)
            {
                log.LogWarning("Missing prerequisite row {PrerequisiteId}; failing prerequisite check closed.", prerequisiteId);
                return false;
            }

            switch (entry.Flags)
            {
                case EvaluationMode.EvaluateAND:
                    return MeetsEvaluateAnd(player, prerequisiteId, entry, parameters);
                case EvaluationMode.EvaluateOR:
                    return MeetsEvaluateOr(player, prerequisiteId, entry, parameters);
                default:
                    log.LogTrace($"Unhandled EvaluationMode {entry.Flags}");
                    return false;
            }
        }

        private bool MeetsEvaluateAnd(IPlayer player, uint prerequisiteId, PrerequisiteEntry entry, IPrerequisiteParameters parameters)
        {
            for (int i = 0; i < entry.PrerequisiteTypeId.Length; i++)
            {
                PrerequisiteType type = entry.PrerequisiteTypeId[i];
                if (type == PrerequisiteType.None)
                    continue;

                PrerequisiteComparison comparison = entry.PrerequisiteComparisonId[i];
                if (!Meets(player, type, comparison, entry.Value[i], entry.ObjectId[i], parameters))
                {
                    log.LogTrace($"Player {player.Name} failed prerequisite AND check ({prerequisiteId}) {type}, {comparison}, {entry.Value[i]}, {entry.ObjectId[i]}");
                    return false;
                }
            }

            return true;
        }

        private bool MeetsEvaluateOr(IPlayer player, uint prerequisiteId, PrerequisiteEntry entry, IPrerequisiteParameters parameters)
        {
            for (int i = 0; i < entry.PrerequisiteTypeId.Length; i++)
            {
                PrerequisiteType type = entry.PrerequisiteTypeId[i];
                if (type == PrerequisiteType.None)
                    continue;

                if (Meets(player, type, entry.PrerequisiteComparisonId[i], entry.Value[i], entry.ObjectId[i], parameters))
                    return true;
            }

            log.LogTrace($"Player {player.Name} failed prerequisite OR check ({prerequisiteId})");
            return false;
        }

        private bool Meets(IPlayer player, PrerequisiteType type, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (PrerequisiteItemTypes.RequiresItemContext(type) && parameters?.Item == null)
                log.LogTrace("Prerequisite {Type} expects item-eval context but IPrerequisiteParameters.Item is null.", type);

            IPrerequisiteCheck handler = serviceProvider.GetKeyedService<IPrerequisiteCheck>(type);
            if (handler == null)
            {
                log.LogWarning($"Unhandled PrerequisiteType {type}!");
                return false;
            }

            return handler.Meets(player, comparison, value, objectId, parameters);
        }
    }
}
