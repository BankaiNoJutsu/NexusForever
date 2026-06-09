using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell
{
    public class SpellInfo : ISpellInfo
    {
        public Spell4Entry Entry { get; }
        public ISpellBaseInfo BaseInfo { get; }
        public SpellPropertyFlags PropertyFlags { get; }
        public bool HideCooldownInTooltip { get; }
        public bool IsBeneficial { get; }
        public bool HasServiceTokenCost { get; }
        public Spell4ServiceTokenCostEntry ServiceTokenCostEntry { get; }
        public Spell4AoeTargetConstraintsEntry AoeTargetConstraints { get; }
        public Spell4ConditionsEntry CasterConditions { get; }
        public Spell4ConditionsEntry TargetConditions { get; }
        public Spell4CCConditionsEntry CasterCCConditions { get; }
        public Spell4CCConditionsEntry TargetCCConditions { get; }
        public SpellCoolDownEntry GlobalCooldown { get; }
        public Spell4StackGroupEntry StackGroup { get; }
        public PrerequisiteEntry CasterCastPrerequisite { get; }
        public PrerequisiteEntry TargetCastPrerequisites { get; }
        public PrerequisiteEntry CasterPersistencePrerequisites { get; }
        public PrerequisiteEntry TargetPersistencePrerequisites { get; }
        public List<PrerequisiteEntry> PrerequisiteRunners { get; } = new List<PrerequisiteEntry>();

        public List<TelegraphDamageEntry> Telegraphs { get; }
        public List<Spell4EffectsEntry> Effects { get; }
        public List<Spell4ThresholdsEntry> Thresholds { get; }

        private Dictionary<uint, (ISpellInfo SpellInfo, Spell4ThresholdsEntry Threshold)> thresholdCache;
        private (ISpellInfo SpellInfo, Spell4ThresholdsEntry Threshold) maxThresholdSpell;

        public SpellInfo(ISpellBaseInfo spellBaseBaseInfo, Spell4Entry spell4Entry)
        {
            Entry                          = spell4Entry;
            BaseInfo                       = spellBaseBaseInfo;
            PropertyFlags                  = (SpellPropertyFlags)spell4Entry.PropertyFlags;
            HideCooldownInTooltip          = (PropertyFlags & SpellPropertyFlags.HideCooldownInTooltip) != 0;
            IsBeneficial                   = (PropertyFlags & SpellPropertyFlags.IsBeneficial) != 0;
            HasServiceTokenCost            = (PropertyFlags & SpellPropertyFlags.HasServiceTokenCost) != 0;
            IEnumerable<Spell4ServiceTokenCostEntry> serviceTokenCostEntries =
                GameTableManager.Instance.Spell4ServiceTokenCost?.Entries ?? Enumerable.Empty<Spell4ServiceTokenCostEntry>();
            ServiceTokenCostEntry          = serviceTokenCostEntries.FirstOrDefault(e => e.Spell4Id == spell4Entry.Id);
            AoeTargetConstraints           = GameTableManager.Instance.Spell4AoeTargetConstraints?.GetEntry(spell4Entry.Spell4AoeTargetConstraintsId);
            CasterConditions               = GameTableManager.Instance.Spell4Conditions?.GetEntry(spell4Entry.Spell4ConditionsIdCaster);
            TargetConditions               = GameTableManager.Instance.Spell4Conditions?.GetEntry(spell4Entry.Spell4ConditionsIdTarget);
            CasterCCConditions             = GameTableManager.Instance.Spell4CCConditions?.GetEntry(spell4Entry.Spell4CCConditionsIdCaster);
            TargetCCConditions             = GameTableManager.Instance.Spell4CCConditions?.GetEntry(spell4Entry.Spell4CCConditionsIdTarget);
            GlobalCooldown                 = GameTableManager.Instance.SpellCoolDown?.GetEntry(spell4Entry.SpellCoolDownIdGlobal);
            StackGroup                     = GameTableManager.Instance.Spell4StackGroup?.GetEntry(spell4Entry.Spell4StackGroupId);
            CasterCastPrerequisite         = GameTableManager.Instance.Prerequisite?.GetEntry(spell4Entry.PrerequisiteIdCasterCast);
            TargetCastPrerequisites        = GameTableManager.Instance.Prerequisite?.GetEntry(spell4Entry.PrerequisiteIdTargetCast);
            CasterPersistencePrerequisites = GameTableManager.Instance.Prerequisite?.GetEntry(spell4Entry.PrerequisiteIdCasterPersistence);
            TargetPersistencePrerequisites = GameTableManager.Instance.Prerequisite?.GetEntry(spell4Entry.PrerequisiteIdTargetPersistence);

            Telegraphs                     = GlobalSpellManager.Instance.GetTelegraphDamageEntries(spell4Entry.Id).ToList();
            Effects                        = GlobalSpellManager.Instance.GetSpell4EffectEntries(spell4Entry.Id).ToList();
            Thresholds                     = GlobalSpellManager.Instance.GetSpell4ThresholdEntries(spell4Entry.Id).ToList();

            foreach (uint runnerId in (spell4Entry.PrerequisiteIdRunners ?? []).Where(r => r != 0))
                PrerequisiteRunners.Add(GameTableManager.Instance.Prerequisite?.GetEntry(runnerId));
        }

        /// <summary>
        /// Return the threshold child spell for the supplied threshold stage.
        /// </summary>
        public ISpellInfo GetThresholdSpellInfo(uint thresholdIndex, out Spell4ThresholdsEntry thresholdEntry)
        {
            thresholdEntry = null;

            if (Thresholds.Count == 0)
                return null;

            InitialiseThresholdCache();

            if (thresholdCache.TryGetValue(thresholdIndex, out (ISpellInfo SpellInfo, Spell4ThresholdsEntry Threshold) value))
            {
                thresholdEntry = value.Threshold;
                return value.SpellInfo;
            }

            thresholdEntry = maxThresholdSpell.Threshold;
            return maxThresholdSpell.SpellInfo;
        }

        private void InitialiseThresholdCache()
        {
            if (thresholdCache != null)
                return;

            thresholdCache = new Dictionary<uint, (ISpellInfo SpellInfo, Spell4ThresholdsEntry Threshold)>();

            foreach (Spell4ThresholdsEntry thresholdsEntry in Thresholds)
            {
                Spell4Entry spell4Entry = GameTableManager.Instance.Spell4?.GetEntry(thresholdsEntry.Spell4IdToCast);
                if (spell4Entry == null)
                    continue;

                ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4Entry.Spell4BaseIdBaseSpell);
                ISpellInfo spellInfo = spellBaseInfo.GetSpellInfo((byte)spell4Entry.TierIndex);
                if (spellInfo == null)
                    continue;

                thresholdCache.TryAdd(thresholdsEntry.OrderIndex, (spellInfo, thresholdsEntry));
            }

            if (thresholdCache.Count == 0)
                return;

            Spell4ThresholdsEntry maxThreshold = Thresholds.MaxBy(t => t.OrderIndex);
            maxThresholdSpell = thresholdCache.TryGetValue(maxThreshold.OrderIndex, out (ISpellInfo SpellInfo, Spell4ThresholdsEntry Threshold) value)
                ? value
                : thresholdCache.OrderByDescending(t => t.Key).First().Value;
        }
    }
}
