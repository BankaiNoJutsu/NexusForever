using System.Collections;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Reputation;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Reputation;

namespace NexusForever.Game.Reputation
{
    public partial class ReputationManager : IReputationManager
    {
        private readonly IPlayer owner;
        private readonly IFactionManager factionManager;
        private readonly Dictionary<Faction, IReputation> reputations = new();
        internal ulong ReputationCharacter { get; }
        internal string ReputationDatabase { get; }
        internal ExperiencePersistence.ReputationState[] LoadedReputations { get; }
        internal (ExperiencePersistence.ReputationState[] Rows, long Sequence) CaptureReputations()
        {
            lock (publicationGate)
            {
                if (owner.CharacterId != ReputationCharacter || reputations.Any(pair => pair.Key != pair.Value.Id))
                    throw new InvalidOperationException("Reputation changed its Character owner or faction.");
                return (reputations.Values.Select(row => new ExperiencePersistence.ReputationState(ReputationCharacter, (uint)row.Id, row.Amount))
                    .OrderBy(row => row.Faction).ToArray(), sequence);
            }
        }
        internal bool Owns(IReputation reputation) => reputations.TryGetValue(reputation.Id, out var owned) && ReferenceEquals(owned, reputation);

        /// <summary>
        /// Create a new <see cref="IReputationManager"/> from existing <see cref="CharacterModel"/> database model.
        /// </summary>
        public ReputationManager(IPlayer player, CharacterModel model, IFactionManager factionManager = null)
        {
            owner = player;
            this.factionManager = factionManager;
            ReputationCharacter = model.Id;
            ReputationDatabase = CharacterLoadOrigin.Get(model)?.Database;
            LoadedReputations = model.Reputation.OrderBy(row => row.FactionId).Select(ExperiencePersistence.FromModel).ToArray();

            foreach (CharacterReputation reputationModel in model.Reputation)
            {
                IFactionNode faction = this.factionManager.GetFaction((Faction)reputationModel.FactionId);
                if (faction == null)
                    throw new DatabaseDataException($"Character {model.Id} has invalid faction {reputationModel.FactionId} stored!");

                var reputation = new Reputation(owner, faction, reputationModel);
                reputations.Add(reputation.Id, reputation);
            }
            if (owner is Player) InitialisePublication();
        }

        public void Save(CharacterContext context)
        {
            if (owner is Player player)
            {
                if (!ReferenceEquals(player.ReputationManager, this))
                {
                    var error = new InvalidOperationException("Reputation lost its loaded manager.");
                    context.RejectSave(error); throw error;
                }
                player.SaveExperience(context); return;
            }
            foreach (IReputation reputation in reputations.Values)
                reputation.Save(context);
        }

        public bool HasResourceConversionFaction(Faction factionId)
            => owner.CharacterId == ReputationCharacter && factionManager?.GetFaction(factionId)?.FactionId == factionId;

        /// <summary>
        /// Update <see cref="IReputation"/> for supplied <see cref="Faction"/> and value.
        /// </summary>
        /// <remarks>
        /// Value is a delta, if a negative value is supplied it will be deducted from the existing reputation if any.
        /// </remarks>
        public void UpdateReputation(Faction factionId, float value)
        {
            IFactionNode faction = factionManager.GetFaction(factionId);
            if (faction == null)
                throw new ArgumentException($"Invalid faction id {factionId}!");

            FactionLevel previousLevel = FactionNode.GetFactionLevel(0f);
            FactionLevel currentLevel;
            lock (publicationGate)
            {
                float? before = null;
                if (!reputations.TryGetValue(factionId, out IReputation reputation))
                {
                    reputation = new Reputation(owner, faction, value);
                    reputations.Add(reputation.Id, reputation);
                }
                else
                {
                    before = reputation.Amount;
                    previousLevel = FactionNode.GetFactionLevel(reputation.Amount);
                    reputation.Amount += value;
                }
                currentLevel = FactionNode.GetFactionLevel(reputation.Amount);
                if (owner is Player)
                    pending.Enqueue(new(++sequence, factionId, value, before));
            }

            if (currentLevel > previousLevel)
                for (int level = (int)previousLevel + 1; level <= (int)currentLevel; level++)
                    owner.AchievementManager.CheckAchievements(owner, AchievementType.ReputationLevel, (uint)factionId, (uint)(FactionLevel)level);

            if (previousLevel < FactionLevel.Beloved && currentLevel >= FactionLevel.Beloved)
                owner.AchievementManager.CheckAchievements(owner, AchievementType.GuildBelovedReputation, (uint)FactionLevel.Beloved);

            if (owner is not Player)
                owner.Session.EnqueueMessageEncrypted(new ServerReputationUpdate { FactionId = factionId, ReputationDelta = value });
        }

        /// <summary>
        /// Get <see cref="IReputation"/> for supplied <see cref="Faction"/>.
        /// </summary>
        public IReputation GetReputation(Faction factionId)
        {
            IFactionNode faction = factionManager.GetFaction(factionId);
            if (faction == null)
                throw new ArgumentException($"Invalid faction id {factionId}!");

            return reputations.TryGetValue(factionId, out IReputation reputation) ? reputation : null;
        }

        public IEnumerator<IReputation> GetEnumerator()
        {
            return reputations.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
