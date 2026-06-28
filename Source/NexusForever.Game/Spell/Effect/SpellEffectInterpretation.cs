using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Spell.Effect
{
    public enum SpellEffectParameterRole
    {
        None,
        CasterPrimaryStatCoefficient,
        CasterPowerCoefficient,
        TargetVitalCoefficient,
        CasterVitalCoefficient,
        CasterLevelCoefficient,
        ItemBudget,
        Unknown
    }

    public sealed record SpellEffectDataBit(byte Index, uint RawValue, float FloatValue);

    public sealed record SpellEffectParameter(int Index, SpellEffectParameterType Type, float Value, SpellEffectParameterRole Role);

    public sealed record SpellEffectTiming(uint DelayTime, uint TickTime, uint DurationTime);

    public sealed record SpellEffectDamageSemantics(float TypeMultiplier, float TypeBaseValue);

    public sealed record SpellEffectProxyRandomExclusiveCandidate(uint Spell4Id, uint Weight);

    public sealed record SpellEffectProxyRandomExclusiveSemantics(IReadOnlyList<SpellEffectProxyRandomExclusiveCandidate> Candidates);

    public sealed record SpellEffectTransferenceSemantics(
        Vital HealedVital,
        Vital SourceVital,
        float DamageMultiplier,
        uint BaseValue,
        uint DataBits04,
        float TransferRate);

    public sealed record SpellEffectAbsorptionSemantics(
        float TypeMultiplier,
        float TypeBaseValue,
        uint DataBits02,
        uint DataBits03,
        uint AbsorptionType,
        uint DataBits05);

    public sealed record SpellEffectHealingAbsorptionSemantics(
        float TypeMultiplier,
        float TypeBaseValue,
        uint Mode,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectVitalModifierSemantics(
        Vital Vital,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        float DataFloat05);

    public sealed record SpellEffectUnitStateSetSemantics(
        uint StateId,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectSetBusySemantics(
        bool Busy,
        uint Mode,
        uint ContextId,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectSapVitalSemantics(
        Vital Vital,
        float DataFloat01,
        float DataFloat02,
        uint Mode,
        uint DataBits04,
        float DataFloat05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectSummonCreatureSemantics(
        uint CreatureId,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectSummonPetSemantics(
        uint CreatureId,
        uint PetType,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectSummonVehicleSemantics(
        uint CreatureId,
        uint UnitVehicleId,
        uint BoardMode,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectSummonTrapSemantics(
        uint CreatureId,
        uint TriggerSpell4Id,
        uint ArmTimeMs,
        uint DataBits03,
        float Radius,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07);

    public sealed record SpellEffectNpcExecutionDelaySemantics(
        uint DataBits00,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectRavelSignalSemantics(
        uint Mode,
        uint SignalId,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectSettlerCampfireSemantics(
        uint TierIndex,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectModifyInterruptArmorSemantics(
        uint Amount,
        bool RemoveOnInterrupt,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectThreatModificationSemantics(
        uint Mode,
        float RatioOrPercent,
        uint DataBits02,
        uint ThreatValue,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectThreatTransferSemantics(
        uint Mode,
        float RatioOrPercent,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectDispelSemantics(
        uint CountA,
        uint CountB,
        uint DataBits02,
        uint SpellClass,
        uint DataBits04,
        uint Priority);

    public sealed record SpellEffectModifyAbilityChargesSemantics(
        uint Spell4Id,
        uint Count,
        uint Mode,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectCooldownResetSemantics(
        uint DataBits00,
        uint Spell4Id,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectModifySpellCooldownSemantics(
        uint Mode,
        uint Spell4Id,
        uint Operation,
        float DataFloat03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectActivateSpellCooldownSemantics(
        uint DataBits00,
        uint Spell4Id,
        uint Mode,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectImmunitySemantics(
        SpellEffectType EffectType,
        uint EffectTypeRaw,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellImmunitySemantics(
        uint Mode,
        uint Spell4Id,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectScaleSemantics(
        float TargetScale,
        uint ApplyTimeMs,
        uint RestoreTimeMs,
        uint DataBits03,
        float DataFloat04,
        uint DataBits05);

    public sealed record SpellEffectFactionSetSemantics(
        uint FactionId,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectAddSpellSemantics(
        uint Spell4Id,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectKillSemantics(
        uint DataBits00,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectProcSemantics(
        uint TriggerEvent,
        uint TriggerSpell4Id,
        float Chance,
        uint TargetData,
        uint CooldownMsOrSentinel,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectDelayDeathSemantics(
        uint Mode,
        uint TriggerSpell4Id,
        uint TriggerDelayMs,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectClampVitalSemantics(
        uint Mode,
        uint VitalMode,
        float Ratio,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectShieldOverloadSemantics(
        uint DataBits00,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectGrantXpSemantics(
        uint Amount,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectPathXpModifySemantics(
        uint Amount,
        uint Mode,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectGrantLevelScaledXpSemantics(
        float PercentOfLevel,
        uint MaxLevel,
        uint Mode,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectModifyRestedXpSemantics(
        float LevelSpanMultiplier,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectGiveAugmentPowerToPlayerSemantics(
        uint Amount,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectQuestAdvanceObjectiveSemantics(
        uint ObjectiveId,
        uint DataBits01,
        uint Progress,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectAchievementAdvanceSemantics(
        uint AchievementId,
        uint Count,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectReputationModifySemantics(
        uint FactionId,
        float Amount,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectGiveItemToPlayerSemantics(
        uint Item2Id,
        uint Count,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectGiveLootTableToPlayerSemantics(
        uint LootGroupId,
        uint RollCount,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectGiveSchematicSemantics(
        uint TradeskillSchematic2Id,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectRewardPropertyModifierSemantics(
        uint RewardPropertyId,
        uint Data,
        float ValueFloat02,
        float ValueFloat03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectItemVisualSwapSemantics(
        uint VisualSlot,
        uint DisplayId,
        uint DataBits02,
        uint DataBits03,
        uint ColourSetId,
        uint DyeData);

    public sealed record SpellEffectDisembarkSemantics(
        uint Mode,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectActionBarSetSemantics(
        uint ActionBarShortcutSetId,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectDisguiseOutfitSemantics(
        uint OutfitInfoId,
        uint PrimaryItemDisplayId,
        uint SecondaryItemDisplayId,
        uint DataBits03,
        float DataFloat04,
        float DataFloat05);

    public sealed record SpellEffectMimicDisguiseSemantics(
        uint DataBits00,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectPetCastSpellSemantics(
        uint RequiredSummonSpell4Id,
        uint PetSpell4Id,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectForceFacingSemantics(
        bool UsesAngleOffset,
        float AngleDegrees,
        uint DataBits01,
        uint DataBits02,
        uint TurnDurationMs,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06,
        uint DataBits07,
        uint DataBits08,
        uint DataBits09);

    public sealed record SpellEffectForcedMoveSemantics(
        uint MovementType,
        float DataFloat01,
        float DataFloat02,
        uint DurationTime,
        float Gravity,
        uint Flags,
        float DataFloat06,
        float DataFloat07,
        float DataFloat08,
        uint DataBits09);

    public sealed record SpellEffectProxySemantics(uint Spell4Id);

    public sealed record SpellEffectTeleportSemantics(uint WorldLocation2Id);

    public sealed record SpellEffectHousingTeleportSemantics(
        uint Mode,
        uint DestinationMode,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectSupportStuckSemantics(
        float DurabilityMode,
        uint DataBits00,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectCCStateSemantics(CCState State, uint ApplyRulesFlags, uint AdditionalDataId);

    public sealed record SpellEffectCCStateBreakSemantics(
        uint StateMask,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectForceRemoveSemantics(
        uint RemoveType,
        uint Spell4Id,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05,
        uint DataBits06);

    public sealed record SpellEffectDespawnUnitSemantics(
        uint DataBits00,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04);

    public sealed record SpellEffectActivateSemantics(
        uint DataBits00,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectStateSemantics(
        uint DataBits00,
        uint DataBits01,
        uint DataBits02,
        uint DataBits03,
        uint DataBits04,
        uint DataBits05);

    public sealed record SpellEffectUnitPropertyModifierSemantics(
        Property Property,
        uint Priority,
        ModType? ModifierTypeHint,
        float PercentageValue,
        float FlatValue,
        float LevelScaleValue);

    public sealed record SpellEffectPersonalDmgHealModSemantics(
        uint ModifierType,
        uint Priority,
        float Multiplier,
        float DataFloat03,
        uint DataBits04,
        uint DataBits05);

    public sealed class SpellEffectInterpretation
    {
        public Spell4EffectsEntry Entry { get; }
        public SpellEffectTiming Timing { get; }
        public IReadOnlyList<SpellEffectDataBit> DataBits { get; }
        public IReadOnlyList<SpellEffectParameter> Parameters { get; }

        public SpellEffectDamageSemantics Damage { get; internal set; }
        public SpellEffectTransferenceSemantics Transference { get; internal set; }
        public SpellEffectAbsorptionSemantics Absorption { get; internal set; }
        public SpellEffectHealingAbsorptionSemantics HealingAbsorption { get; internal set; }
        public SpellEffectVitalModifierSemantics VitalModifier { get; internal set; }
        public SpellEffectUnitStateSetSemantics UnitStateSet { get; internal set; }
        public SpellEffectSetBusySemantics SetBusy { get; internal set; }
        public SpellEffectSapVitalSemantics SapVital { get; internal set; }
        public SpellEffectSummonCreatureSemantics SummonCreature { get; internal set; }
        public SpellEffectSummonPetSemantics SummonPet { get; internal set; }
        public SpellEffectSummonVehicleSemantics SummonVehicle { get; internal set; }
        public SpellEffectSummonTrapSemantics SummonTrap { get; internal set; }
        public SpellEffectNpcExecutionDelaySemantics NpcExecutionDelay { get; internal set; }
        public SpellEffectRavelSignalSemantics RavelSignal { get; internal set; }
        public SpellEffectSettlerCampfireSemantics SettlerCampfire { get; internal set; }
        public SpellEffectModifyInterruptArmorSemantics ModifyInterruptArmor { get; internal set; }
        public SpellEffectThreatModificationSemantics ThreatModification { get; internal set; }
        public SpellEffectThreatTransferSemantics ThreatTransfer { get; internal set; }
        public SpellEffectDispelSemantics Dispel { get; internal set; }
        public SpellEffectModifyAbilityChargesSemantics ModifyAbilityCharges { get; internal set; }
        public SpellEffectCooldownResetSemantics CooldownReset { get; internal set; }
        public SpellEffectModifySpellCooldownSemantics ModifySpellCooldown { get; internal set; }
        public SpellEffectActivateSpellCooldownSemantics ActivateSpellCooldown { get; internal set; }
        public SpellEffectImmunitySemantics SpellEffectImmunity { get; internal set; }
        public SpellImmunitySemantics SpellImmunity { get; internal set; }
        public SpellEffectScaleSemantics Scale { get; internal set; }
        public SpellEffectFactionSetSemantics FactionSet { get; internal set; }
        public SpellEffectAddSpellSemantics AddSpell { get; internal set; }
        public SpellEffectKillSemantics Kill { get; internal set; }
        public SpellEffectProcSemantics Proc { get; internal set; }
        public SpellEffectDelayDeathSemantics DelayDeath { get; internal set; }
        public SpellEffectClampVitalSemantics ClampVital { get; internal set; }
        public SpellEffectShieldOverloadSemantics ShieldOverload { get; internal set; }
        public SpellEffectGrantXpSemantics GrantXp { get; internal set; }
        public SpellEffectPathXpModifySemantics PathXpModify { get; internal set; }
        public SpellEffectGrantLevelScaledXpSemantics GrantLevelScaledXp { get; internal set; }
        public SpellEffectModifyRestedXpSemantics ModifyRestedXp { get; internal set; }
        public SpellEffectGiveAugmentPowerToPlayerSemantics GiveAugmentPowerToPlayer { get; internal set; }
        public SpellEffectQuestAdvanceObjectiveSemantics QuestAdvanceObjective { get; internal set; }
        public SpellEffectAchievementAdvanceSemantics AchievementAdvance { get; internal set; }
        public SpellEffectReputationModifySemantics ReputationModify { get; internal set; }
        public SpellEffectGiveItemToPlayerSemantics GiveItemToPlayer { get; internal set; }
        public SpellEffectGiveLootTableToPlayerSemantics GiveLootTableToPlayer { get; internal set; }
        public SpellEffectGiveSchematicSemantics GiveSchematic { get; internal set; }
        public SpellEffectRewardPropertyModifierSemantics RewardPropertyModifier { get; internal set; }
        public SpellEffectItemVisualSwapSemantics ItemVisualSwap { get; internal set; }
        public SpellEffectDisembarkSemantics Disembark { get; internal set; }
        public SpellEffectActionBarSetSemantics ActionBarSet { get; internal set; }
        public SpellEffectDisguiseOutfitSemantics DisguiseOutfit { get; internal set; }
        public SpellEffectMimicDisguiseSemantics MimicDisguise { get; internal set; }
        public SpellEffectPetCastSpellSemantics PetCastSpell { get; internal set; }
        public SpellEffectForceFacingSemantics ForceFacing { get; internal set; }
        public SpellEffectForcedMoveSemantics ForcedMove { get; internal set; }
        public SpellEffectProxySemantics Proxy { get; internal set; }
        public SpellEffectProxyRandomExclusiveSemantics ProxyRandomExclusive { get; internal set; }
        public SpellEffectTeleportSemantics Teleport { get; internal set; }
        public SpellEffectHousingTeleportSemantics HousingTeleport { get; internal set; }
        public SpellEffectSupportStuckSemantics SupportStuck { get; internal set; }
        public SpellEffectCCStateSemantics CCState { get; internal set; }
        public SpellEffectCCStateBreakSemantics CCStateBreak { get; internal set; }
        public SpellEffectForceRemoveSemantics ForceRemove { get; internal set; }
        public SpellEffectDespawnUnitSemantics DespawnUnit { get; internal set; }
        public SpellEffectActivateSemantics Activate { get; internal set; }
        public SpellEffectStateSemantics Stealth { get; internal set; }
        public SpellEffectStateSemantics RemoveStealth { get; internal set; }
        public SpellEffectStateSemantics AggroImmune { get; internal set; }
        public SpellEffectUnitPropertyModifierSemantics UnitPropertyModifier { get; internal set; }
        public SpellEffectPersonalDmgHealModSemantics PersonalDmgHealMod { get; internal set; }

        public bool HasKnownFamilySemantics => Damage != null
            || Transference != null
            || Absorption != null
            || HealingAbsorption != null
            || VitalModifier != null
            || UnitStateSet != null
            || SetBusy != null
            || SapVital != null
            || SummonCreature != null
            || SummonPet != null
            || SummonVehicle != null
            || SummonTrap != null
            || NpcExecutionDelay != null
            || RavelSignal != null
            || SettlerCampfire != null
            || ModifyInterruptArmor != null
            || ThreatModification != null
            || ThreatTransfer != null
            || Dispel != null
            || ModifyAbilityCharges != null
            || CooldownReset != null
            || ModifySpellCooldown != null
            || ActivateSpellCooldown != null
            || SpellEffectImmunity != null
            || SpellImmunity != null
            || Scale != null
            || FactionSet != null
            || AddSpell != null
            || Kill != null
            || Proc != null
            || DelayDeath != null
            || ClampVital != null
            || ShieldOverload != null
            || GrantXp != null
            || PathXpModify != null
            || GrantLevelScaledXp != null
            || ModifyRestedXp != null
            || GiveAugmentPowerToPlayer != null
            || QuestAdvanceObjective != null
            || AchievementAdvance != null
            || ReputationModify != null
            || GiveItemToPlayer != null
            || GiveLootTableToPlayer != null
            || GiveSchematic != null
            || RewardPropertyModifier != null
            || ItemVisualSwap != null
            || Disembark != null
            || ActionBarSet != null
            || DisguiseOutfit != null
            || MimicDisguise != null
            || PetCastSpell != null
            || ForceFacing != null
            || ForcedMove != null
            || Proxy != null
            || ProxyRandomExclusive != null
            || Teleport != null
            || HousingTeleport != null
            || SupportStuck != null
            || CCState != null
            || CCStateBreak != null
            || ForceRemove != null
            || DespawnUnit != null
            || Activate != null
            || Stealth != null
            || RemoveStealth != null
            || AggroImmune != null
            || UnitPropertyModifier != null
            || PersonalDmgHealMod != null;

        internal SpellEffectInterpretation(
            Spell4EffectsEntry entry,
            SpellEffectTiming timing,
            IReadOnlyList<SpellEffectDataBit> dataBits,
            IReadOnlyList<SpellEffectParameter> parameters)
        {
            Entry      = entry;
            Timing     = timing;
            DataBits   = dataBits;
            Parameters = parameters;
        }

        public string FormatDataBits()
        {
            return string.Join(", ", DataBits.Select(d => $"DataBits{d.Index:00}={d.RawValue} (0x{d.RawValue:X8}, float={d.FloatValue:R})"));
        }

        public string FormatParameters()
        {
            return string.Join(", ", Parameters.Select(p => $"Parameter{p.Index}={p.Type}:{p.Value:R}:{p.Role}"));
        }
    }

    public static class SpellEffectInterpreter
    {
        public static SpellEffectInterpretation Interpret(ISpellTargetEffectInfo info)
        {
            if (info is SpellTargetInfo.SpellTargetEffectInfo targetEffectInfo)
                return targetEffectInfo.Interpretation;

            return Interpret(info.Entry);
        }

        public static SpellEffectInterpretation Interpret(Spell4EffectsEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var interpretation = new SpellEffectInterpretation(
                entry,
                new SpellEffectTiming(entry.DelayTime, entry.TickTime, entry.DurationTime),
                CreateDataBits(entry),
                CreateParameters(entry));

            switch (entry.EffectType)
            {
                case SpellEffectType.VitalModifier:
                    interpretation.VitalModifier = new SpellEffectVitalModifierSemantics(
                        (Vital)entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        BitConverter.UInt32BitsToSingle(entry.DataBits05));
                    break;
                case SpellEffectType.UnitStateSet:
                    interpretation.UnitStateSet = new SpellEffectUnitStateSetSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.SetBusy:
                    interpretation.SetBusy = new SpellEffectSetBusySemantics(
                        entry.DataBits00 != 0u,
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.SapVital:
                    interpretation.SapVital = new SpellEffectSapVitalSemantics(
                        (Vital)entry.DataBits00,
                        BitConverter.UInt32BitsToSingle(entry.DataBits01),
                        BitConverter.UInt32BitsToSingle(entry.DataBits02),
                        entry.DataBits03,
                        entry.DataBits04,
                        BitConverter.UInt32BitsToSingle(entry.DataBits05),
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.SummonCreature:
                    interpretation.SummonCreature = new SpellEffectSummonCreatureSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.SummonPet:
                    interpretation.SummonPet = new SpellEffectSummonPetSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.SummonVehicle:
                    interpretation.SummonVehicle = new SpellEffectSummonVehicleSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.SummonTrap:
                    interpretation.SummonTrap = new SpellEffectSummonTrapSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        BitConverter.UInt32BitsToSingle(entry.DataBits04),
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07);
                    break;
                case SpellEffectType.NpcExecutionDelay:
                    interpretation.NpcExecutionDelay = new SpellEffectNpcExecutionDelaySemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.RavelSignal:
                    interpretation.RavelSignal = new SpellEffectRavelSignalSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.SettlerCampfire:
                    interpretation.SettlerCampfire = new SpellEffectSettlerCampfireSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.ModifyInterruptArmor:
                    interpretation.ModifyInterruptArmor = new SpellEffectModifyInterruptArmorSemantics(
                        entry.DataBits00,
                        entry.DataBits01 != 0u,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ThreatModification:
                    interpretation.ThreatModification = new SpellEffectThreatModificationSemantics(
                        entry.DataBits00,
                        BitConverter.UInt32BitsToSingle(entry.DataBits01),
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ThreatTransfer:
                    interpretation.ThreatTransfer = new SpellEffectThreatTransferSemantics(
                        entry.DataBits00,
                        BitConverter.UInt32BitsToSingle(entry.DataBits01),
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.SpellDispel:
                    interpretation.Dispel = new SpellEffectDispelSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ModifyAbilityCharges:
                    interpretation.ModifyAbilityCharges = new SpellEffectModifyAbilityChargesSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.CooldownReset:
                    interpretation.CooldownReset = new SpellEffectCooldownResetSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ModifySpellCooldown:
                    interpretation.ModifySpellCooldown = new SpellEffectModifySpellCooldownSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        BitConverter.UInt32BitsToSingle(entry.DataBits03),
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ActivateSpellCooldown:
                    interpretation.ActivateSpellCooldown = new SpellEffectActivateSpellCooldownSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.SpellEffectImmunity:
                    interpretation.SpellEffectImmunity = new SpellEffectImmunitySemantics(
                        (SpellEffectType)entry.DataBits00,
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.SpellImmunity:
                    interpretation.SpellImmunity = new SpellImmunitySemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.Scale:
                    interpretation.Scale = new SpellEffectScaleSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        BitConverter.UInt32BitsToSingle(entry.DataBits04),
                        entry.DataBits05);
                    break;
                case SpellEffectType.FactionSet:
                    interpretation.FactionSet = new SpellEffectFactionSetSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.AddSpell:
                    interpretation.AddSpell = new SpellEffectAddSpellSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.Kill:
                    interpretation.Kill = new SpellEffectKillSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.Proc:
                    interpretation.Proc = new SpellEffectProcSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        BitConverter.UInt32BitsToSingle(entry.DataBits02),
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.DelayDeath:
                    interpretation.DelayDeath = new SpellEffectDelayDeathSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.ClampVital:
                    interpretation.ClampVital = new SpellEffectClampVitalSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        BitConverter.UInt32BitsToSingle(entry.DataBits02),
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.ShieldOverload:
                    interpretation.ShieldOverload = new SpellEffectShieldOverloadSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.GrantXP:
                    interpretation.GrantXp = new SpellEffectGrantXpSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.PathXpModify:
                    interpretation.PathXpModify = new SpellEffectPathXpModifySemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.GrantLevelScaledXP:
                    interpretation.GrantLevelScaledXp = new SpellEffectGrantLevelScaledXpSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ModifyRestedXP:
                    interpretation.ModifyRestedXp = new SpellEffectModifyRestedXpSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.GiveAugmentPowerToPlayer:
                    interpretation.GiveAugmentPowerToPlayer = new SpellEffectGiveAugmentPowerToPlayerSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.QuestAdvanceObjective:
                    interpretation.QuestAdvanceObjective = new SpellEffectQuestAdvanceObjectiveSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.AchievementAdvance:
                    interpretation.AchievementAdvance = new SpellEffectAchievementAdvanceSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ReputationModify:
                    interpretation.ReputationModify = new SpellEffectReputationModifySemantics(
                        entry.DataBits00,
                        BitConverter.UInt32BitsToSingle(entry.DataBits01),
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.GiveItemToPlayer:
                    interpretation.GiveItemToPlayer = new SpellEffectGiveItemToPlayerSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.GiveLootTableToPlayer:
                    interpretation.GiveLootTableToPlayer = new SpellEffectGiveLootTableToPlayerSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.GiveSchematic:
                    interpretation.GiveSchematic = new SpellEffectGiveSchematicSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.RewardPropertyModifier:
                    interpretation.RewardPropertyModifier = new SpellEffectRewardPropertyModifierSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        BitConverter.UInt32BitsToSingle(entry.DataBits02),
                        BitConverter.UInt32BitsToSingle(entry.DataBits03),
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ItemVisualSwap:
                    interpretation.ItemVisualSwap = new SpellEffectItemVisualSwapSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.Disembark:
                    interpretation.Disembark = new SpellEffectDisembarkSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ActionBarSet:
                    interpretation.ActionBarSet = new SpellEffectActionBarSetSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.DisguiseOutfit:
                    interpretation.DisguiseOutfit = new SpellEffectDisguiseOutfitSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        BitConverter.UInt32BitsToSingle(entry.DataBits04),
                        BitConverter.UInt32BitsToSingle(entry.DataBits05));
                    break;
                case SpellEffectType.MimicDisguise:
                    interpretation.MimicDisguise = new SpellEffectMimicDisguiseSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.PetCastSpell:
                    interpretation.PetCastSpell = new SpellEffectPetCastSpellSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ForceFacing:
                    interpretation.ForceFacing = new SpellEffectForceFacingSemantics(
                        false,
                        0f,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.NpcForceFacing:
                    interpretation.ForceFacing = new SpellEffectForceFacingSemantics(
                        true,
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06,
                        entry.DataBits07,
                        entry.DataBits08,
                        entry.DataBits09);
                    break;
                case SpellEffectType.Transference:
                    float transferenceDamageMultiplier = BitConverter.UInt32BitsToSingle(entry.DataBits02);
                    interpretation.Transference = new SpellEffectTransferenceSemantics(
                        (Vital)entry.DataBits00,
                        (Vital)entry.DataBits01,
                        transferenceDamageMultiplier,
                        entry.DataBits03,
                        entry.DataBits04,
                        BitConverter.UInt32BitsToSingle(entry.DataBits05));
                    if ((!float.IsFinite(transferenceDamageMultiplier) || transferenceDamageMultiplier == 0f) && entry.DataBits03 > 0u)
                        transferenceDamageMultiplier = 1f;
                    interpretation.Damage = new SpellEffectDamageSemantics(
                        transferenceDamageMultiplier,
                        entry.DataBits03);
                    break;
                case SpellEffectType.Absorption:
                    interpretation.Damage = new SpellEffectDamageSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        BitConverter.UInt32BitsToSingle(entry.DataBits01));
                    interpretation.Absorption = new SpellEffectAbsorptionSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        BitConverter.UInt32BitsToSingle(entry.DataBits01),
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.HealingAbsorption:
                    interpretation.Damage = new SpellEffectDamageSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        BitConverter.UInt32BitsToSingle(entry.DataBits01));
                    interpretation.HealingAbsorption = new SpellEffectHealingAbsorptionSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        BitConverter.UInt32BitsToSingle(entry.DataBits01),
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.ForcedMove:
                    interpretation.ForcedMove = new SpellEffectForcedMoveSemantics(
                        entry.DataBits00,
                        BitConverter.UInt32BitsToSingle(entry.DataBits01),
                        BitConverter.UInt32BitsToSingle(entry.DataBits02),
                        entry.DataBits03,
                        BitConverter.UInt32BitsToSingle(entry.DataBits04),
                        entry.DataBits05,
                        BitConverter.UInt32BitsToSingle(entry.DataBits06),
                        BitConverter.UInt32BitsToSingle(entry.DataBits07),
                        BitConverter.UInt32BitsToSingle(entry.DataBits08),
                        entry.DataBits09);
                    break;
                case SpellEffectType.Damage:
                case SpellEffectType.Heal:
                case SpellEffectType.DistanceDependentDamage:
                case SpellEffectType.DistributedDamage:
                case SpellEffectType.HealShields:
                case SpellEffectType.DamageShields:
                    interpretation.Damage = new SpellEffectDamageSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        entry.DataBits01);
                    break;
                case SpellEffectType.Proxy:
                case SpellEffectType.ProxyLinearAE:
                case SpellEffectType.ProxyChannel:
                case SpellEffectType.ProxyChannelVariableTime:
                    interpretation.Proxy = new SpellEffectProxySemantics(entry.DataBits00);
                    break;
                case SpellEffectType.ProxyRandomExclusive:
                    interpretation.ProxyRandomExclusive = new SpellEffectProxyRandomExclusiveSemantics(
                    [
                        new(entry.DataBits00, entry.DataBits01),
                        new(entry.DataBits02, entry.DataBits03),
                        new(entry.DataBits04, entry.DataBits05),
                        new(entry.DataBits06, entry.DataBits07)
                    ]);
                    break;
                case SpellEffectType.Teleport:
                    interpretation.Teleport = new SpellEffectTeleportSemantics(entry.DataBits00);
                    break;
                case SpellEffectType.HousingTeleport:
                case SpellEffectType.HousingEscape:
                    interpretation.HousingTeleport = new SpellEffectHousingTeleportSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.SupportStuck:
                    interpretation.SupportStuck = new SpellEffectSupportStuckSemantics(
                        BitConverter.UInt32BitsToSingle(entry.DataBits00),
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.UnitPropertyModifier:
                    interpretation.UnitPropertyModifier = CreateUnitPropertyModifier(entry);
                    break;
                case SpellEffectType.PersonalDmgHealMod:
                    interpretation.PersonalDmgHealMod = new SpellEffectPersonalDmgHealModSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        BitConverter.UInt32BitsToSingle(entry.DataBits02),
                        BitConverter.UInt32BitsToSingle(entry.DataBits03),
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.CCStateSet:
                    interpretation.CCState = new SpellEffectCCStateSemantics((CCState)entry.DataBits00, entry.DataBits03, entry.DataBits07);
                    break;
                case SpellEffectType.CCStateBreak:
                    interpretation.CCStateBreak = new SpellEffectCCStateBreakSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.SpellForceRemove:
                case SpellEffectType.SpellForceRemoveChanneled:
                    interpretation.ForceRemove = new SpellEffectForceRemoveSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05,
                        entry.DataBits06);
                    break;
                case SpellEffectType.DespawnUnit:
                    interpretation.DespawnUnit = new SpellEffectDespawnUnitSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04);
                    break;
                case SpellEffectType.Activate:
                    interpretation.Activate = new SpellEffectActivateSemantics(
                        entry.DataBits00,
                        entry.DataBits01,
                        entry.DataBits02,
                        entry.DataBits03,
                        entry.DataBits04,
                        entry.DataBits05);
                    break;
                case SpellEffectType.Stealth:
                    interpretation.Stealth = CreateStateSemantics(entry);
                    break;
                case SpellEffectType.RemoveStealth:
                    interpretation.RemoveStealth = CreateStateSemantics(entry);
                    break;
                case SpellEffectType.AggroImmune:
                    interpretation.AggroImmune = CreateStateSemantics(entry);
                    break;
            }

            return interpretation;
        }

        private static IReadOnlyList<SpellEffectDataBit> CreateDataBits(Spell4EffectsEntry entry)
        {
            uint[] raw =
            [
                entry.DataBits00,
                entry.DataBits01,
                entry.DataBits02,
                entry.DataBits03,
                entry.DataBits04,
                entry.DataBits05,
                entry.DataBits06,
                entry.DataBits07,
                entry.DataBits08,
                entry.DataBits09
            ];

            return raw
                .Select((value, index) => new SpellEffectDataBit((byte)index, value, BitConverter.UInt32BitsToSingle(value)))
                .ToArray();
        }

        private static SpellEffectStateSemantics CreateStateSemantics(Spell4EffectsEntry entry)
        {
            return new SpellEffectStateSemantics(
                entry.DataBits00,
                entry.DataBits01,
                entry.DataBits02,
                entry.DataBits03,
                entry.DataBits04,
                entry.DataBits05);
        }

        private static IReadOnlyList<SpellEffectParameter> CreateParameters(Spell4EffectsEntry entry)
        {
            var parameters = new SpellEffectParameter[4];
            for (int i = 0; i < parameters.Length; i++)
            {
                SpellEffectParameterType type = entry.ParameterType?.Length > i
                    ? entry.ParameterType[i]
                    : SpellEffectParameterType.None;
                float value = entry.ParameterValue?.Length > i
                    ? entry.ParameterValue[i]
                    : 0f;

                parameters[i] = new SpellEffectParameter(i, type, value, GetParameterRole(type));
            }

            return parameters;
        }

        private static SpellEffectParameterRole GetParameterRole(SpellEffectParameterType type)
        {
            switch (type)
            {
                case SpellEffectParameterType.None:
                    return SpellEffectParameterRole.None;
                case SpellEffectParameterType.Brutality:
                case SpellEffectParameterType.Finesse:
                case SpellEffectParameterType.Tech:
                case SpellEffectParameterType.Moxie:
                case SpellEffectParameterType.Insight:
                case SpellEffectParameterType.Grit:
                    return SpellEffectParameterRole.CasterPrimaryStatCoefficient;
                case SpellEffectParameterType.AssaultPower:
                case SpellEffectParameterType.SupportPower:
                    return SpellEffectParameterRole.CasterPowerCoefficient;
                case SpellEffectParameterType.TargetMaxHealth:
                case SpellEffectParameterType.TargetShieldCapacity:
                case SpellEffectParameterType.TargetMaxShieldCapacity:
                case SpellEffectParameterType.TargetCurrentHealth:
                case SpellEffectParameterType.TargetShieldCapacity2:
                case SpellEffectParameterType.TargetMissingHealth:
                case SpellEffectParameterType.TargetMissingShields:
                    return SpellEffectParameterRole.TargetVitalCoefficient;
                case SpellEffectParameterType.CasterMaxHealth:
                case SpellEffectParameterType.CasterShieldCapacity:
                case SpellEffectParameterType.CasterMaxShieldCapacity:
                case SpellEffectParameterType.CasterCurrentHealth:
                case SpellEffectParameterType.CasterShieldCapacity2:
                case SpellEffectParameterType.CasterMissingHealth:
                case SpellEffectParameterType.CasterMissingShields:
                    return SpellEffectParameterRole.CasterVitalCoefficient;
                case SpellEffectParameterType.PerLevel:
                    return SpellEffectParameterRole.CasterLevelCoefficient;
                case SpellEffectParameterType.ItemBudget:
                    return SpellEffectParameterRole.ItemBudget;
                default:
                    return SpellEffectParameterRole.Unknown;
            }
        }

        private static SpellEffectUnitPropertyModifierSemantics CreateUnitPropertyModifier(Spell4EffectsEntry entry)
        {
            ModType? modifierTypeHint = Enum.IsDefined(typeof(ModType), (int)entry.DataBits01)
                ? (ModType)entry.DataBits01
                : null;

            return new SpellEffectUnitPropertyModifierSemantics(
                (Property)entry.DataBits00,
                entry.DataBits01,
                modifierTypeHint,
                BitConverter.UInt32BitsToSingle(entry.DataBits02),
                BitConverter.UInt32BitsToSingle(entry.DataBits03),
                BitConverter.UInt32BitsToSingle(entry.DataBits04));
        }
    }
}
