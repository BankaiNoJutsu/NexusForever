using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell
{
    public class SpellBaseInfo : ISpellBaseInfo
    {
        public Spell4BaseEntry Entry { get; }
        public Spell4HitResultsEntry HitResult { get; }
        public Spell4TargetMechanicsEntry TargetMechanics { get; }
        public Spell4TargetAngleEntry TargetAngle { get; }
        public Spell4PrerequisitesEntry Prerequisites { get; }
        public SpellPrerequisiteFlags PrerequisiteFlags { get; }
        public Spell4ValidTargetsEntry ValidTargets { get; }
        public TargetGroupEntry CastGroup { get; }
        public Creature2Entry PositionalAoe { get; }
        public TargetGroupEntry AoeGroup { get; }
        public Spell4BaseEntry PrerequisiteSpell { get; }
        public Spell4SpellTypesEntry SpellType { get; }
        public SpellClass SpellClass { get; }
        public SpellCastMethod CastMethod { get; }
        public SpellSchool School { get; }
        public SpellTargetingFlags TargetingFlags { get; }
        public bool HasIcon { get; }
        public bool IsDebuff { get; }
        public bool IsBuff { get; }
        public bool IsDispellable { get; }
        public bool IsFreeformTarget { get; }
        public bool IsMovingInterrupted { get; }

        private readonly ISpellInfo[] spellInfoStore;
        private readonly IGlobalSpellManager globalSpellManager;
        private readonly IGameTableManager gameTableManager;

        public SpellBaseInfo(
            Spell4BaseEntry spell4BaseEntry,
            IGlobalSpellManager globalSpellManager = null,
            IGameTableManager gameTableManager = null)
        {
            this.globalSpellManager = globalSpellManager;
            this.gameTableManager = gameTableManager;

            Entry             = spell4BaseEntry;
            HitResult         = gameTableManager.Spell4HitResults?.GetEntry(Entry.Spell4HitResultId);
            TargetMechanics   = gameTableManager.Spell4TargetMechanics?.GetEntry(Entry.Spell4TargetMechanicId);
            TargetAngle       = gameTableManager.Spell4TargetAngle?.GetEntry(Entry.Spell4TargetAngleId);
            Prerequisites     = gameTableManager.Spell4Prerequisites?.GetEntry(Entry.Spell4PrerequisiteId);
            PrerequisiteFlags = (SpellPrerequisiteFlags)(Prerequisites?.Flags ?? 0u);
            ValidTargets      = gameTableManager.Spell4ValidTargets?.GetEntry(Entry.Spell4ValidTargetId);
            CastGroup         = gameTableManager.TargetGroup?.GetEntry(Entry.TargetGroupIdCastGroup);
            PositionalAoe     = gameTableManager.Creature2?.GetEntry(Entry.Creature2IdPositionalAoe);
            AoeGroup          = gameTableManager.TargetGroup?.GetEntry(Entry.TargetGroupIdAoeGroup);
            PrerequisiteSpell = gameTableManager.Spell4Base?.GetEntry(Entry.Spell4BaseIdPrerequisiteSpell);
            SpellType         = gameTableManager.Spell4SpellTypes?.GetEntry(Entry.Spell4SpellTypesIdSpellType);

            SpellClass        = (SpellClass)Entry.SpellClass;
            CastMethod        = (SpellCastMethod)Entry.CastMethod;
            School            = (SpellSchool)Entry.School;
            TargetingFlags    = (SpellTargetingFlags)Entry.TargetingFlags;
            HasIcon           = SpellClass == SpellClass.BuffNonDispelRightClickOk || (SpellClass >= SpellClass.BuffDispellable && SpellClass <= SpellClass.DebuffNonDispellable);
            IsDebuff          = SpellClass == SpellClass.DebuffDispellable || SpellClass == SpellClass.DebuffNonDispellable;
            IsBuff            = SpellClass == SpellClass.BuffDispellable || SpellClass == SpellClass.BuffNonDispellable || SpellClass == SpellClass.BuffNonDispelRightClickOk;
            IsDispellable     = SpellClass == SpellClass.BuffDispellable || SpellClass == SpellClass.DebuffDispellable;
            IsFreeformTarget  = (TargetingFlags & SpellTargetingFlags.FreeformTarget) != 0;
            IsMovingInterrupted = (TargetingFlags & SpellTargetingFlags.InterruptOnMove) != 0;

            List<Spell4Entry> spellEntries = GetGlobalSpellManager().GetSpell4Entries(spell4BaseEntry.Id).ToList();
            if (spellEntries.Count < 1)
                return;

            // spell don't always have sequential tiers, create from highest tier not total
            if (spellInfoStore == null)
                spellInfoStore = new SpellInfo[spellEntries[0].TierIndex];

            foreach (Spell4Entry spell4Entry in spellEntries)
            {
                var spellInfo = new SpellInfo(this, spell4Entry, GetGlobalSpellManager(), gameTableManager);
                spellInfoStore[spell4Entry.TierIndex - 1] = spellInfo;
            }
        }

        private IGlobalSpellManager GetGlobalSpellManager()
        {
            return globalSpellManager ?? throw new InvalidOperationException($"{nameof(SpellBaseInfo)} requires an {nameof(IGlobalSpellManager)}.");
        }

        /// <summary>
        /// Return <see cref="ISpellInfo"/> for the supplied spell tier.
        /// </summary>
        public ISpellInfo GetSpellInfo(byte tier)
        {
            if (tier < 1)
                tier = 1;

            if (spellInfoStore == null || tier > spellInfoStore.Length)
                return null;

            return spellInfoStore[tier - 1];
        }
    }
}
