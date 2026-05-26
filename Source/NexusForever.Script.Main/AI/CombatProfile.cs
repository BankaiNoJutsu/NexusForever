using System.Reflection;
using System.Text.Json;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Script.Main.AI
{
    public sealed record CombatSpecialAttack(
        uint Spell4Id,
        double CooldownSeconds,
        float? MaxRange = null,
        bool FaceTarget = true);

    public sealed record CombatProfile(
        IReadOnlyList<uint> AutoAttackSpell4Ids,
        IReadOnlyList<CombatSpecialAttack> SpecialAttacks,
        uint AggroSpell4Id,
        float ChaseDistance,
        float? AggroRange,
        float? MinimumLeashRange,
        float AssistRange,
        bool Stationary,
        bool TraceCombat,
        bool AllowNonPlayerTargets)
    {
        public static CombatProfile Default { get; } = new(
            AutoAttackSpell4Ids: [5649u, 5652u],
            SpecialAttacks: [],
            AggroSpell4Id: 0u,
            ChaseDistance: 5f,
            AggroRange: null,
            MinimumLeashRange: null,
            AssistRange: 0f,
            Stationary: false,
            TraceCombat: false,
            AllowNonPlayerTargets: false);
    }

    public interface ICombatProfileProvider
    {
        CombatProfile GetProfile(ICreatureEntity creature);
    }

    public interface IDefaultCombatProfileProvider : ICombatProfileProvider
    {
        CombatProfile GetDefaultProfile();
    }

    public sealed class DefaultCombatProfileProvider : IDefaultCombatProfileProvider
    {
        private const string ResourceName = "NexusForever.Script.Main.AI.CombatProfiles.json";

        public static DefaultCombatProfileProvider Instance { get; } = new();

        private static readonly CombatProfileData profileData = LoadProfileData();

        private DefaultCombatProfileProvider()
        {
        }

        public CombatProfile GetProfile(ICreatureEntity creature)
        {
            if (creature != null && profileData.CreatureProfiles.TryGetValue(creature.CreatureId, out CombatProfile profile))
                return profile;

            return null;
        }

        public CombatProfile GetDefaultProfile()
        {
            return profileData.DefaultProfile;
        }

        private static CombatProfileData LoadProfileData()
        {
            using Stream stream = typeof(DefaultCombatProfileProvider).Assembly.GetManifestResourceStream(ResourceName);
            if (stream == null)
                return CombatProfileData.Empty;

            CombatProfileDataFile data = JsonSerializer.Deserialize<CombatProfileDataFile>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data?.Profiles == null || data.Creatures == null)
                return CombatProfileData.Empty;

            Dictionary<string, CombatProfileDefinition> profileDefinitions = data.Profiles
                .Where(profile => !string.IsNullOrWhiteSpace(profile.Name))
                .ToDictionary(profile => profile.Name, StringComparer.OrdinalIgnoreCase);

            CombatProfile defaultProfile = CombatProfile.Default;
            if (!string.IsNullOrWhiteSpace(data.DefaultProfile) && profileDefinitions.TryGetValue(data.DefaultProfile, out CombatProfileDefinition defaultDefinition))
                defaultProfile = ToCombatProfile(defaultDefinition, CombatProfile.Default);

            Dictionary<string, CombatProfile> profiles = profileDefinitions
                .ToDictionary(pair => pair.Key, pair => ToCombatProfile(pair.Value, defaultProfile), StringComparer.OrdinalIgnoreCase);

            Dictionary<uint, CombatProfile> creatureProfiles = [];
            foreach (CreatureProfileDefinition creature in data.Creatures)
            {
                if (creature.Creature2Id == 0u || string.IsNullOrWhiteSpace(creature.Profile))
                    continue;

                if (!profiles.TryGetValue(creature.Profile, out CombatProfile profile))
                    continue;

                creatureProfiles[creature.Creature2Id] = profile;
            }

            return new CombatProfileData(creatureProfiles, defaultProfile);
        }

        private static CombatProfile ToCombatProfile(CombatProfileDefinition definition, CombatProfile fallback)
        {
            return fallback with
            {
                AutoAttackSpell4Ids  = definition.AutoAttackSpell4Ids ?? fallback.AutoAttackSpell4Ids,
                SpecialAttacks       = definition.SpecialAttacks?.Select(ToCombatSpecialAttack).ToList() ?? fallback.SpecialAttacks,
                AggroSpell4Id        = definition.AggroSpell4Id ?? fallback.AggroSpell4Id,
                ChaseDistance        = definition.ChaseDistance ?? fallback.ChaseDistance,
                AggroRange           = definition.AggroRange,
                MinimumLeashRange    = definition.MinimumLeashRange,
                AssistRange          = definition.AssistRange ?? fallback.AssistRange,
                Stationary           = definition.Stationary ?? fallback.Stationary,
                TraceCombat          = definition.TraceCombat ?? fallback.TraceCombat,
                AllowNonPlayerTargets = definition.AllowNonPlayerTargets ?? fallback.AllowNonPlayerTargets
            };
        }

        private static CombatSpecialAttack ToCombatSpecialAttack(CombatSpecialAttackDefinition definition)
        {
            return new CombatSpecialAttack(
                definition.Spell4Id,
                definition.CooldownSeconds,
                definition.MaxRange,
                definition.FaceTarget ?? true);
        }

        private sealed record CombatProfileData(IReadOnlyDictionary<uint, CombatProfile> CreatureProfiles, CombatProfile DefaultProfile)
        {
            public static CombatProfileData Empty { get; } = new(new Dictionary<uint, CombatProfile>(), CombatProfile.Default);
        }

        private sealed class CombatProfileDataFile
        {
            public string DefaultProfile { get; set; }
            public List<CombatProfileDefinition> Profiles { get; set; } = [];
            public List<CreatureProfileDefinition> Creatures { get; set; } = [];
        }

        private sealed class CombatProfileDefinition
        {
            public string Name { get; set; }
            public IReadOnlyList<uint> AutoAttackSpell4Ids { get; set; }
            public IReadOnlyList<CombatSpecialAttackDefinition> SpecialAttacks { get; set; }
            public uint? AggroSpell4Id { get; set; }
            public float? ChaseDistance { get; set; }
            public float? AggroRange { get; set; }
            public float? MinimumLeashRange { get; set; }
            public float? AssistRange { get; set; }
            public bool? Stationary { get; set; }
            public bool? TraceCombat { get; set; }
            public bool? AllowNonPlayerTargets { get; set; }
        }

        private sealed class CombatSpecialAttackDefinition
        {
            public uint Spell4Id { get; set; }
            public double CooldownSeconds { get; set; }
            public float? MaxRange { get; set; }
            public bool? FaceTarget { get; set; }
        }

        private sealed class CreatureProfileDefinition
        {
            public uint Creature2Id { get; set; }
            public string Profile { get; set; }
        }
    }
}
