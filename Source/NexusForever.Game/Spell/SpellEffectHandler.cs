using System.Numerics;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Achievement;
using NexusForever.Game.Combat;
using NexusForever.Game.Combat.CrowdControl;
using NexusForever.Game.Entity;
using NexusForever.Game;
using NexusForever.Game.Housing;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Lock;
using NexusForever.Game.Map.Search;
using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Static.Spell.Effect;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Combat;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Spell
{
    public static class SpellHandler
    {
        private const uint VitalModifierSentinel = 2147483647u;
        private const uint VitalModifierMaxConservativeFlatAmount = 100000000u;
        private const uint ItemVisualSlotPacketMax = 0x7Fu;
        private const uint ItemVisualDisplayPacketMax = 0x7FFFu;
        private const uint ItemVisualColourSetPacketMax = 0x3FFFu;
        private const uint ActionBarShortcutSetPacketMax = 0x3FFFu;
        private const uint OutfitInfoPacketMax = 0x7FFFu;

        private static IDamageCalculator CreateDamageCalculator()
        {
            IFactory<IDamageCalculator> factory = LegacyServiceProvider.Provider.GetRequiredService<IFactory<IDamageCalculator>>();
            return factory.Resolve();
        }

        private static IEntityFactory GetEntityFactory()
        {
            return LegacyServiceProvider.Provider.GetService<IEntityFactory>();
        }

        [SpellEffectHandler(SpellEffectType.Damage)]
        public static void HandleEffectDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.CanAttack(spell.Caster))
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            damageCalculator.CalculateDamage(spell.Caster, target, spell, info);

            target.TakeDamage(spell.Caster, info.Damage);
        }

        [SpellEffectHandler(SpellEffectType.DistanceDependentDamage)]
        public static void HandleEffectDistanceDependentDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDamage(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.DistributedDamage)]
        public static void HandleEffectDistributedDamage(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDamage(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.Transference)]
        public static void HandleEffectTransference(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectTransferenceSemantics transference = SpellEffectInterpreter.Interpret(info).Transference;
            if (transference == null)
                return;

            if (!target.CanAttack(spell.Caster))
            {
                SpellEffectDiagnostics.TraceTransference(spell, target, transference, 0u, 0u, 0u, 0u, false, "target-cannot-attack-caster");
                return;
            }

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            damageCalculator.CalculateDamage(spell.Caster, target, spell, info);
            if (info.DropEffect || info.Damage == null)
                return;

            target.TakeDamage(spell.Caster, info.Damage);

            uint damageAmount = info.Damage.AdjustedDamage;
            uint rawHeal = CalculateTransferenceHealAmount(damageAmount, ResolveTransferenceRate(transference));
            uint appliedHeal = 0u;
            uint overheal = rawHeal;
            bool applied = false;
            string skippedReason = null;
            var healedUnits = new List<CombatLogTransference.CombatHealData>();

            if (rawHeal == 0u)
            {
                skippedReason = "zero-heal";
            }
            else if (spell.Caster.TryModifyVital(transference.HealedVital, rawHeal, out float appliedAmount, spell.Caster))
            {
                appliedHeal = appliedAmount > 0f ? (uint)MathF.Round(appliedAmount) : 0u;
                overheal = rawHeal - Math.Min(rawHeal, appliedHeal);
                applied = appliedHeal > 0u;
                skippedReason = applied ? null : "fully-overheal";
                healedUnits.Add(new CombatLogTransference.CombatHealData
                {
                    HealedUnitId = spell.Caster.Guid,
                    HealAmount   = appliedHeal,
                    Vital        = transference.HealedVital,
                    Overheal     = overheal,
                    Absorption   = 0u
                });
            }
            else
            {
                skippedReason = "evidence-gap-heal-vital";
            }

            info.AddCombatLog(new CombatLogTransference
            {
                DamageAmount      = damageAmount,
                DamageType        = info.Entry.DamageType,
                Shield            = info.Damage.ShieldAbsorbAmount,
                Absorption        = info.Damage.AbsorbedAmount,
                Overkill          = info.Damage.OverkillAmount,
                GlanceAmount      = 0u,
                BTargetVulnerable = false,
                HealedUnits       = healedUnits
            });

            SpellEffectDiagnostics.TraceTransference(spell, target, transference, damageAmount, rawHeal, appliedHeal, overheal, applied, skippedReason);
        }

        [SpellEffectHandler(SpellEffectType.Heal)]
        public static void HandleEffectHeal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.IsAlive)
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            damageCalculator.CalculateHealing(spell.Caster, target, spell, info);

            target.ModifyHealth(info.Damage.AdjustedDamage, DamageType.Heal, spell.Caster);
        }

        [SpellEffectHandler(SpellEffectType.HealShields)]
        public static void HandleEffectHealShields(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.IsAlive)
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            damageCalculator.CalculateShieldHealing(spell.Caster, target, spell, info);

            target.Shield += info.Damage.AdjustedDamage;
        }

        [SpellEffectHandler(SpellEffectType.DamageShields)]
        public static void HandleEffectDamageShields(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (!target.CanAttack(spell.Caster))
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            damageCalculator.CalculateShieldDamage(spell.Caster, target, spell, info);

            target.Shield = target.Shield > info.Damage.ShieldAbsorbAmount
                ? target.Shield - info.Damage.ShieldAbsorbAmount
                : 0u;
        }

        [SpellEffectHandler(SpellEffectType.Absorption)]
        public static void HandleEffectAbsorption(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectAbsorptionSemantics absorption = SpellEffectInterpreter.Interpret(info).Absorption;
            if (absorption == null || !target.IsAlive)
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            uint amount = damageCalculator.CalculateAbsorption(spell.Caster, target, spell, info);
            if (amount == 0u)
                return;

            target.AddAbsorption(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, amount);
            SpellEffectDiagnostics.TraceAbsorption(spell, target, absorption, amount, false);
        }

        [SpellEffectHandler(SpellEffectType.HealingAbsorption)]
        public static void HandleEffectHealingAbsorption(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectHealingAbsorptionSemantics absorption = SpellEffectInterpreter.Interpret(info).HealingAbsorption;
            if (absorption == null || !target.IsAlive)
                return;

            IDamageCalculator damageCalculator = CreateDamageCalculator();
            uint amount = damageCalculator.CalculateHealingAbsorption(spell.Caster, target, spell, info);
            if (amount == 0u)
                return;

            target.AddHealingAbsorption(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, amount);
            SpellEffectDiagnostics.TraceHealingAbsorption(spell, target, absorption, amount, false);
        }

        [SpellEffectHandler(SpellEffectType.VitalModifier)]
        public static void HandleEffectVitalModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation interpretation = SpellEffectInterpreter.Interpret(info);
            SpellEffectVitalModifierSemantics vitalModifier = interpretation.VitalModifier;
            if (vitalModifier == null)
                return;

            if (!target.IsAlive)
            {
                SpellEffectDiagnostics.TraceVitalModifier(spell, target, vitalModifier, "none", 0f, 0f, false, "target-not-alive");
                return;
            }

            if (!TryResolveVitalModifierAmount(target, interpretation, vitalModifier, out float amount, out string mode, out string skippedReason))
            {
                SpellEffectDiagnostics.TraceVitalModifier(spell, target, vitalModifier, mode, amount, 0f, false, skippedReason);
                return;
            }

            bool applied = target.TryModifyVital(vitalModifier.Vital, amount, out float appliedAmount, spell.Caster);
            SpellEffectDiagnostics.TraceVitalModifier(spell, target, vitalModifier, mode, amount, appliedAmount, applied, applied ? string.Empty : "evidence-gap-vital");
            if (!applied)
                return;

            info.AddCombatLog(new CombatLogVitalModifier
            {
                Amount        = appliedAmount,
                VitalModified = vitalModifier.Vital,
                BShowCombatLog = true,
                CastData = new CombatLogCastData
                {
                    CasterId     = spell.Caster.Guid,
                    TargetId     = target.Guid,
                    SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                    CombatResult = CombatResult.Hit
                }
            });
        }

        private static bool TryResolveVitalModifierAmount(
            IUnitEntity target,
            SpellEffectInterpretation interpretation,
            SpellEffectVitalModifierSemantics vitalModifier,
            out float amount,
            out string mode,
            out string skippedReason)
        {
            amount        = 0f;
            mode          = "none";
            skippedReason = string.Empty;

            if (interpretation.Parameters.Any(p => p.Type != SpellEffectParameterType.None))
            {
                skippedReason = "parameter-driven";
                return false;
            }

            if (TryResolveFlatVitalModifierAmount(vitalModifier, out amount))
            {
                mode = "flat";
                return true;
            }

            if (TryResolvePercentVitalModifierAmount(target, vitalModifier, out amount, out skippedReason))
            {
                mode = "percent-max";
                return true;
            }

            if (string.IsNullOrEmpty(skippedReason))
                skippedReason = "ambiguous-payload";

            return false;
        }

        private static bool TryResolveFlatVitalModifierAmount(SpellEffectVitalModifierSemantics vitalModifier, out float amount)
        {
            amount = 0f;
            if (vitalModifier.DataBits01 == 0u ||
                vitalModifier.DataBits01 != vitalModifier.DataBits02 ||
                vitalModifier.DataBits01 == VitalModifierSentinel ||
                vitalModifier.DataBits01 > VitalModifierMaxConservativeFlatAmount)
            {
                return false;
            }

            amount = vitalModifier.DataBits01;
            return true;
        }

        private static bool TryResolvePercentVitalModifierAmount(IUnitEntity target, SpellEffectVitalModifierSemantics vitalModifier, out float amount, out string skippedReason)
        {
            amount        = 0f;
            skippedReason = string.Empty;

            if (vitalModifier.DataBits01 != 0u ||
                vitalModifier.DataBits02 != 0u ||
                vitalModifier.DataBits03 != 0u ||
                vitalModifier.DataBits04 != 0u)
            {
                return false;
            }

            if (!float.IsFinite(vitalModifier.DataFloat05) || vitalModifier.DataFloat05 <= 0f)
            {
                skippedReason = "invalid-percent";
                return false;
            }

            if (!target.TryGetVitalMax(vitalModifier.Vital, out float maxValue))
            {
                skippedReason = "evidence-gap-vital-max";
                return false;
            }

            float fraction = vitalModifier.DataFloat05 <= 1f
                ? vitalModifier.DataFloat05
                : vitalModifier.DataFloat05 <= 100f
                    ? vitalModifier.DataFloat05 / 100f
                    : 0f;
            if (fraction <= 0f)
            {
                skippedReason = "percent-out-of-range";
                return false;
            }

            amount = maxValue * fraction;
            return amount > 0f;
        }

        [SpellEffectHandler(SpellEffectType.SapVital)]
        public static void HandleEffectSapVital(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation interpretation = SpellEffectInterpreter.Interpret(info);
            SpellEffectSapVitalSemantics sapVital = interpretation.SapVital;
            if (sapVital == null)
                return;

            if (!target.IsAlive)
            {
                SpellEffectDiagnostics.TraceSapVital(spell, target, sapVital, "none", "none", 0f, 0f, false, "target-not-alive");
                return;
            }

            if (interpretation.Parameters.Any(p => p.Type != SpellEffectParameterType.None))
            {
                SpellEffectDiagnostics.TraceSapVital(spell, target, sapVital, "none", "none", 0f, 0f, false, "parameter-driven");
                return;
            }

            if (!TryResolveSapVitalAmount(target, sapVital, out float amount, out string mode, out string amountSource, out string skippedReason))
            {
                SpellEffectDiagnostics.TraceSapVital(spell, target, sapVital, mode, amountSource, amount, 0f, false, skippedReason);
                return;
            }

            bool applied = target.TryModifyVital(sapVital.Vital, amount, out float appliedAmount, spell.Caster, info.Entry.DamageType);
            SpellEffectDiagnostics.TraceSapVital(spell, target, sapVital, mode, amountSource, amount, appliedAmount, applied, applied ? string.Empty : "evidence-gap-vital");
            if (!applied)
                return;

            info.AddCombatLog(new CombatLogVitalModifier
            {
                Amount         = appliedAmount,
                VitalModified  = sapVital.Vital,
                BShowCombatLog = true,
                CastData       = new CombatLogCastData
                {
                    CasterId     = spell.Caster.Guid,
                    TargetId     = target.Guid,
                    SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                    CombatResult = CombatResult.Hit
                }
            });
        }

        private static bool TryResolveSapVitalAmount(
            IUnitEntity target,
            SpellEffectSapVitalSemantics sapVital,
            out float amount,
            out string mode,
            out string amountSource,
            out string skippedReason)
        {
            amount        = 0f;
            mode          = "none";
            amountSource  = "none";
            skippedReason = string.Empty;

            if (sapVital.DataBits04 != 0u ||
                sapVital.DataFloat05 != 0f ||
                sapVital.DataBits06 != 0u ||
                sapVital.DataBits07 != 0u ||
                sapVital.DataBits08 != 0u ||
                sapVital.DataBits09 != 0u)
            {
                skippedReason = "secondary-payload";
                return false;
            }

            if (sapVital.Mode > 2u)
            {
                skippedReason = "unknown-mode";
                return false;
            }

            if (!TrySelectSapVitalScalar(sapVital, out float scalar, out amountSource, out skippedReason))
                return false;

            float magnitude = MathF.Abs(scalar);
            if (amountSource == "dataFloat01" && magnitude > 1f)
            {
                skippedReason = "ambiguous-datafloat01";
                return false;
            }

            if (amountSource == "dataFloat02" && magnitude > 100f)
            {
                skippedReason = "percent-out-of-range";
                return false;
            }

            if (!target.TryGetVitalMax(sapVital.Vital, out float maxValue))
            {
                skippedReason = "evidence-gap-vital-max";
                return false;
            }

            float fraction = magnitude <= 1f
                ? magnitude
                : magnitude / 100f;
            if (!float.IsFinite(fraction) || fraction <= 0f)
            {
                skippedReason = "invalid-fraction";
                return false;
            }

            float resolvedAmount = maxValue * fraction;
            if (!float.IsFinite(resolvedAmount) || resolvedAmount <= 0f)
            {
                skippedReason = "invalid-amount";
                return false;
            }

            if (sapVital.Mode == 1u && scalar > 0f)
            {
                amount = resolvedAmount;
                mode   = "restore-percent-max";
            }
            else
            {
                amount = -resolvedAmount;
                mode   = sapVital.Mode == 1u ? "signed-drain-percent-max" : "drain-percent-max";
            }

            return true;
        }

        private static bool TrySelectSapVitalScalar(SpellEffectSapVitalSemantics sapVital, out float scalar, out string amountSource, out string skippedReason)
        {
            scalar        = 0f;
            amountSource  = "none";
            skippedReason = string.Empty;

            if (float.IsFinite(sapVital.DataFloat02) && sapVital.DataFloat02 != 0f)
            {
                scalar       = sapVital.DataFloat02;
                amountSource = "dataFloat02";
                return true;
            }

            if (float.IsFinite(sapVital.DataFloat01) && sapVital.DataFloat01 != 0f)
            {
                scalar       = sapVital.DataFloat01;
                amountSource = "dataFloat01";
                return true;
            }

            skippedReason = "zero-amount";
            return false;
        }

        [SpellEffectHandler(SpellEffectType.SummonCreature)]
        public static void HandleEffectSummonCreature(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectSummonCreatureSemantics summonCreature = SpellEffectInterpreter.Interpret(info).SummonCreature;
            if (summonCreature == null)
                return;

            Vector3 position = ResolveSummonCreaturePosition(spell, target, summonCreature);
            if (summonCreature.CreatureId == 0u)
            {
                SpellEffectDiagnostics.TraceSummonCreature(spell, target, summonCreature, position, false, 0u, "missing-creature-id");
                return;
            }

            if (GameTableManager.Instance.Creature2.GetEntry(summonCreature.CreatureId) == null)
            {
                SpellEffectDiagnostics.TraceSummonCreature(spell, target, summonCreature, position, false, 0u, "unknown-creature-id");
                return;
            }

            var map = target.Map ?? spell.Caster.Map;
            if (map == null)
            {
                SpellEffectDiagnostics.TraceSummonCreature(spell, target, summonCreature, position, false, 0u, "target-not-in-world");
                return;
            }

            IEntityFactory factory = GetEntityFactory();
            if (factory == null)
            {
                SpellEffectDiagnostics.TraceSummonCreature(spell, target, summonCreature, position, false, 0u, "missing-entity-factory");
                return;
            }

            INonPlayerEntity summoned = factory.CreateEntity<INonPlayerEntity>();
            summoned.Initialise(summonCreature.CreatureId);
            summoned.Rotation = target.Rotation;

            var mapPosition = new MapPosition
            {
                Position = position
            };

            if (!map.CanEnter(summoned, mapPosition))
            {
                SpellEffectDiagnostics.TraceSummonCreature(spell, target, summonCreature, position, false, summoned.Guid, "map-rejected-position");
                return;
            }

            map.EnqueueAdd(summoned, mapPosition);
            info.AddCreatedEntity(summoned);
            SpellEffectDiagnostics.TraceSummonCreature(spell, target, summonCreature, position, true, summoned.Guid, string.Empty);
        }

        [SpellEffectHandler(SpellEffectType.SummonVehicle)]
        public static void HandleEffectSummonVehicle(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectSummonVehicleSemantics summonVehicle = SpellEffectInterpreter.Interpret(info).SummonVehicle;
            if (summonVehicle == null)
                return;

            var map = target.Map ?? spell.Caster.Map;
            Vector3 position = target.Map != null ? target.Position : spell.Caster.Position;

            if (summonVehicle.CreatureId == 0u)
            {
                SpellEffectDiagnostics.TraceSummonVehicle(spell, target, summonVehicle, position, false, false, 0u, "missing-creature-id");
                return;
            }

            Creature2Entry creatureEntry = GameTableManager.Instance.Creature2.GetEntry(summonVehicle.CreatureId);
            if (creatureEntry == null)
            {
                SpellEffectDiagnostics.TraceSummonVehicle(spell, target, summonVehicle, position, false, false, 0u, "unknown-creature-id");
                return;
            }

            uint unitVehicleId = summonVehicle.UnitVehicleId != 0u
                ? summonVehicle.UnitVehicleId
                : creatureEntry.UnitVehicleId;
            if (unitVehicleId == 0u || GameTableManager.Instance.UnitVehicle.GetEntry(unitVehicleId) == null)
            {
                SpellEffectDiagnostics.TraceSummonVehicle(spell, target, summonVehicle, position, false, false, 0u, "unknown-unit-vehicle");
                return;
            }

            if (map == null)
            {
                SpellEffectDiagnostics.TraceSummonVehicle(spell, target, summonVehicle, position, false, false, 0u, "target-not-in-world");
                return;
            }

            bool shouldBoard = summonVehicle.BoardMode != 0u;
            IPlayer player = target as IPlayer ?? spell.Caster as IPlayer;
            if (shouldBoard)
            {
                if (player == null)
                {
                    SpellEffectDiagnostics.TraceSummonVehicle(spell, target, summonVehicle, position, false, false, 0u, "no-player-to-board");
                    return;
                }

                if (!player.CanMount())
                {
                    SpellEffectDiagnostics.TraceSummonVehicle(spell, target, summonVehicle, position, false, false, 0u, "player-cannot-board");
                    return;
                }
            }

            IEntityFactory factory = GetEntityFactory();
            if (factory == null)
            {
                SpellEffectDiagnostics.TraceSummonVehicle(spell, target, summonVehicle, position, false, false, 0u, "missing-entity-factory");
                return;
            }

            IVehicleEntity vehicle = factory.CreateEntity<IVehicleEntity>();
            vehicle.Initialise(summonVehicle.CreatureId, unitVehicleId, spell.Parameters.SpellInfo.Entry.Id);
            vehicle.Rotation = target.Rotation;

            if (shouldBoard)
                vehicle.EnqueuePassengerAdd(player, VehicleSeatType.Pilot, 0);

            var mapPosition = new MapPosition
            {
                Position = position
            };

            if (!map.CanEnter(vehicle, mapPosition))
            {
                SpellEffectDiagnostics.TraceSummonVehicle(spell, target, summonVehicle, position, false, shouldBoard, vehicle.Guid, "map-rejected-position");
                return;
            }

            map.EnqueueAdd(vehicle, mapPosition);
            info.AddCreatedEntity(vehicle);
            SpellEffectDiagnostics.TraceSummonVehicle(spell, target, summonVehicle, position, true, shouldBoard, vehicle.Guid, null);
        }

        [SpellEffectHandler(SpellEffectType.SummonTrap)]
        public static void HandleEffectSummonTrap(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectSummonTrapSemantics summonTrap = SpellEffectInterpreter.Interpret(info).SummonTrap;
            if (summonTrap == null)
                return;

            var map = target.Map ?? spell.Caster.Map;
            Vector3 position = target.Map != null ? target.Position : spell.Caster.Position;

            if (summonTrap.CreatureId == 0u)
            {
                SpellEffectDiagnostics.TraceSummonTrap(spell, target, summonTrap, position, false, 0u, "missing-creature-id");
                return;
            }

            if (GameTableManager.Instance.Creature2.GetEntry(summonTrap.CreatureId) == null)
            {
                SpellEffectDiagnostics.TraceSummonTrap(spell, target, summonTrap, position, false, 0u, "unknown-creature-id");
                return;
            }

            if (summonTrap.TriggerSpell4Id != 0u && GameTableManager.Instance.Spell4.GetEntry(summonTrap.TriggerSpell4Id) == null)
            {
                SpellEffectDiagnostics.TraceSummonTrap(spell, target, summonTrap, position, false, 0u, "unknown-trigger-spell4");
                return;
            }

            if (map == null)
            {
                SpellEffectDiagnostics.TraceSummonTrap(spell, target, summonTrap, position, false, 0u, "target-not-in-world");
                return;
            }

            IEntityFactory factory = GetEntityFactory();
            if (factory == null)
            {
                SpellEffectDiagnostics.TraceSummonTrap(spell, target, summonTrap, position, false, 0u, "missing-entity-factory");
                return;
            }

            INonPlayerEntity trap = factory.CreateEntity<INonPlayerEntity>();
            trap.Initialise(summonTrap.CreatureId);
            trap.Rotation = target.Rotation;

            var mapPosition = new MapPosition
            {
                Position = position
            };

            if (!map.CanEnter(trap, mapPosition))
            {
                SpellEffectDiagnostics.TraceSummonTrap(spell, target, summonTrap, position, false, trap.Guid, "map-rejected-position");
                return;
            }

            map.EnqueueAdd(trap, mapPosition);
            info.AddCreatedEntity(trap);
            SpellEffectDiagnostics.TraceSummonTrap(spell, target, summonTrap, position, true, trap.Guid, null);
        }

        private static Vector3 ResolveSummonCreaturePosition(ISpell spell, IUnitEntity target, SpellEffectSummonCreatureSemantics summonCreature)
        {
            Vector3 anchor = target.Map != null
                ? target.Position
                : spell.Caster.Position;

            float radius = ResolveSummonCreatureRadius(summonCreature);
            if (radius <= 0f)
                return anchor;

            float angle = ResolveSummonCreatureAngle(spell, summonCreature);
            return anchor + new Vector3(MathF.Cos(angle) * radius, 0f, MathF.Sin(angle) * radius);
        }

        private static float ResolveSummonCreatureRadius(SpellEffectSummonCreatureSemantics summonCreature)
        {
            bool hasMin = IsUsableSummonCreatureDistance(summonCreature.DataBits03);
            bool hasMax = IsUsableSummonCreatureDistance(summonCreature.DataBits04);

            if (hasMin && hasMax)
                return (summonCreature.DataBits03 + summonCreature.DataBits04) / 2f;

            if (hasMin)
                return summonCreature.DataBits03;

            return hasMax ? summonCreature.DataBits04 : 0f;
        }

        private static bool IsUsableSummonCreatureDistance(uint value)
        {
            return value > 0u && value <= 100u;
        }

        private static float ResolveSummonCreatureAngle(ISpell spell, SpellEffectSummonCreatureSemantics summonCreature)
        {
            float casterForwardAngle = -spell.Caster.Rotation.X + MathF.PI / 2f;
            if (summonCreature.DataBits05 > 360u)
                return casterForwardAngle;

            return casterForwardAngle + summonCreature.DataBits05 * MathF.PI / 180f;
        }

        [SpellEffectHandler(SpellEffectType.NpcExecutionDelay)]
        public static void HandleEffectNpcExecutionDelay(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectNpcExecutionDelaySemantics executionDelay = SpellEffectInterpreter.Interpret(info).NpcExecutionDelay;
            if (executionDelay == null)
                return;

            SpellEffectDiagnostics.TraceNpcExecutionDelay(spell, target, info, executionDelay);
        }

        [SpellEffectHandler(SpellEffectType.RavelSignal)]
        public static void HandleEffectRavelSignal(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectRavelSignalCore(spell, target, info);
        }

        public static void HandleEffectRavelSignalWorld(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectRavelSignalCore(spell, target, info);
        }

        private static void HandleEffectRavelSignalCore(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectRavelSignalSemantics ravelSignal = SpellEffectInterpreter.Interpret(info).RavelSignal;
            if (ravelSignal == null)
                return;

            SpellEffectDiagnostics.TraceRavelSignal(spell, target, info, ravelSignal, "receiver-not-implemented");
        }

        [SpellEffectHandler(SpellEffectType.ModifyInterruptArmor)]
        public static void HandleEffectModifyInterruptArmor(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectModifyInterruptArmorSemantics interruptArmor = SpellEffectInterpreter.Interpret(info).ModifyInterruptArmor;
            if (interruptArmor == null)
                return;

            target.TryModifyVital(Vital.InterruptArmor, interruptArmor.Amount, out float appliedAmountFloat, spell.Caster);
            uint appliedAmount = appliedAmountFloat > 0f
                ? (uint)MathF.Round(appliedAmountFloat)
                : 0u;

            SpellEffectDiagnostics.TraceModifyInterruptArmor(spell, target, interruptArmor, appliedAmount, false);
            if (appliedAmount == 0u)
                return;

            info.AddCombatLog(new CombatLogModifyInterruptArmor
            {
                Amount = appliedAmount,
                CastData = new CombatLogCastData
                {
                    CasterId     = spell.Caster.Guid,
                    TargetId     = target.Guid,
                    SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                    CombatResult = CombatResult.Hit
                }
            });
        }

        [SpellEffectHandler(SpellEffectType.ThreatModification)]
        public static void HandleEffectThreatModification(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectThreatModificationSemantics threat = SpellEffectInterpreter.Interpret(info).ThreatModification;
            if (threat == null)
                return;

            switch (threat.Mode)
            {
                case 0u:
                    ApplyThreatDelta(spell, target, threat, ResolveThreatAmount(threat), "add");
                    break;
                case 1u:
                    ReduceThreatFromVisibleOwners(spell, target, threat, ResolveThreatReductionFraction(threat));
                    break;
                case 2u:
                case 6u:
                    ClearThreatFromVisibleOwners(spell, target, threat);
                    break;
                case 4u:
                    SetThreat(spell, target, threat, ResolveThreatAmount(threat), "set");
                    break;
                case 5u:
                    SetThreat(spell, target, threat, ResolveThreatAmount(threat), "fixate");
                    break;
                default:
                    SpellEffectDiagnostics.TraceThreatModification(spell, target, threat, "none", 0u, 0u, 0u, 0u, "unknown-mode");
                    break;
            }
        }

        [SpellEffectHandler(SpellEffectType.ThreatTransfer)]
        public static void HandleEffectThreatTransfer(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectThreatTransferSemantics threatTransfer = SpellEffectInterpreter.Interpret(info).ThreatTransfer;
            if (threatTransfer == null)
                return;

            SpellEffectDiagnostics.TraceThreatTransfer(spell, target, threatTransfer);
        }

        private static void ApplyThreatDelta(ISpell spell, IUnitEntity target, SpellEffectThreatModificationSemantics threat, uint amount, string action)
        {
            if (amount == 0u)
            {
                SpellEffectDiagnostics.TraceThreatModification(spell, target, threat, action, 0u, 0u, 0u, 0u, "zero-amount");
                return;
            }

            (IUnitEntity owner, IUnitEntity hated) = ResolveThreatOwnerAndHated(spell, target);
            uint beforeThreat = owner.ThreatManager.GetHostile(hated.Guid)?.Threat ?? 0u;
            owner.ThreatManager.UpdateThreat(hated, (int)Math.Min(int.MaxValue, amount));
            uint afterThreat = owner.ThreatManager.GetHostile(hated.Guid)?.Threat ?? 0u;
            SpellEffectDiagnostics.TraceThreatModification(spell, target, threat, action, owner.Guid, hated.Guid, beforeThreat, afterThreat, string.Empty);
        }

        private static void SetThreat(ISpell spell, IUnitEntity target, SpellEffectThreatModificationSemantics threat, uint amount, string action)
        {
            (IUnitEntity owner, IUnitEntity hated) = ResolveThreatOwnerAndHated(spell, target);
            uint beforeThreat = owner.ThreatManager.GetHostile(hated.Guid)?.Threat ?? 0u;
            owner.ThreatManager.SetThreat(hated, amount);
            uint afterThreat = owner.ThreatManager.GetHostile(hated.Guid)?.Threat ?? 0u;
            SpellEffectDiagnostics.TraceThreatModification(spell, target, threat, action, owner.Guid, hated.Guid, beforeThreat, afterThreat, string.Empty);
        }

        private static (IUnitEntity Owner, IUnitEntity Hated) ResolveThreatOwnerAndHated(ISpell spell, IUnitEntity target)
        {
            return spell.Caster is IPlayer
                ? (target, spell.Caster)
                : (spell.Caster, target);
        }

        private static uint ResolveThreatAmount(SpellEffectThreatModificationSemantics threat)
        {
            if (threat.ThreatValue != 0u)
                return threat.ThreatValue;

            if (threat.DataBits02 != 0u)
                return threat.DataBits02;

            if (float.IsFinite(threat.RatioOrPercent) && threat.RatioOrPercent > 1f)
                return (uint)MathF.Ceiling(threat.RatioOrPercent);

            return 0u;
        }

        private static float ResolveThreatReductionFraction(SpellEffectThreatModificationSemantics threat)
        {
            if (!float.IsFinite(threat.RatioOrPercent) || threat.RatioOrPercent <= 0f)
                return 1f;

            return threat.RatioOrPercent > 1f
                ? Math.Clamp(threat.RatioOrPercent / 100f, 0f, 1f)
                : Math.Clamp(threat.RatioOrPercent, 0f, 1f);
        }

        private static void ClearThreatFromVisibleOwners(ISpell spell, IUnitEntity target, SpellEffectThreatModificationSemantics threat)
        {
            int changedCount = 0;
            foreach (IUnitEntity owner in GetVisibleThreatOwners(target))
            {
                uint beforeThreat = owner.ThreatManager.GetHostile(target.Guid)?.Threat ?? 0u;
                if (beforeThreat == 0u)
                    continue;

                owner.ThreatManager.RemoveHostile(target.Guid);
                changedCount++;
                SpellEffectDiagnostics.TraceThreatModification(spell, target, threat, "clear-visible-owner", owner.Guid, target.Guid, beforeThreat, 0u, string.Empty);
            }

            uint selfBeforeThreat = target.ThreatManager.GetHostile(spell.Caster.Guid)?.Threat ?? 0u;
            if (selfBeforeThreat != 0u)
            {
                target.ThreatManager.RemoveHostile(spell.Caster.Guid);
                changedCount++;
                SpellEffectDiagnostics.TraceThreatModification(spell, target, threat, "clear-target-owner", target.Guid, spell.Caster.Guid, selfBeforeThreat, 0u, string.Empty);
            }

            if (changedCount == 0)
                SpellEffectDiagnostics.TraceThreatModification(spell, target, threat, "clear", 0u, target.Guid, 0u, 0u, "no-threat");
        }

        private static void ReduceThreatFromVisibleOwners(ISpell spell, IUnitEntity target, SpellEffectThreatModificationSemantics threat, float fraction)
        {
            int changedCount = 0;
            foreach (IUnitEntity owner in GetVisibleThreatOwners(target))
            {
                uint beforeThreat = owner.ThreatManager.GetHostile(target.Guid)?.Threat ?? 0u;
                if (beforeThreat == 0u)
                    continue;

                uint afterThreat = (uint)MathF.Floor(beforeThreat * (1f - fraction));
                owner.ThreatManager.SetThreat(target, afterThreat);
                changedCount++;
                SpellEffectDiagnostics.TraceThreatModification(spell, target, threat, "reduce-visible-owner", owner.Guid, target.Guid, beforeThreat, afterThreat, string.Empty);
            }

            if (changedCount == 0)
                SpellEffectDiagnostics.TraceThreatModification(spell, target, threat, "reduce", 0u, target.Guid, 0u, 0u, "no-threat");
        }

        private static IEnumerable<IUnitEntity> GetVisibleThreatOwners(IUnitEntity target)
        {
            if (target.Map == null)
                return Enumerable.Empty<IUnitEntity>();

            return target.Map.Search(target.Position, target.Map.VisionRange, new SearchCheckRange<IUnitEntity>(target.Position, target.Map.VisionRange, target));
        }

        [SpellEffectHandler(SpellEffectType.ForceFacing)]
        public static void HandleEffectForceFacing(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectForceFacing(spell, target, info, false);
        }

        [SpellEffectHandler(SpellEffectType.NpcForceFacing)]
        public static void HandleEffectNpcForceFacing(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectForceFacing(spell, target, info, true);
        }

        private static void HandleEffectForceFacing(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info, bool npcVariant)
        {
            SpellEffectForceFacingSemantics forceFacing = SpellEffectInterpreter.Interpret(info).ForceFacing;
            if (forceFacing == null)
                return;

            Vector3 previousRotation = target.Rotation;
            if (!TryResolveForceFacingRotation(spell, target, forceFacing, out Vector3 desiredRotation, out string skippedReason))
            {
                SpellEffectDiagnostics.TraceForceFacing(spell, target, forceFacing, npcVariant, previousRotation.X, previousRotation.X, false, skippedReason);
                return;
            }

            if (forceFacing.TurnDurationMs > 0u && target.MovementManager.ServerControl)
            {
                target.MovementManager.SetRotationKeys(
                    new List<uint> { 0u, forceFacing.TurnDurationMs },
                    new List<Vector3> { previousRotation, desiredRotation });
            }
            else
            {
                target.Rotation = desiredRotation;
            }

            SpellEffectDiagnostics.TraceForceFacing(spell, target, forceFacing, npcVariant, previousRotation.X, desiredRotation.X, true, null);
        }

        private static bool TryResolveForceFacingRotation(ISpell spell, IUnitEntity target, SpellEffectForceFacingSemantics forceFacing, out Vector3 rotation, out string skippedReason)
        {
            Vector3 currentRotation = target.Rotation;
            if (forceFacing.UsesAngleOffset)
            {
                rotation = new Vector3(
                    NormaliseYaw(currentRotation.X + DegreesToRadians(forceFacing.AngleDegrees)),
                    currentRotation.Y,
                    currentRotation.Z);
                skippedReason = null;
                return true;
            }

            if (TryResolveForceFacingPoint(spell, target, out Vector3 point, out skippedReason))
            {
                rotation = new Vector3(target.Position.GetAngle(point), currentRotation.Y, currentRotation.Z);
                return true;
            }

            rotation = currentRotation;
            return false;
        }

        private static bool TryResolveForceFacingPoint(ISpell spell, IUnitEntity target, out Vector3 point, out string skippedReason)
        {
            if (spell.Parameters.PrimaryTargetId != 0u && spell.Parameters.PrimaryTargetId != target.Guid)
            {
                IUnitEntity primaryTarget = spell.Caster.GetVisible<IUnitEntity>(spell.Parameters.PrimaryTargetId);
                if (primaryTarget != null && IsUsefulFacingPoint(target, primaryTarget.Position))
                {
                    point = primaryTarget.Position;
                    skippedReason = null;
                    return true;
                }
            }

            if (spell.Parameters.Position != null && IsUsefulFacingPoint(target, spell.Parameters.Position.Vector))
            {
                point = spell.Parameters.Position.Vector;
                skippedReason = null;
                return true;
            }

            if (target.Guid != spell.Caster.Guid && IsUsefulFacingPoint(target, spell.Caster.Position))
            {
                point = spell.Caster.Position;
                skippedReason = null;
                return true;
            }

            point = default;
            skippedReason = "no-facing-point";
            return false;
        }

        private static bool IsUsefulFacingPoint(IUnitEntity target, Vector3 point)
        {
            Vector3 delta = point - target.Position;
            delta.Y = 0f;
            return delta.LengthSquared() > 0.0001f;
        }

        private static float DegreesToRadians(float degrees)
        {
            return degrees * MathF.PI / 180f;
        }

        private static float NormaliseYaw(float radians)
        {
            float yaw = radians % (MathF.PI * 2f);
            if (yaw > MathF.PI)
                yaw -= MathF.PI * 2f;
            else if (yaw < -MathF.PI)
                yaw += MathF.PI * 2f;

            return yaw;
        }

        [SpellEffectHandler(SpellEffectType.ForcedMove)]
        public static void HandleEffectForcedMove(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectForcedMoveSemantics forcedMove = SpellEffectInterpreter.Interpret(info).ForcedMove;
            if (forcedMove == null)
                return;

            SpellEffectForcedMoveType moveType = (SpellEffectForcedMoveType)forcedMove.MovementType;
            if (UsesVelocityForcedMoveFallback(moveType))
            {
                float fallbackSpeed = ResolveForcedMoveSpeed(forcedMove);
                SpellEffectDiagnostics.TraceForcedMove(spell, target, forcedMove, fallbackSpeed);
                ApplyVelocityForcedMove(spell, target, forcedMove, fallbackSpeed);
                return;
            }

            if (!TryResolveForcedMove(spell, target, forcedMove, out IUnitEntity mover, out Vector3 position, out float angle, out TimeSpan flightTime, out float gravity, out float speed))
            {
                SpellEffectDiagnostics.TraceForcedMove(spell, target, forcedMove, 0f);
                return;
            }

            SpellEffectDiagnostics.TraceForcedMove(spell, target, forcedMove, speed);

            switch (moveType)
            {
                case SpellEffectForcedMoveType.PositionForward:
                case SpellEffectForcedMoveType.PositionBackward:
                case SpellEffectForcedMoveType.PositionRandom:
                case SpellEffectForcedMoveType.Unknown8:
                    ApplyDirectForcedMove(mover, position);
                    break;
                case SpellEffectForcedMoveType.KeyForward:
                case SpellEffectForcedMoveType.KeyBackward:
                case SpellEffectForcedMoveType.KeyRandom:
                case SpellEffectForcedMoveType.Unknown9:
                case SpellEffectForcedMoveType.Unknown10:
                case SpellEffectForcedMoveType.Unknown11:
                case SpellEffectForcedMoveType.Unknown12:
                case SpellEffectForcedMoveType.Unknown13:
                case SpellEffectForcedMoveType.Unknown15:
                    ApplyKeyedForcedMove(spell, target, forcedMove, mover, position, angle, flightTime, gravity, speed);
                    break;
            }
        }

        private static bool TryResolveForcedMove(
            ISpell spell,
            IUnitEntity target,
            SpellEffectForcedMoveSemantics forcedMove,
            out IUnitEntity mover,
            out Vector3 position,
            out float angle,
            out TimeSpan flightTime,
            out float gravity,
            out float speed)
        {
            mover      = ResolveForcedMoveMover(spell, target, forcedMove);
            position   = Vector3.Zero;
            angle      = 0f;
            flightTime = TimeSpan.FromMilliseconds(forcedMove.DurationTime);
            gravity    = ResolveForcedMoveGravity(forcedMove, flightTime);

            float distance = ResolveForcedMoveDistance(forcedMove);
            speed = flightTime > TimeSpan.Zero && distance > 0f
                ? distance / (float)flightTime.TotalSeconds
                : ResolveForcedMoveSpeed(forcedMove);

            switch ((SpellEffectForcedMoveType)forcedMove.MovementType)
            {
                case SpellEffectForcedMoveType.PositionForward:
                case SpellEffectForcedMoveType.KeyForward:
                case SpellEffectForcedMoveType.Unknown11:
                    angle = -target.Rotation.X - MathF.PI / 2f;
                    if (forcedMove.DataFloat07 != 0f)
                        angle -= forcedMove.DataFloat07.ToRadians();

                    position = target.Position.GetPoint2D(angle, distance);
                    break;
                case SpellEffectForcedMoveType.PositionBackward:
                case SpellEffectForcedMoveType.KeyBackward:
                case SpellEffectForcedMoveType.Unknown12:
                    angle = -target.Rotation.X + MathF.PI / 2f;
                    if (forcedMove.DataFloat07 != 0f)
                        angle += forcedMove.DataFloat07.ToRadians();

                    position = target.Position.GetPoint2D(angle, distance);
                    break;
                case SpellEffectForcedMoveType.PositionRandom:
                case SpellEffectForcedMoveType.KeyRandom:
                case SpellEffectForcedMoveType.Unknown13:
                    angle = (float)Random.Shared.NextDouble() * MathF.PI * 2f;
                    position = target.Position.GetPoint2D(angle, distance);
                    break;
                case SpellEffectForcedMoveType.Unknown8:
                case SpellEffectForcedMoveType.Unknown9:
                case SpellEffectForcedMoveType.Unknown10:
                case SpellEffectForcedMoveType.Unknown15:
                    mover = target;
                    position = spell.Caster.Position;
                    angle = target.Position.GetAngle(position);
                    break;
                default:
                    return false;
            }

            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
                return false;

            float? terrainHeight = mover.Map?.GetTerrainHeight(position.X, position.Z);
            if (terrainHeight.HasValue && position.Y < terrainHeight.Value)
                position.Y = terrainHeight.Value;

            return true;
        }

        private static IUnitEntity ResolveForcedMoveMover(ISpell spell, IUnitEntity target, SpellEffectForcedMoveSemantics forcedMove)
        {
            SpellEffectForcedMoveFlags flags = (SpellEffectForcedMoveFlags)forcedMove.Flags;
            return (flags & SpellEffectForcedMoveFlags.Target) != 0 ? target : spell.Caster;
        }

        private static float ResolveForcedMoveDistance(SpellEffectForcedMoveSemantics forcedMove)
        {
            float minDistance = IsFinitePositive(forcedMove.DataFloat01) ? forcedMove.DataFloat01 : 0f;
            float maxDistance = IsFinitePositive(forcedMove.DataFloat02) ? forcedMove.DataFloat02 : minDistance;

            if (maxDistance < minDistance)
                (minDistance, maxDistance) = (maxDistance, minDistance);

            if (maxDistance == minDistance)
                return minDistance;

            return minDistance + (float)Random.Shared.NextDouble() * (maxDistance - minDistance);
        }

        private static float ResolveForcedMoveGravity(SpellEffectForcedMoveSemantics forcedMove, TimeSpan flightTime)
        {
            float gravity = float.IsFinite(forcedMove.Gravity) ? forcedMove.Gravity : 0f;
            if (flightTime <= TimeSpan.Zero)
                return gravity;

            if (!float.IsFinite(forcedMove.DataFloat06) || forcedMove.DataFloat06 == 0f)
                return gravity;

            float seconds = (float)flightTime.TotalSeconds;
            return (forcedMove.DataFloat06 * 8f) / (seconds * seconds);
        }

        private static void ApplyDirectForcedMove(IUnitEntity mover, Vector3 position)
        {
            if (mover is IPlayer player)
                player.TeleportToLocal(position, false);
            else
                mover.MovementManager.SetPosition(position, false);
        }

        private static void ApplyKeyedForcedMove(ISpell spell, IUnitEntity target, SpellEffectForcedMoveSemantics forcedMove, IUnitEntity mover, Vector3 position, float angle, TimeSpan flightTime, float gravity, float fallbackSpeed)
        {
            if (!mover.MovementManager.ServerControl)
            {
                ApplyDirectForcedMove(mover, position);
                return;
            }

            IForcedMovementGenerator forcedMovementGenerator = LegacyServiceProvider.Provider?.GetService<IForcedMovementGenerator>();
            if (forcedMovementGenerator == null || flightTime <= TimeSpan.Zero)
            {
                ApplyVelocityForcedMove(spell, target, forcedMove, fallbackSpeed);
                return;
            }

            float spin = forcedMove.DataFloat08 != 0f
                ? (forcedMove.DataFloat08 * MathF.PI * 2f) / (float)flightTime.TotalSeconds
                : 0f;

            forcedMovementGenerator.ForceMove(mover, position, new Vector3(angle, 0f, 0f), flightTime, gravity, spin);
        }

        private static bool UsesVelocityForcedMoveFallback(SpellEffectForcedMoveType moveType)
        {
            return moveType
                is SpellEffectForcedMoveType.KeyVelocity
                or SpellEffectForcedMoveType.PositionVelocity
                or SpellEffectForcedMoveType.Unknown14;
        }

        private static void ApplyVelocityForcedMove(ISpell spell, IUnitEntity target, SpellEffectForcedMoveSemantics forcedMove, float speed)
        {
            if (speed <= 0f)
                return;

            Vector3 direction = ResolveForcedMoveDirection(spell, target, forcedMove);
            IUnitEntity mover = ResolveForcedMoveMover(spell, target, forcedMove);
            mover.MovementManager.SetState(mover.MovementManager.GetState() | StateFlags.Velocity);
            mover.MovementManager.SetVelocity(direction * speed, false);
        }

        private static float ResolveForcedMoveSpeed(SpellEffectForcedMoveSemantics forcedMove)
        {
            if (IsUsableForcedMoveMagnitude(forcedMove.DataFloat06))
                return forcedMove.DataFloat06;

            if (IsUsableForcedMoveMagnitude(forcedMove.DataFloat08))
                return forcedMove.DataFloat08;

            return new[]
            {
                forcedMove.DataFloat01,
                forcedMove.DataFloat02
            }
                .Where(IsUsableForcedMoveMagnitude)
                .DefaultIfEmpty(0f)
                .Max();
        }

        private static bool IsUsableForcedMoveMagnitude(float value)
        {
            return float.IsFinite(value) && value > 0f && value < 100f;
        }

        private static bool IsFinitePositive(float value)
        {
            return float.IsFinite(value) && value > 0f;
        }

        private static Vector3 ResolveForcedMoveDirection(ISpell spell, IUnitEntity target, SpellEffectForcedMoveSemantics forcedMove)
        {
            Vector3 direction = target.Position - spell.Caster.Position;
            direction.Y = 0f;

            if (direction.LengthSquared() < 0.0001f)
            {
                float angle = -spell.Caster.Rotation.X + MathF.PI / 2f;
                direction = new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
            }
            else
            {
                direction = Vector3.Normalize(direction);
            }

            return IsPullForcedMoveType(forcedMove.MovementType) ? -direction : direction;
        }

        private static bool IsPullForcedMoveType(uint movementType)
        {
            return movementType is 9u or 10u;
        }

        [SpellEffectHandler(SpellEffectType.Resurrect)]
        public static void HandleEffectResurrect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
                return;

            player.ResurrectionManager.ResurrectRequest(spell.Caster.Guid);
        }

        [SpellEffectHandler(SpellEffectType.Proxy)]
        public static void HandleEffectProxy(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleProxySpell(spell, target, target, info);
        }

        public static void HandleEffectProxyWorld(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            HandleProxySpell(spell, spell.Caster, target, info);
        }

        [SpellEffectHandler(SpellEffectType.ProxyLinearAE)]
        public static void HandleEffectProxyLinearAE(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleProxySpell(spell, target, target, info);
        }

        [SpellEffectHandler(SpellEffectType.ProxyChannel)]
        public static void HandleEffectProxyChannel(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleProxySpell(spell, target, target, info);
        }

        [SpellEffectHandler(SpellEffectType.ProxyChannelVariableTime)]
        public static void HandleEffectProxyChannelVariableTime(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleProxySpell(spell, target, target, info);
        }

        private static void HandleProxySpell(ISpell spell, IUnitEntity proxyCaster, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectProxySemantics proxy = SpellEffectInterpreter.Interpret(info).Proxy;
            if (proxy == null || proxy.Spell4Id == 0u)
                return;

            SpellEffectDiagnostics.TraceProxy(spell, target, info, proxy);
            proxyCaster.CastSpell(proxy.Spell4Id, new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                PrimaryTargetId        = target.Guid,
                UserInitiatedSpellCast = false,
                ClientContextToken     = spell.Parameters.ClientContextToken,
                ClientRequestSource    = spell.Parameters.ClientRequestSource
            });
        }

        [SpellEffectHandler(SpellEffectType.DespawnUnit)]
        public static void HandleEffectDespawnUnit(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDespawnUnitCore(spell, target, info);
        }

        public static void HandleEffectDespawnUnitWorld(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectDespawnUnitCore(spell, target, info);
        }

        private static void HandleEffectDespawnUnitCore(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            bool removed = target is not IPlayer && target.InWorld;
            SpellEffectDiagnostics.TraceDespawnUnit(spell, target, info, removed);
            if (!removed)
                return;

            target.RemoveFromMap();
        }

        [SpellEffectHandler(SpellEffectType.Activate)]
        public static void HandleEffectActivate(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectActivateCore(spell, target, info);
        }

        public static void HandleEffectActivateWorld(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectActivateCore(spell, target, info);
        }

        private static void HandleEffectActivateCore(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectActivateSemantics activate = SpellEffectInterpreter.Interpret(info).Activate;
            if (activate == null)
                return;

            IPlayer player = spell.Caster as IPlayer ?? target as IPlayer;
            IWorldEntity activatedEntity = player == spell.Caster ? target : spell.Caster;
            if (player == null || activatedEntity == null || activatedEntity.Guid == player.Guid)
            {
                SpellEffectDiagnostics.TraceActivate(spell, target, activate, player?.Guid ?? 0u, 0u, 0);
                return;
            }

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, activatedEntity.CreatureId, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity2, activatedEntity.CreatureId, 1u);

            IReadOnlyCollection<uint> targetGroupIds = AssetManager.Instance.GetTargetGroupsForCreatureId(activatedEntity.CreatureId);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateTargetGroup, activatedEntity.CreatureId, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateTargetGroupChecklist, activatedEntity.CreatureId, activatedEntity.QuestChecklistIdx);

            SpellEffectDiagnostics.TraceActivate(spell, target, activate, player.Guid, activatedEntity.CreatureId, targetGroupIds?.Count ?? 0);
        }

        [SpellEffectHandler(SpellEffectType.Stealth)]
        public static void HandleEffectStealth(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectStateSemantics stealth = SpellEffectInterpreter.Interpret(info).Stealth;
            if (stealth == null)
                return;

            bool changed = !target.IsStealthed;
            target.AddStealth(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId);
            target.CreateFlags |= EntityCreateFlag.IsStealthed;

            if (changed)
            {
                info.AddCombatLog(new CombatLogStealth
                {
                    UnitId   = target.Guid,
                    BExiting = false
                });
            }

            SpellEffectDiagnostics.TraceStealth(spell, target, stealth, false, changed);
        }

        [SpellEffectHandler(SpellEffectType.RemoveStealth)]
        public static void HandleEffectRemoveStealth(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectStateSemantics stealth = SpellEffectInterpreter.Interpret(info).RemoveStealth;
            if (stealth == null)
                return;

            bool changed = target.RemoveStealth();
            if (changed)
            {
                target.CreateFlags &= ~EntityCreateFlag.IsStealthed;
                info.AddCombatLog(new CombatLogStealth
                {
                    UnitId   = target.Guid,
                    BExiting = true
                });
            }

            SpellEffectDiagnostics.TraceStealth(spell, target, stealth, true, changed);
        }

        [SpellEffectHandler(SpellEffectType.AggroImmune)]
        public static void HandleEffectAggroImmune(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectStateSemantics aggroImmune = SpellEffectInterpreter.Interpret(info).AggroImmune;
            if (aggroImmune == null)
                return;

            bool changed = !target.IsAggroImmune;
            target.AddAggroImmune(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId);
            SpellEffectDiagnostics.TraceAggroImmune(spell, target, aggroImmune, changed);
        }

        [SpellEffectHandler(SpellEffectType.UnitStateSet)]
        public static void HandleEffectUnitStateSet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation interpretation = SpellEffectInterpreter.Interpret(info);
            SpellEffectUnitStateSetSemantics unitState = interpretation.UnitStateSet;
            if (unitState == null)
                return;

            if (interpretation.Parameters.Any(p => p.Type != SpellEffectParameterType.None))
            {
                SpellEffectDiagnostics.TraceUnitStateSet(spell, target, unitState, false, false, false, "parameter-driven");
                return;
            }

            if (unitState.StateId == 0u)
            {
                SpellEffectDiagnostics.TraceUnitStateSet(spell, target, unitState, false, false, false, "zero-state");
                return;
            }

            bool changed = !target.HasUnitState(unitState.StateId);
            target.AddUnitState(
                info.EffectId,
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                unitState.StateId,
                unitState.DataBits01,
                unitState.DataBits02,
                unitState.DataBits03,
                unitState.DataBits04,
                unitState.DataBits05,
                unitState.DataBits06,
                unitState.DataBits07,
                unitState.DataBits08,
                unitState.DataBits09);

            SpellEffectDiagnostics.TraceUnitStateSet(spell, target, unitState, changed, true, false, null);
        }

        [SpellEffectHandler(SpellEffectType.SetBusy)]
        public static void HandleEffectSetBusy(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectSetBusyCore(spell, target, info);
        }

        public static void HandleEffectSetBusyWorld(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectSetBusyCore(spell, target, info);
        }

        private static void HandleEffectSetBusyCore(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation interpretation = SpellEffectInterpreter.Interpret(info);
            SpellEffectSetBusySemantics setBusy = interpretation.SetBusy;
            if (setBusy == null)
                return;

            if (interpretation.Parameters.Any(p => p.Type != SpellEffectParameterType.None))
            {
                SpellEffectDiagnostics.TraceSetBusy(spell, target, setBusy, false, false, false, 0u, "parameter-driven");
                return;
            }

            if (setBusy.Busy)
            {
                bool changed = !target.IsBusy;
                target.AddBusy(
                    info.EffectId,
                    spell.Parameters.SpellInfo.Entry.Id,
                    spell.CastingId,
                    setBusy.Mode,
                    setBusy.ContextId,
                    setBusy.DataBits02,
                    setBusy.DataBits03,
                    setBusy.DataBits04,
                    setBusy.DataBits05,
                    setBusy.DataBits06,
                    setBusy.DataBits07,
                    setBusy.DataBits08,
                    setBusy.DataBits09);

                SpellEffectDiagnostics.TraceSetBusy(spell, target, setBusy, changed, true, false, 0u, null);
                return;
            }

            IReadOnlyCollection<uint> removedEffectIds = target.ClearBusy(spell.Parameters.SpellInfo.Entry.Id, setBusy.ContextId);
            SpellEffectDiagnostics.TraceSetBusy(spell, target, setBusy, removedEffectIds.Count != 0, false, removedEffectIds.Count != 0, (uint)removedEffectIds.Count, null);
        }

        [SpellEffectHandler(SpellEffectType.Disguise)]
        public static void HandleEffectDisguise(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            Creature2Entry creature2 = GameTableManager.Instance.Creature2.GetEntry(info.Entry.DataBits02);
            if (creature2 == null)
                return;

            Creature2DisplayGroupEntryEntry displayGroupEntry = GameTableManager.Instance.Creature2DisplayGroupEntry.Entries.FirstOrDefault(d => d.Creature2DisplayGroupId == creature2.Creature2DisplayGroupId);
            if (displayGroupEntry == null)
                return;

            target.DisplayInfo = displayGroupEntry.Creature2DisplayInfoId;
        }

        [SpellEffectHandler(SpellEffectType.SummonMount)]
        public static void HandleEffectSummonMount(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-mount", 0u, info.Entry.DataBits00, 0u, false, "target-not-player");
                return;
            }

            if (!player.CanMount())
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-mount", player.Guid, info.Entry.DataBits00, 0u, false, "cannot-mount");
                return;
            }

            if (GameTableManager.Instance.Creature2.GetEntry(info.Entry.DataBits00) == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-mount", player.Guid, info.Entry.DataBits00, 0u, false, "unknown-creature2");
                return;
            }

            IEntityFactory factory = GetEntityFactory();
            if (factory == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-mount", player.Guid, info.Entry.DataBits00, 0u, false, "missing-entity-factory");
                return;
            }

            IMountEntity mount = factory.CreateEntity<IMountEntity>();
            mount.Initialise(player, spell.Parameters.SpellInfo.Entry.Id, info.Entry.DataBits00, info.Entry.DataBits01, info.Entry.DataBits04);
            mount.EnqueuePassengerAdd(player, VehicleSeatType.Pilot, 0);

            // usually for hover boards
            /*if (info.Entry.DataBits04 > 0u)
            {
                mount.SetAppearance(new ItemVisual
                {
                    Slot      = ItemSlot.Mount,
                    DisplayId = (ushort)info.Entry.DataBits04
                });
            }*/

            var position = new MapPosition
            {
                Position = player.Position
            };

            if (player.Map.CanEnter(mount, position))
            {
                player.Map.EnqueueAdd(mount, position);
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-mount", player.Guid, info.Entry.DataBits00, spell.Parameters.SpellInfo.Entry.Id, true, null);
            }
            else
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-mount", player.Guid, info.Entry.DataBits00, spell.Parameters.SpellInfo.Entry.Id, false, "map-cannot-enter");

            // FIXME: also cast 52539,Riding License - Riding Skill 1 - SWC - Tier 1,34464
            // FIXME: also cast 80530,Mount Sprint  - Tier 2,36122

            player.CastSpell(52539, new SpellParameters());
            player.CastSpell(80530, new SpellParameters());
        }

        [SpellEffectHandler(SpellEffectType.Disembark)]
        public static void HandleEffectDisembark(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectDisembarkSemantics disembark = SpellEffectInterpreter.Interpret(info).Disembark;
            if (disembark == null)
                return;

            IPlayer player = target as IPlayer ?? GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceDisembark(spell, target, disembark, 0u, false, false, "no-player");
                return;
            }

            bool wasMounted = player.PlatformGuid != null;
            if (!wasMounted)
            {
                SpellEffectDiagnostics.TraceDisembark(spell, target, disembark, player.Guid, false, false, "not-mounted");
                return;
            }

            player.Dismount();
            info.AddCombatLog(new CombatLogMount
            {
                BDismounted = true,
                CastData    = new CombatLogCastData
                {
                    CasterId     = spell.Caster.Guid,
                    TargetId     = player.Guid,
                    SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                    CombatResult = CombatResult.Hit
                }
            });
            SpellEffectDiagnostics.TraceDisembark(spell, target, disembark, player.Guid, true, true, null);
        }

        [SpellEffectHandler(SpellEffectType.ActionBarSet)]
        public static void HandleEffectActionBarSet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectActionBarSetSemantics actionBarSet = SpellEffectInterpreter.Interpret(info).ActionBarSet;
            if (actionBarSet == null)
                return;

            IPlayer player = target as IPlayer ?? GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceActionBarSet(spell, target, actionBarSet, 0u, target.Guid, ShortcutSet.FloatingSpellBar, false, "no-player");
                return;
            }

            if (actionBarSet.ActionBarShortcutSetId == 0u || actionBarSet.ActionBarShortcutSetId > ActionBarShortcutSetPacketMax)
            {
                SpellEffectDiagnostics.TraceActionBarSet(spell, target, actionBarSet, player.Guid, target.Guid, ShortcutSet.FloatingSpellBar, false, "invalid-shortcut-set-id");
                return;
            }

            ActionBarShortcutSetEntry actionBarShortcutSetEntry = GameTableManager.Instance.ActionBarShortcutSet.GetEntry(actionBarSet.ActionBarShortcutSetId);
            if (actionBarShortcutSetEntry == null)
            {
                SpellEffectDiagnostics.TraceActionBarSet(spell, target, actionBarSet, player.Guid, target.Guid, ShortcutSet.FloatingSpellBar, false, "unknown-shortcut-set-id");
                return;
            }

            const ShortcutSet shortcutSet = ShortcutSet.FloatingSpellBar;
            uint associatedUnitId = target.Guid != 0u ? target.Guid : player.Guid;
            player.Session.EnqueueMessageEncrypted(new ServerShowActionBar
            {
                ShortcutSet            = shortcutSet,
                ActionBarShortcutSetId = (ushort)actionBarSet.ActionBarShortcutSetId,
                AssociatedUnitId       = associatedUnitId
            });
            SpellEffectDiagnostics.TraceActionBarSet(spell, target, actionBarSet, player.Guid, associatedUnitId, shortcutSet, true, null);
        }

        [SpellEffectHandler(SpellEffectType.Teleport)]
        public static void HandleEffectTeleport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectTeleportSemantics teleport = SpellEffectInterpreter.Interpret(info).Teleport;
            if (teleport == null || teleport.WorldLocation2Id == 0u)
                return;

            WorldLocation2Entry locationEntry = GameTableManager.Instance.WorldLocation2.GetEntry(teleport.WorldLocation2Id);
            if (locationEntry == null)
                return;

            if (target is IPlayer player)
                if (player.CanTeleport())
                    player.TeleportTo((ushort)locationEntry.WorldId, locationEntry.Position0, locationEntry.Position1, locationEntry.Position2);
        }

        [SpellEffectHandler(SpellEffectType.HousingTeleport)]
        public static void HandleEffectHousingTeleport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectHousingTeleport(spell, target, info, false);
        }

        [SpellEffectHandler(SpellEffectType.HousingEscape)]
        public static void HandleEffectHousingEscape(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectHousingTeleport(spell, target, info, true);
        }

        private static void HandleEffectHousingTeleport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info, bool escapeVariant)
        {
            SpellEffectHousingTeleportSemantics housingTeleport = SpellEffectInterpreter.Interpret(info).HousingTeleport;
            if (housingTeleport == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceHousingTeleport(spell, target, housingTeleport, escapeVariant, 0u, false, "no-player-owner");
                return;
            }

            if (housingTeleport.Mode != 0u)
            {
                SpellEffectDiagnostics.TraceHousingTeleport(spell, target, housingTeleport, escapeVariant, player.Guid, false, "evidence-gap-mode");
                return;
            }

            if (!player.CanTeleport())
            {
                SpellEffectDiagnostics.TraceHousingTeleport(spell, target, housingTeleport, escapeVariant, player.Guid, false, "pending-teleport");
                return;
            }

            IResidence residence = GlobalResidenceManager.Instance.GetResidenceByOwner(player.Name)
                ?? GlobalResidenceManager.Instance.CreateResidence(player);
            if (residence == null)
            {
                SpellEffectDiagnostics.TraceHousingTeleport(spell, target, housingTeleport, escapeVariant, player.Guid, false, "missing-residence");
                return;
            }

            IResidenceEntrance entrance;
            try
            {
                entrance = GlobalResidenceManager.Instance.GetResidenceEntrance(residence.PropertyInfoId);
            }
            catch (HousingException)
            {
                SpellEffectDiagnostics.TraceHousingTeleport(spell, target, housingTeleport, escapeVariant, player.Guid, false, "missing-entrance");
                return;
            }

            IMapLock mapLock = MapLockManager.Instance.GetResidenceLock(residence.Parent ?? residence);
            player.Rotation = entrance.Rotation.ToEuler();
            player.TeleportTo(entrance.Entry, entrance.Position, mapLock);

            SpellEffectDiagnostics.TraceHousingTeleport(spell, target, housingTeleport, escapeVariant, player.Guid, true, null);
        }

        [SpellEffectHandler(SpellEffectType.SupportStuck)]
        public static void HandleEffectSupportStuck(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectSupportStuckSemantics supportStuck = SpellEffectInterpreter.Interpret(info).SupportStuck;
            if (supportStuck == null)
                return;

            if (target is not IPlayer)
            {
                SpellEffectDiagnostics.TraceSupportStuck(spell, target, supportStuck, target.Health, target.Health, false, "target-not-player");
                return;
            }

            if (!target.IsAlive)
            {
                SpellEffectDiagnostics.TraceSupportStuck(spell, target, supportStuck, target.Health, target.Health, false, "target-not-alive");
                return;
            }

            uint healthBefore = target.Health;
            target.ModifyHealth(Math.Max(healthBefore, 1u), DamageType.Physical, spell.Caster);
            SpellEffectDiagnostics.TraceSupportStuck(spell, target, supportStuck, healthBefore, target.Health, true, null);

            if (!target.IsAlive)
            {
                spell.Caster.ProbeProcEvent("target-killed", ProcTriggerEventCandidate.KillTarget, spell.Caster, target, spell, info, null, "after-apply");
                info.AddCombatLog(new CombatLogDeath
                {
                    UnitId = target.Guid
                });
            }
        }

        [SpellEffectHandler(SpellEffectType.FullScreenEffect)]
        public static void HandleFullScreenEffect(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            // Duration-bearing spell effects are now finished by the central Spell lifetime scheduler.
        }

        [SpellEffectHandler(SpellEffectType.RapidTransport)]
        public static void HandleEffectRapidTransport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            TaxiNodeEntry taxiNode = GameTableManager.Instance.TaxiNode.GetEntry(spell.Parameters.TaxiNode);
            if (taxiNode == null)
                return;

            WorldLocation2Entry worldLocation = GameTableManager.Instance.WorldLocation2.GetEntry(taxiNode.WorldLocation2Id);
            if (worldLocation == null)
                return;

            if (target is not IPlayer player)
                return;

            if (!player.CanTeleport())
                return;

            var rotation = new Quaternion(worldLocation.Facing0, worldLocation.Facing0, worldLocation.Facing2, worldLocation.Facing3);
            player.Rotation = rotation.ToEuler();
            player.TeleportTo((ushort)worldLocation.WorldId, worldLocation.Position0, worldLocation.Position1, worldLocation.Position2);
        }

        [SpellEffectHandler(SpellEffectType.LearnDyeColor)]
        public static void HandleEffectLearnDyeColor(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "learn-dye-color", 0u, info.Entry.DataBits00, 0u, false, "target-not-player");
                return;
            }

            GenericUnlockEntryEntry entry = GameTableManager.Instance.GenericUnlockEntry.GetEntry(info.Entry.DataBits00);
            bool alreadyUnlocked = entry != null && player.Account.GenericUnlockManager.IsUnlocked(entry.GenericUnlockTypeEnum, entry.UnlockObject);
            player.Account.GenericUnlockManager.Unlock((ushort)info.Entry.DataBits00);
            SpellEffectDiagnostics.TracePlayerCollection(spell, target, "learn-dye-color", player.Guid, info.Entry.DataBits00, entry?.UnlockObject ?? 0u, !alreadyUnlocked && entry != null, entry == null ? "unknown-generic-unlock" : alreadyUnlocked ? "already-unlocked" : null);
        }

        [SpellEffectHandler(SpellEffectType.UnlockMount)]
        public static void HandleEffectUnlockMount(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-mount", 0u, info.Entry.DataBits00, 0u, false, "target-not-player");
                return;
            }

            if (!TryLearnCollectionSpell(player, info.Entry.DataBits00, out uint spell4BaseId, out string skippedReason))
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-mount", player.Guid, info.Entry.DataBits00, spell4BaseId, false, skippedReason);
                return;
            }

            player.Session.EnqueueMessageEncrypted(new ServerUnlockMount
            {
                Spell4Id = info.Entry.DataBits00
            });
            SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-mount", player.Guid, info.Entry.DataBits00, spell4BaseId, true, null);
        }

        [SpellEffectHandler(SpellEffectType.UnlockPetFlair)]
        public static void HandleEffectUnlockPetFlair(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-pet-flair", 0u, info.Entry.DataBits00, 0u, false, "target-not-player");
                return;
            }

            if (info.Entry.DataBits00 == 0u)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-pet-flair", player.Guid, info.Entry.DataBits00, 0u, false, "missing-pet-flair");
                return;
            }

            if (GameTableManager.Instance.PetFlair.GetEntry(info.Entry.DataBits00) == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-pet-flair", player.Guid, info.Entry.DataBits00, 0u, false, "unknown-pet-flair");
                return;
            }

            ushort petFlairId = (ushort)info.Entry.DataBits00;
            if (player.PetCustomisationManager.HasFlair(petFlairId))
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-pet-flair", player.Guid, info.Entry.DataBits00, 0u, false, "already-unlocked");
                return;
            }

            player.PetCustomisationManager.UnlockFlair(petFlairId);
            SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-pet-flair", player.Guid, info.Entry.DataBits00, 0u, true, null);
        }

        [SpellEffectHandler(SpellEffectType.UnlockVanityPet)]
        public static void HandleEffectUnlockVanityPet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-vanity-pet", 0u, info.Entry.DataBits00, 0u, false, "target-not-player");
                return;
            }

            if (!TryLearnCollectionSpell(player, info.Entry.DataBits00, out uint spell4BaseId, out string skippedReason))
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-vanity-pet", player.Guid, info.Entry.DataBits00, spell4BaseId, false, skippedReason);
                return;
            }

            player.Session.EnqueueMessageEncrypted(new ServerUnlockVanityPet
            {
                Spell4Id = info.Entry.DataBits00
            });
            SpellEffectDiagnostics.TracePlayerCollection(spell, target, "unlock-vanity-pet", player.Guid, info.Entry.DataBits00, spell4BaseId, true, null);
        }

        [SpellEffectHandler(SpellEffectType.SummonVanityPet)]
        public static void HandleEffectSummonVanityPet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-vanity-pet", 0u, info.Entry.DataBits00, 0u, false, "target-not-player");
                return;
            }

            if (GameTableManager.Instance.Creature2.GetEntry(info.Entry.DataBits00) == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-vanity-pet", player.Guid, info.Entry.DataBits00, 0u, false, "unknown-creature2");
                return;
            }

            // enqueue removal of existing vanity pet if summoned
            if (player.VanityPetGuid != null)
            {
                IPetEntity oldVanityPet = player.GetVisible<IPetEntity>(player.VanityPetGuid.Value);
                oldVanityPet?.RemoveFromMap();
                player.VanityPetGuid = null;
            }

            IEntityFactory factory = GetEntityFactory();
            if (factory == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-vanity-pet", player.Guid, info.Entry.DataBits00, 0u, false, "missing-entity-factory");
                return;
            }

            IPetEntity pet = factory.CreateEntity<IPetEntity>();
            pet.Initialise(player, info.Entry.DataBits00);

            var position = new MapPosition
            {
                Position = player.Position
            };

            if (player.Map.CanEnter(pet, position))
            {
                player.Map.EnqueueAdd(pet, position);
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-vanity-pet", player.Guid, info.Entry.DataBits00, 0u, true, null);
            }
            else
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "summon-vanity-pet", player.Guid, info.Entry.DataBits00, 0u, false, "map-cannot-enter");
        }

        [SpellEffectHandler(SpellEffectType.TitleGrant)]
        public static void HandleEffectTitleGrant(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "title-grant", 0u, info.Entry.DataBits00, 0u, false, "target-not-player");
                return;
            }

            if (GameTableManager.Instance.CharacterTitle.GetEntry(info.Entry.DataBits00) == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "title-grant", player.Guid, info.Entry.DataBits00, 0u, false, "unknown-title");
                return;
            }

            bool alreadyOwned = player.TitleManager.HasTitle((ushort)info.Entry.DataBits00);

            player.TitleManager.AddTitle((ushort)info.Entry.DataBits00);
            SpellEffectDiagnostics.TracePlayerCollection(spell, target, "title-grant", player.Guid, info.Entry.DataBits00, 0u, !alreadyOwned, alreadyOwned ? "already-owned" : null);
        }

        [SpellEffectHandler(SpellEffectType.TitleRevoke)]
        public static void HandleEffectTitleRevoke(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "title-revoke", 0u, info.Entry.DataBits00, 0u, false, "target-not-player");
                return;
            }

            if (GameTableManager.Instance.CharacterTitle.GetEntry(info.Entry.DataBits00) == null)
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "title-revoke", player.Guid, info.Entry.DataBits00, 0u, false, "unknown-title");
                return;
            }

            if (!player.TitleManager.HasTitle((ushort)info.Entry.DataBits00))
            {
                SpellEffectDiagnostics.TracePlayerCollection(spell, target, "title-revoke", player.Guid, info.Entry.DataBits00, 0u, false, "not-owned");
                return;
            }

            player.TitleManager.RevokeTitle((ushort)info.Entry.DataBits00);
            SpellEffectDiagnostics.TracePlayerCollection(spell, target, "title-revoke", player.Guid, info.Entry.DataBits00, 0u, true, null);
        }

        [SpellEffectHandler(SpellEffectType.Fluff)]
        public static void HandleEffectFluff(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
        }

        [SpellEffectHandler(SpellEffectType.Proc)]
        public static void HandleEffectProc(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectProcSemantics proc = SpellEffectInterpreter.Interpret(info).Proc;
            if (proc == null)
                return;

            if (proc.TriggerSpell4Id == 0u || GameTableManager.Instance.Spell4.GetEntry(proc.TriggerSpell4Id) == null)
            {
                SpellEffectDiagnostics.TraceProc(spell, target, proc, false, false, "unknown-trigger-spell4");
                return;
            }

            if (!float.IsFinite(proc.Chance) || proc.Chance < 0f)
            {
                SpellEffectDiagnostics.TraceProc(spell, target, proc, false, false, "invalid-chance");
                return;
            }

            target.AddProc(
                info.EffectId,
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                proc.TriggerEvent,
                proc.TriggerSpell4Id,
                proc.Chance,
                proc.TargetData,
                proc.CooldownMsOrSentinel,
                proc.DataBits05,
                proc.DataBits06,
                proc.DataBits07,
                proc.DataBits08,
                proc.DataBits09);

            SpellEffectDiagnostics.TraceProc(spell, target, proc, true, false, null);
        }

        [SpellEffectHandler(SpellEffectType.CCStateBreak)]
        public static void HandleEffectCCStateBreak(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectCCStateBreakSemantics ccStateBreak = SpellEffectInterpreter.Interpret(info).CCStateBreak;
            if (ccStateBreak == null)
                return;

            uint beforeMask = target.ActiveCCStateMask;
            if (ccStateBreak.StateMask == 0u)
            {
                SpellEffectDiagnostics.TraceCCStateBreak(spell, target, ccStateBreak, beforeMask, beforeMask, Array.Empty<(CCState State, uint EffectId)>());
                return;
            }

            IReadOnlyCollection<SpellStateRemoval> removedStates = target.RemoveCCStates(ccStateBreak.StateMask);
            uint afterMask = target.ActiveCCStateMask;
            SpellEffectDiagnostics.TraceCCStateBreak(spell, target, ccStateBreak, beforeMask, afterMask, ToCCStateTraceTuples(removedStates));

            foreach (SpellStateRemoval removal in removedStates.Where(r => r.CCState.HasValue))
            {
                CCState state = removal.CCState!.Value;
                target.EnqueueToVisible(new ServerEntityCCStateRemove
                {
                    UnitId              = target.Guid,
                    CCType              = state,
                    SpellCastUniqueId   = removal.CastingId == 0u ? spell.CastingId : removal.CastingId,
                    SpellEffectUniqueId = removal.EffectId,
                    Removed             = true
                }, true);

                info.AddCombatLog(new CombatLogCCStateBreak
                {
                    CasterId = spell.Caster.Guid,
                    State    = state
                });
            }
        }

        [SpellEffectHandler(SpellEffectType.CCStateSet)]
        public static void HandleEffectCCStateSet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectCCStateSemantics ccState = SpellEffectInterpreter.Interpret(info).CCState;
            if (ccState == null)
                return;

            if (info.Entry.DurationTime > 0u)
                target.AddCCState(ccState.State, info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId);

            ApplyCrowdControlMovementState(target, ccState.State);

            info.AddCombatLog(new CombatLogCCState
            {
                State                     = ccState.State,
                Result                    = CCStateApplyRulesResult.Ok,
                CcStateDiminishingReturnsId = ResolveCrowdControlDiminishingReturnsId(ccState.State),
                CastData = new CombatLogCastData
                {
                    CasterId     = spell.Caster.Guid,
                    TargetId     = target.Guid,
                    SpellId      = spell.Parameters.SpellInfo.Entry.Id,
                    CombatResult = CombatResult.Hit
                }
            });
        }

        private static IReadOnlyCollection<(CCState State, uint EffectId)> ToCCStateTraceTuples(IReadOnlyCollection<SpellStateRemoval> removals)
        {
            return removals.Where(r => r.CCState.HasValue)
                .Select(r => (r.CCState!.Value, r.EffectId))
                .ToArray();
        }

        private static void ApplyCrowdControlMovementState(IUnitEntity target, CCState state)
        {
            uint stateMask = 1u << (int)state;
            StateFlags filteredState = CrowdControlStateRules.FilterClientStateFlags(target.MovementManager.GetState(), stateMask);
            if (filteredState != target.MovementManager.GetState())
                target.MovementManager.SetState(filteredState);

            if (!CrowdControlStateRules.HasClientMovementBlock(stateMask))
                return;

            target.MovementManager.SetMove(Vector3.Zero, false);
            target.MovementManager.SetVelocity(Vector3.Zero, false);
        }

        private static ushort ResolveCrowdControlDiminishingReturnsId(CCState state)
        {
            return (ushort)(GameTableManager.Instance.CCStates.GetEntry((uint)state)?.CcStateDiminishingReturnsId ?? 0u);
        }

        [SpellEffectHandler(SpellEffectType.SpellDispel)]
        public static void HandleEffectSpellDispel(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectDispelSemantics dispel = SpellEffectInterpreter.Interpret(info).Dispel;
            if (dispel == null)
                return;

            uint maxCount = ResolveDispelCount(dispel);
            IReadOnlyCollection<SpellStateRemoval> removals = target.RemoveTrackedSpellStates(
                spell4Id => MatchesDispelClass(spell4Id, dispel),
                maxCount);

            SpellEffectDiagnostics.TraceDispel(spell, target, dispel, maxCount, removals.Count);
            SendTrackedStateRemovalMessages(spell, target, info, removals, true);
        }

        [SpellEffectHandler(SpellEffectType.ModifyAbilityCharges)]
        public static void HandleEffectModifyAbilityCharges(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectModifyAbilityChargesSemantics charges = SpellEffectInterpreter.Interpret(info).ModifyAbilityCharges;
            if (charges == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceModifyAbilityCharges(spell, target, charges, 0u, 0u, "none", "target-not-player");
                return;
            }

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(charges.Spell4Id);
            if (spell4Entry == null)
            {
                SpellEffectDiagnostics.TraceModifyAbilityCharges(spell, target, charges, 0u, 0u, "none", "unknown-spell4");
                return;
            }

            ICharacterSpell characterSpell = player.SpellManager.GetSpell(spell4Entry.Spell4BaseIdBaseSpell);
            if (characterSpell == null || characterSpell.MaxAbilityCharges == 0u)
            {
                SpellEffectDiagnostics.TraceModifyAbilityCharges(spell, target, charges, 0u, 0u, "none", "spell-not-known-or-uncharged");
                return;
            }

            uint beforeCharges = characterSpell.AbilityCharges;
            string action;
            switch (charges.Mode)
            {
                case 1u:
                    characterSpell.SetAbilityCharges(charges.Count);
                    action = "set";
                    break;
                default:
                    characterSpell.ModifyAbilityCharges((int)Math.Min(charges.Count, int.MaxValue));
                    action = "add";
                    break;
            }

            SpellEffectDiagnostics.TraceModifyAbilityCharges(spell, target, charges, beforeCharges, characterSpell.AbilityCharges, action, null);
        }

        [SpellEffectHandler(SpellEffectType.CooldownReset)]
        public static void HandleEffectCooldownReset(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectCooldownResetSemantics cooldownReset = SpellEffectInterpreter.Interpret(info).CooldownReset;
            if (cooldownReset == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceCooldownReset(spell, target, cooldownReset, "none", 0u, "target-not-player");
                return;
            }

            uint spell4Id = ResolveSpell4Id(cooldownReset.Spell4Id, cooldownReset.DataBits00);
            if (spell4Id != 0u)
            {
                player.SpellManager.SetSpellCooldown(spell4Id, 0d);
                SpellEffectDiagnostics.TraceCooldownReset(spell, target, cooldownReset, "reset-spell", spell4Id, null);
                return;
            }

            player.SpellManager.ResetAllSpellCooldowns();
            SpellEffectDiagnostics.TraceCooldownReset(spell, target, cooldownReset, "reset-all", 0u, null);
        }

        [SpellEffectHandler(SpellEffectType.ModifySpellCooldown)]
        public static void HandleEffectModifySpellCooldown(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectModifySpellCooldownSemantics cooldown = SpellEffectInterpreter.Interpret(info).ModifySpellCooldown;
            if (cooldown == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceModifySpellCooldown(spell, target, cooldown, "none", 0u, 0d, 0d, "target-not-player");
                return;
            }

            uint spell4Id = ResolveSpell4Id(cooldown.Spell4Id);
            if (spell4Id == 0u)
            {
                SpellEffectDiagnostics.TraceModifySpellCooldown(spell, target, cooldown, "none", 0u, 0d, 0d, "no-concrete-spell4-target");
                return;
            }

            double beforeCooldown = player.SpellManager.GetSpellCooldown(spell4Id);
            if (!TryResolveCooldownMutation(cooldown, beforeCooldown, out double afterCooldown, out string action, out string skippedReason))
            {
                SpellEffectDiagnostics.TraceModifySpellCooldown(spell, target, cooldown, "none", spell4Id, beforeCooldown, beforeCooldown, skippedReason);
                return;
            }

            player.SpellManager.SetSpellCooldown(spell4Id, afterCooldown);
            SpellEffectDiagnostics.TraceModifySpellCooldown(spell, target, cooldown, action, spell4Id, beforeCooldown, afterCooldown, null);
        }

        [SpellEffectHandler(SpellEffectType.ActivateSpellCooldown)]
        public static void HandleEffectActivateSpellCooldown(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectActivateSpellCooldownSemantics cooldown = SpellEffectInterpreter.Interpret(info).ActivateSpellCooldown;
            if (cooldown == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceActivateSpellCooldown(spell, target, cooldown, "none", 0u, 0d, "target-not-player");
                return;
            }

            uint spell4Id = ResolveSpell4Id(cooldown.Spell4Id, cooldown.DataBits00);
            Spell4Entry spell4Entry = spell4Id == 0u ? null : GameTableManager.Instance.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
            {
                SpellEffectDiagnostics.TraceActivateSpellCooldown(spell, target, cooldown, "none", 0u, 0d, "unknown-spell4");
                return;
            }

            double cooldownSeconds = spell4Entry.SpellCoolDown / 1000d;
            player.SpellManager.SetSpellCooldown(spell4Id, cooldownSeconds);
            SpellEffectDiagnostics.TraceActivateSpellCooldown(spell, target, cooldown, "activate", spell4Id, cooldownSeconds, null);
        }

        [SpellEffectHandler(SpellEffectType.SpellForceRemove)]
        public static void HandleEffectSpellForceRemove(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectForceRemoveSemantics forceRemove = SpellEffectInterpreter.Interpret(info).ForceRemove;
            if (forceRemove == null)
                return;

            if (!TryCreateForceRemovePredicate(forceRemove, out Func<uint, bool> predicate, out string removeScope, out string skippedReason))
            {
                SpellEffectDiagnostics.TraceForceRemove(spell, target, forceRemove, removeScope, 0, false, 0, skippedReason);
                return;
            }

            IReadOnlyCollection<SpellStateRemoval> removals = target.RemoveTrackedSpellStates(predicate, uint.MaxValue);
            bool removedProperties = removals.Any(r => r.Kind == SpellStateRemovalKind.PropertyModifier);
            IReadOnlyCollection<SpellStateRemoval> removedCCStates = removals.Where(r => r.Kind == SpellStateRemovalKind.CrowdControl).ToArray();
            SpellEffectDiagnostics.TraceForceRemove(spell, target, forceRemove, removeScope, removals.Count, removedProperties, removedCCStates.Count, null);

            SendTrackedStateRemovalMessages(spell, target, info, removals, false);
        }

        public static void HandleEffectSpellForceRemoveWorld(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectForceRemoveSemantics forceRemove = SpellEffectInterpreter.Interpret(info).ForceRemove;
            if (forceRemove == null)
                return;

            if (!TryCreateForceRemovePredicate(forceRemove, out System.Func<uint, bool> predicate, out string removeScope, out string skippedReason))
            {
                SpellEffectDiagnostics.TraceForceRemove(spell, target, forceRemove, removeScope, 0, skippedReason);
                return;
            }

            IReadOnlyCollection<uint> removedBusyEffectIds = target.RemoveBusy(predicate, uint.MaxValue);
            SpellEffectDiagnostics.TraceForceRemove(spell, target, forceRemove, removeScope, removedBusyEffectIds.Count, null);
        }

        [SpellEffectHandler(SpellEffectType.SpellForceRemoveChanneled)]
        public static void HandleEffectSpellForceRemoveChanneled(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            HandleEffectSpellForceRemove(spell, target, info);
        }

        [SpellEffectHandler(SpellEffectType.SpellEffectImmunity)]
        public static void HandleEffectSpellEffectImmunity(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectImmunitySemantics immunity = SpellEffectInterpreter.Interpret(info).SpellEffectImmunity;
            if (immunity == null)
                return;

            if (!Enum.IsDefined(typeof(SpellEffectType), (int)immunity.EffectTypeRaw))
            {
                SpellEffectDiagnostics.TraceSpellEffectImmunity(spell, target, immunity, false, false, "unknown-effect-type");
                return;
            }

            target.AddSpellEffectImmunity(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, immunity.EffectType);
            SpellEffectDiagnostics.TraceSpellEffectImmunity(spell, target, immunity, true, false, null);
        }

        [SpellEffectHandler(SpellEffectType.SpellImmunity)]
        public static void HandleEffectSpellImmunity(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellImmunitySemantics immunity = SpellEffectInterpreter.Interpret(info).SpellImmunity;
            if (immunity == null)
                return;

            if (immunity.Mode != 0u)
            {
                SpellEffectDiagnostics.TraceSpellImmunity(spell, target, immunity, false, false, "evidence-gap-mode");
                return;
            }

            Spell4Entry immuneSpell = GameTableManager.Instance.Spell4.GetEntry(immunity.Spell4Id);
            if (immuneSpell == null)
            {
                SpellEffectDiagnostics.TraceSpellImmunity(spell, target, immunity, false, false, "unknown-spell4");
                return;
            }

            target.AddSpellImmunity(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, immunity.Spell4Id, immunity.Mode);
            SpellEffectDiagnostics.TraceSpellImmunity(spell, target, immunity, true, false, null);
        }

        [SpellEffectHandler(SpellEffectType.Scale)]
        public static void HandleEffectScale(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectScaleSemantics scale = SpellEffectInterpreter.Interpret(info).Scale;
            if (scale == null)
                return;

            if (!float.IsFinite(scale.TargetScale) || scale.TargetScale <= 0f)
            {
                SpellEffectDiagnostics.TraceScale(spell, target, scale, target.MovementManager.GetScale(), false, false, "invalid-scale");
                return;
            }

            float previousScale = target.MovementManager.GetScale();
            if (info.Entry.DurationTime > 0u)
                target.AddScale(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, previousScale, scale.RestoreTimeMs);

            ApplyScale(target, previousScale, scale.TargetScale, scale.ApplyTimeMs);
            SpellEffectDiagnostics.TraceScale(spell, target, scale, previousScale, true, false, null);
        }

        [SpellEffectHandler(SpellEffectType.FactionSet)]
        public static void HandleEffectFactionSet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectFactionSetSemantics factionSet = SpellEffectInterpreter.Interpret(info).FactionSet;
            if (factionSet == null)
                return;

            if (factionSet.FactionId == 0u)
            {
                SpellEffectDiagnostics.TraceFactionSet(spell, target, factionSet, (uint)target.Faction1, false, false, "missing-faction");
                return;
            }

            Faction previousFaction = target.Faction1;
            if (info.Entry.DurationTime > 0u)
                target.AddFaction(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, previousFaction);

            target.SetFaction((Faction)factionSet.FactionId);
            SpellEffectDiagnostics.TraceFactionSet(spell, target, factionSet, (uint)previousFaction, true, false, null);
        }

        [SpellEffectHandler(SpellEffectType.AddSpell)]
        public static void HandleEffectAddSpell(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectAddSpellSemantics addSpell = SpellEffectInterpreter.Interpret(info).AddSpell;
            if (addSpell == null)
                return;

            if (target is not IPlayer player)
            {
                SpellEffectDiagnostics.TraceAddSpell(spell, target, addSpell, 0u, "none", "target-not-player");
                return;
            }

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(addSpell.Spell4Id);
            if (spell4Entry == null)
            {
                SpellEffectDiagnostics.TraceAddSpell(spell, target, addSpell, 0u, "none", "unknown-spell4");
                return;
            }

            uint spell4BaseId = spell4Entry.Spell4BaseIdBaseSpell;
            if (player.SpellManager.GetSpell(spell4BaseId) != null)
            {
                SpellEffectDiagnostics.TraceAddSpell(spell, target, addSpell, spell4BaseId, "none", "already-known");
                return;
            }

            player.SpellManager.AddSpell(spell4BaseId);
            SpellEffectDiagnostics.TraceAddSpell(spell, target, addSpell, spell4BaseId, "add", null);
        }

        [SpellEffectHandler(SpellEffectType.Kill)]
        public static void HandleEffectKill(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectKillSemantics kill = SpellEffectInterpreter.Interpret(info).Kill;
            if (kill == null)
                return;

            if (!target.IsAlive)
            {
                SpellEffectDiagnostics.TraceKill(spell, target, kill, target.Health, target.Health, false, "target-not-alive");
                return;
            }

            uint healthBefore = target.Health;
            target.ModifyHealth(Math.Max(healthBefore, 1u), DamageType.Physical, spell.Caster);
            SpellEffectDiagnostics.TraceKill(spell, target, kill, healthBefore, target.Health, true, null);

            if (!target.IsAlive)
            {
                spell.Caster.ProbeProcEvent("target-killed", ProcTriggerEventCandidate.KillTarget, spell.Caster, target, spell, info, null, "after-apply");
                info.AddCombatLog(new CombatLogDeath
                {
                    UnitId = target.Guid
                });
            }
        }

        [SpellEffectHandler(SpellEffectType.DelayDeath)]
        public static void HandleEffectDelayDeath(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectDelayDeathSemantics delayDeath = SpellEffectInterpreter.Interpret(info).DelayDeath;
            if (delayDeath == null)
                return;

            if (delayDeath.TriggerSpell4Id != 0u && GameTableManager.Instance.Spell4.GetEntry(delayDeath.TriggerSpell4Id) == null)
            {
                SpellEffectDiagnostics.TraceDelayDeath(spell, target, delayDeath, false, false, "unknown-trigger-spell4");
                return;
            }

            target.AddDelayDeath(
                info.EffectId,
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                delayDeath.Mode,
                delayDeath.TriggerSpell4Id,
                delayDeath.TriggerDelayMs,
                delayDeath.DataBits03,
                delayDeath.DataBits04,
                delayDeath.DataBits05,
                delayDeath.DataBits06,
                delayDeath.DataBits07);

            SpellEffectDiagnostics.TraceDelayDeath(spell, target, delayDeath, true, false, null);
        }

        [SpellEffectHandler(SpellEffectType.ClampVital)]
        public static void HandleEffectClampVital(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectClampVitalSemantics clampVital = SpellEffectInterpreter.Interpret(info).ClampVital;
            if (clampVital == null)
                return;

            if (!float.IsFinite(clampVital.Ratio) || clampVital.Ratio <= 0f)
            {
                SpellEffectDiagnostics.TraceClampVital(spell, target, clampVital, Vital.Invalid, target.Health, target.Health, false, false, "invalid-ratio");
                return;
            }

            Vital vital = ResolveClampVital(clampVital);
            if (vital != Vital.Health)
            {
                SpellEffectDiagnostics.TraceClampVital(spell, target, clampVital, vital, target.Health, target.Health, false, false, "evidence-gap-vital");
                return;
            }

            uint healthBefore = target.Health;
            target.AddVitalClamp(
                info.EffectId,
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                vital,
                clampVital.Ratio,
                clampVital.Mode,
                clampVital.VitalMode);

            SpellEffectDiagnostics.TraceClampVital(spell, target, clampVital, vital, healthBefore, target.Health, true, false, null);
        }

        private static Vital ResolveClampVital(SpellEffectClampVitalSemantics clampVital)
        {
            return Vital.Health;
        }

        [SpellEffectHandler(SpellEffectType.ShieldOverload)]
        public static void HandleEffectShieldOverload(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectInterpretation interpretation = SpellEffectInterpreter.Interpret(info);
            SpellEffectShieldOverloadSemantics shieldOverload = interpretation.ShieldOverload;
            if (shieldOverload == null)
                return;

            if (interpretation.Parameters.Any(p => p.Type != SpellEffectParameterType.None))
            {
                SpellEffectDiagnostics.TraceShieldOverload(spell, target, shieldOverload, target.Shield, target.Shield, false, false, "parameter-driven");
                return;
            }

            if (!IsSimpleShieldOverload(shieldOverload))
            {
                SpellEffectDiagnostics.TraceShieldOverload(spell, target, shieldOverload, target.Shield, target.Shield, false, false, "secondary-payload");
                return;
            }

            uint shieldBefore = target.Shield;
            target.AddShieldOverload(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId);
            target.Shield = 0u;
            SpellEffectDiagnostics.TraceShieldOverload(spell, target, shieldOverload, shieldBefore, target.Shield, true, false, null);
        }

        private static bool IsSimpleShieldOverload(SpellEffectShieldOverloadSemantics shieldOverload)
        {
            return shieldOverload.DataBits00 == 0u &&
                shieldOverload.DataBits01 == 0u &&
                shieldOverload.DataBits02 == 0u &&
                shieldOverload.DataBits03 == 0u &&
                shieldOverload.DataBits04 == 0u &&
                shieldOverload.DataBits05 == 0u &&
                shieldOverload.DataBits06 == 0u &&
                shieldOverload.DataBits07 == 0u &&
                shieldOverload.DataBits08 == 0u &&
                shieldOverload.DataBits09 == 0u;
        }

        [SpellEffectHandler(SpellEffectType.GrantXP)]
        public static void HandleEffectGrantXp(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectGrantXpSemantics grantXp = SpellEffectInterpreter.Interpret(info).GrantXp;
            if (grantXp == null)
                return;

            if (target is not IPlayer player)
            {
                SpellEffectDiagnostics.TraceGrantXp(spell, target, grantXp, false, "target-not-player");
                return;
            }

            if (grantXp.Amount == 0u)
            {
                SpellEffectDiagnostics.TraceGrantXp(spell, target, grantXp, false, "zero-amount");
                return;
            }

            player.XpManager.GrantXp(grantXp.Amount, ExpReason.Spell);
            SpellEffectDiagnostics.TraceGrantXp(spell, target, grantXp, true, null);
        }

        [SpellEffectHandler(SpellEffectType.PathXpModify)]
        public static void HandleEffectPathXpModify(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectPathXpModifySemantics pathXp = SpellEffectInterpreter.Interpret(info).PathXpModify;
            if (pathXp == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TracePathXpModify(spell, target, pathXp, "unknown", false, "no-player-owner");
                return;
            }

            if (pathXp.Amount == 0u)
            {
                SpellEffectDiagnostics.TracePathXpModify(spell, target, pathXp, "unknown", false, "zero-amount");
                return;
            }

            switch (pathXp.Mode)
            {
                case 0u:
                    player.PathManager.AddXp(pathXp.Amount);
                    SpellEffectDiagnostics.TracePathXpModify(spell, target, pathXp, "add-xp", true, null);
                    break;
                case 1u:
                    player.PathManager.AddLevels(pathXp.Amount);
                    SpellEffectDiagnostics.TracePathXpModify(spell, target, pathXp, "add-levels", true, null);
                    break;
                default:
                    SpellEffectDiagnostics.TracePathXpModify(spell, target, pathXp, "unknown", false, "unknown-mode");
                    break;
            }
        }

        [SpellEffectHandler(SpellEffectType.GrantLevelScaledXP)]
        public static void HandleEffectGrantLevelScaledXp(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectGrantLevelScaledXpSemantics levelScaledXp = SpellEffectInterpreter.Interpret(info).GrantLevelScaledXp;
            if (levelScaledXp == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceGrantLevelScaledXp(spell, target, levelScaledXp, 0u, false, "no-player-owner");
                return;
            }

            if (levelScaledXp.Mode != 1u)
            {
                SpellEffectDiagnostics.TraceGrantLevelScaledXp(spell, target, levelScaledXp, 0u, false, "unknown-mode");
                return;
            }

            uint amount = CalculateLevelScaledXp(player, levelScaledXp, out string skippedReason);
            if (amount == 0u)
            {
                SpellEffectDiagnostics.TraceGrantLevelScaledXp(spell, target, levelScaledXp, 0u, false, skippedReason);
                return;
            }

            player.XpManager.GrantXp(amount, ExpReason.Spell);
            SpellEffectDiagnostics.TraceGrantLevelScaledXp(spell, target, levelScaledXp, amount, true, null);
        }

        [SpellEffectHandler(SpellEffectType.GiveAugmentPowerToPlayer)]
        public static void HandleEffectGiveAugmentPowerToPlayer(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectGiveAugmentPowerToPlayerSemantics augmentPower = SpellEffectInterpreter.Interpret(info).GiveAugmentPowerToPlayer;
            if (augmentPower == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceGiveAugmentPowerToPlayer(spell, target, augmentPower, false, "no-player-owner");
                return;
            }

            if (augmentPower.Amount == 0u || augmentPower.Amount > ushort.MaxValue)
            {
                SpellEffectDiagnostics.TraceGiveAugmentPowerToPlayer(spell, target, augmentPower, false, "invalid-amount");
                return;
            }

            player.SpellManager.AddAmpPower((ushort)augmentPower.Amount);
            SpellEffectDiagnostics.TraceGiveAugmentPowerToPlayer(spell, target, augmentPower, true, null);
        }

        [SpellEffectHandler(SpellEffectType.QuestAdvanceObjective)]
        public static void HandleEffectQuestAdvanceObjective(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectQuestAdvanceObjectiveSemantics questAdvance = SpellEffectInterpreter.Interpret(info).QuestAdvanceObjective;
            if (questAdvance == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceQuestAdvanceObjective(spell, target, questAdvance, 0u, false, "no-player-owner");
                return;
            }

            if (questAdvance.ObjectiveId == 0u)
            {
                SpellEffectDiagnostics.TraceQuestAdvanceObjective(spell, target, questAdvance, player.Guid, false, "missing-objective");
                return;
            }

            uint progress = questAdvance.Progress == 0u ? 1u : questAdvance.Progress;
            player.QuestManager.ObjectiveUpdate(questAdvance.ObjectiveId, progress);
            SpellEffectDiagnostics.TraceQuestAdvanceObjective(spell, target, questAdvance, player.Guid, true, null);
        }

        [SpellEffectHandler(SpellEffectType.AchievementAdvance)]
        public static void HandleEffectAchievementAdvance(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectAchievementAdvanceSemantics achievementAdvance = SpellEffectInterpreter.Interpret(info).AchievementAdvance;
            if (achievementAdvance == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceAchievementAdvance(spell, target, achievementAdvance, 0u, false, "no-player-owner");
                return;
            }

            if (achievementAdvance.AchievementId == 0u || achievementAdvance.AchievementId > ushort.MaxValue)
            {
                SpellEffectDiagnostics.TraceAchievementAdvance(spell, target, achievementAdvance, player.Guid, false, "invalid-achievement");
                return;
            }

            var achievementId = (ushort)achievementAdvance.AchievementId;
            if (GlobalAchievementManager.Instance.GetAchievement(achievementId) == null)
            {
                SpellEffectDiagnostics.TraceAchievementAdvance(spell, target, achievementAdvance, player.Guid, false, "unknown-achievement");
                return;
            }

            if (player.AchievementManager.HasCompletedAchievement(achievementId))
            {
                SpellEffectDiagnostics.TraceAchievementAdvance(spell, target, achievementAdvance, player.Guid, false, "already-complete");
                return;
            }

            player.AchievementManager.GrantAchievement(achievementId);
            SpellEffectDiagnostics.TraceAchievementAdvance(spell, target, achievementAdvance, player.Guid, true, null);
        }

        [SpellEffectHandler(SpellEffectType.ReputationModify)]
        public static void HandleEffectReputationModify(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectReputationModifySemantics reputationModify = SpellEffectInterpreter.Interpret(info).ReputationModify;
            if (reputationModify == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceReputationModify(spell, target, reputationModify, 0u, false, "no-player-owner");
                return;
            }

            if (reputationModify.FactionId == 0u || !float.IsFinite(reputationModify.Amount) || reputationModify.Amount == 0f)
            {
                SpellEffectDiagnostics.TraceReputationModify(spell, target, reputationModify, player.Guid, false, "invalid-faction-or-amount");
                return;
            }

            try
            {
                player.ReputationManager.UpdateReputation((Faction)reputationModify.FactionId, reputationModify.Amount);
            }
            catch (ArgumentException)
            {
                SpellEffectDiagnostics.TraceReputationModify(spell, target, reputationModify, player.Guid, false, "unknown-faction");
                return;
            }

            SpellEffectDiagnostics.TraceReputationModify(spell, target, reputationModify, player.Guid, true, null);
        }

        [SpellEffectHandler(SpellEffectType.GiveItemToPlayer)]
        public static void HandleEffectGiveItemToPlayer(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectGiveItemToPlayerSemantics giveItem = SpellEffectInterpreter.Interpret(info).GiveItemToPlayer;
            if (giveItem == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceGiveItemToPlayer(spell, target, giveItem, 0u, 0u, false, "no-player-owner");
                return;
            }

            if (giveItem.Item2Id == 0u || GameTableManager.Instance.Item.GetEntry(giveItem.Item2Id) == null)
            {
                SpellEffectDiagnostics.TraceGiveItemToPlayer(spell, target, giveItem, player.Guid, 0u, false, "unknown-item");
                return;
            }

            uint count = giveItem.Count == 0u ? 1u : giveItem.Count;
            player.Inventory.ItemCreate(InventoryLocation.Inventory, giveItem.Item2Id, count, ItemUpdateReason.SpellEffect);
            SpellEffectDiagnostics.TraceGiveItemToPlayer(spell, target, giveItem, player.Guid, count, true, null);
        }

        [SpellEffectHandler(SpellEffectType.GiveSchematic)]
        public static void HandleEffectGiveSchematic(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectGiveSchematicSemantics giveSchematic = SpellEffectInterpreter.Interpret(info).GiveSchematic;
            if (giveSchematic == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceGiveSchematic(spell, target, giveSchematic, 0u, 0u, false, "no-player-owner");
                return;
            }

            TradeskillSchematic2Entry schematicEntry = GameTableManager.Instance.TradeskillSchematic2.GetEntry(giveSchematic.TradeskillSchematic2Id);
            if (schematicEntry == null)
            {
                SpellEffectDiagnostics.TraceGiveSchematic(spell, target, giveSchematic, player.Guid, 0u, false, "unknown-schematic");
                return;
            }

            player.Session.EnqueueMessageEncrypted(new ServerSchematicAddLearned
            {
                TradeskillId            = (TradeskillType)schematicEntry.TradeSkillId,
                TradeskillSchematic2Id  = giveSchematic.TradeskillSchematic2Id,
                DiscoveryCoordinates    = new Vector2(schematicEntry.VectorX, schematicEntry.VectorY)
            });

            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ObtainSchematic, giveSchematic.TradeskillSchematic2Id, 1u);
            SpellEffectDiagnostics.TraceGiveSchematic(spell, target, giveSchematic, player.Guid, schematicEntry.TradeSkillId, true, "packet-only");
        }

        [SpellEffectHandler(SpellEffectType.RewardPropertyModifier)]
        public static void HandleEffectRewardPropertyModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectRewardPropertyModifierSemantics rewardProperty = SpellEffectInterpreter.Interpret(info).RewardPropertyModifier;
            if (rewardProperty == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceRewardPropertyModifier(spell, target, rewardProperty, 0u, 0f, false, false, "no-player-owner");
                return;
            }

            if (!TryResolveRewardPropertyModifier(rewardProperty, out RewardPropertyEntry entry, out float value, out string skippedReason))
            {
                SpellEffectDiagnostics.TraceRewardPropertyModifier(spell, target, rewardProperty, player.Guid, value, false, false, skippedReason);
                return;
            }

            player.Account.RewardPropertyManager.UpdateRewardProperty(entry, value, rewardProperty.Data);
            SpellEffectDiagnostics.TraceRewardPropertyModifier(spell, target, rewardProperty, player.Guid, value, true, false, null);
        }

        [SpellEffectHandler(SpellEffectType.ItemVisualSwap)]
        public static void HandleEffectItemVisualSwap(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectItemVisualSwapSemantics itemVisualSwap = SpellEffectInterpreter.Interpret(info).ItemVisualSwap;
            if (itemVisualSwap == null)
                return;

            if (itemVisualSwap.VisualSlot > ItemVisualSlotPacketMax)
            {
                SpellEffectDiagnostics.TraceItemVisualSwap(spell, target, itemVisualSwap, false, "invalid-visual-slot");
                return;
            }

            if (itemVisualSwap.DisplayId == 0u || itemVisualSwap.DisplayId > ItemVisualDisplayPacketMax)
            {
                SpellEffectDiagnostics.TraceItemVisualSwap(spell, target, itemVisualSwap, false, "invalid-display-id");
                return;
            }

            if (itemVisualSwap.ColourSetId > ItemVisualColourSetPacketMax)
            {
                SpellEffectDiagnostics.TraceItemVisualSwap(spell, target, itemVisualSwap, false, "invalid-colour-set-id");
                return;
            }

            var previousVisuals = new Dictionary<ItemSlot, IItemVisual>();
            ItemSlot slot = (ItemSlot)itemVisualSwap.VisualSlot;
            if (info.Entry.DurationTime > 0u)
                CapturePreviousVisual(target, previousVisuals, slot);

            target.AddVisual(
                slot,
                (ushort)itemVisualSwap.DisplayId,
                (ushort)itemVisualSwap.ColourSetId,
                unchecked((int)itemVisualSwap.DyeData));

            if (info.Entry.DurationTime > 0u)
                target.AddItemVisualSwap(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, previousVisuals);

            SpellEffectDiagnostics.TraceItemVisualSwap(spell, target, itemVisualSwap, true, null);
        }

        [SpellEffectHandler(SpellEffectType.DisguiseOutfit)]
        public static void HandleEffectDisguiseOutfit(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectDisguiseOutfitSemantics disguiseOutfit = SpellEffectInterpreter.Interpret(info).DisguiseOutfit;
            if (disguiseOutfit == null)
                return;

            ushort previousOutfitInfo = target.OutfitInfo;
            var previousVisuals = new Dictionary<ItemSlot, IItemVisual>();
            var skippedReasons = new List<string>();

            bool appliedOutfit = TryApplyDisguiseOutfitInfo(target, disguiseOutfit, skippedReasons);
            bool appliedPrimary = TryApplyDisguiseOutfitVisual(target, previousVisuals, ItemSlot.WeaponPrimary, disguiseOutfit.PrimaryItemDisplayId, skippedReasons, "primary-display");
            bool appliedSecondary = false;

            // DataBits02 is an item display for mech/exo suit rows; low values are flags in a few rows.
            if (disguiseOutfit.SecondaryItemDisplayId > 1u)
                appliedSecondary = TryApplyDisguiseOutfitVisual(target, previousVisuals, ItemSlot.EngineerMechSuit, disguiseOutfit.SecondaryItemDisplayId, skippedReasons, "secondary-display");

            bool applied = appliedOutfit || appliedPrimary || appliedSecondary;
            if (applied && info.Entry.DurationTime > 0u)
                target.AddDisguiseOutfit(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, previousOutfitInfo, previousVisuals);

            string skippedReason = skippedReasons.Count == 0
                ? (applied ? null : "missing-outfit-or-display")
                : string.Join("|", skippedReasons);
            SpellEffectDiagnostics.TraceDisguiseOutfit(spell, target, disguiseOutfit, previousOutfitInfo, appliedOutfit, appliedPrimary, appliedSecondary, false, skippedReason);
        }

        [SpellEffectHandler(SpellEffectType.MimicDisguise)]
        public static void HandleEffectMimicDisguise(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectMimicDisguiseSemantics mimicDisguise = SpellEffectInterpreter.Interpret(info).MimicDisguise;
            if (mimicDisguise == null)
                return;

            IUnitEntity source = spell.Caster;
            if (source == null || source.Guid == target.Guid)
            {
                SpellEffectDiagnostics.TraceMimicDisguise(spell, target, mimicDisguise, 0u, 0u, 0, 0u, 0, false, false, false, false, "invalid-source");
                return;
            }

            uint previousDisplayInfo = target.DisplayInfo;
            ushort previousOutfitInfo = target.OutfitInfo;
            uint sourceDisplayInfo = source.DisplayInfo;
            ushort sourceOutfitInfo = source.OutfitInfo;

            var previousVisuals = new Dictionary<ItemSlot, IItemVisual>();
            IItemVisual[] sourceVisuals = source.GetVisuals()
                .Where(v => v.DisplayId.HasValue)
                .Select(CloneVisual)
                .ToArray();
            ItemSlot[] targetVisualSlots = target.GetVisuals()
                .Select(v => v.Slot)
                .ToArray();

            foreach (ItemSlot slot in targetVisualSlots.Concat(sourceVisuals.Select(v => v.Slot)).Distinct())
                CapturePreviousVisual(target, previousVisuals, slot);

            bool appliedDisplay = false;
            if (sourceDisplayInfo != 0u)
            {
                target.DisplayInfo = sourceDisplayInfo;
                appliedDisplay = target.DisplayInfo == sourceDisplayInfo;
            }

            target.OutfitInfo = sourceOutfitInfo;
            bool appliedOutfit = target.OutfitInfo == sourceOutfitInfo;

            var sourceSlots = sourceVisuals.Select(v => v.Slot).ToHashSet();
            foreach (ItemSlot slot in targetVisualSlots.Where(slot => !sourceSlots.Contains(slot)))
                target.RemoveVisual(slot);

            foreach (IItemVisual sourceVisual in sourceVisuals)
                target.AddVisual(sourceVisual);

            bool appliedVisuals = previousVisuals.Count > 0 || sourceVisuals.Length > 0;
            bool applied = appliedDisplay || appliedOutfit || appliedVisuals;
            if (applied && info.Entry.DurationTime > 0u)
                target.AddMimicDisguise(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId, previousDisplayInfo, previousOutfitInfo, previousVisuals);

            SpellEffectDiagnostics.TraceMimicDisguise(
                spell,
                target,
                mimicDisguise,
                source.Guid,
                previousDisplayInfo,
                previousOutfitInfo,
                sourceDisplayInfo,
                sourceOutfitInfo,
                appliedDisplay,
                appliedOutfit,
                appliedVisuals,
                false,
                applied ? null : "no-appearance-data");
        }

        private static bool TryApplyDisguiseOutfitInfo(IUnitEntity target, SpellEffectDisguiseOutfitSemantics disguiseOutfit, ICollection<string> skippedReasons)
        {
            if (disguiseOutfit.OutfitInfoId <= 1u)
                return false;

            if (disguiseOutfit.OutfitInfoId > OutfitInfoPacketMax)
            {
                skippedReasons.Add("invalid-outfit-info-id");
                return false;
            }

            Creature2OutfitInfoEntry outfitEntry = GameTableManager.Instance.Creature2OutfitInfo.GetEntry(disguiseOutfit.OutfitInfoId);
            if (outfitEntry == null)
            {
                skippedReasons.Add("unknown-outfit-info");
                return false;
            }

            target.OutfitInfo = (ushort)disguiseOutfit.OutfitInfoId;
            return true;
        }

        private static bool TryApplyDisguiseOutfitVisual(
            IUnitEntity target,
            IDictionary<ItemSlot, IItemVisual> previousVisuals,
            ItemSlot slot,
            uint itemDisplayId,
            ICollection<string> skippedReasons,
            string label)
        {
            if (itemDisplayId == 0u)
                return false;

            if (itemDisplayId > ItemVisualDisplayPacketMax)
            {
                skippedReasons.Add($"invalid-{label}");
                return false;
            }

            if (GameTableManager.Instance.ItemDisplay.GetEntry(itemDisplayId) == null)
            {
                skippedReasons.Add($"unknown-{label}");
                return false;
            }

            CapturePreviousVisual(target, previousVisuals, slot);
            target.AddVisual(slot, (ushort)itemDisplayId);
            return true;
        }

        private static void CapturePreviousVisual(IUnitEntity target, IDictionary<ItemSlot, IItemVisual> previousVisuals, ItemSlot slot)
        {
            if (previousVisuals.ContainsKey(slot))
                return;

            IItemVisual previousVisual = target.GetVisuals().FirstOrDefault(v => v.Slot == slot);
            previousVisuals[slot] = previousVisual == null
                ? null
                : new ItemVisual
                {
                    Slot        = previousVisual.Slot,
                    DisplayId   = previousVisual.DisplayId,
                    ColourSetId = previousVisual.ColourSetId,
                    DyeData     = previousVisual.DyeData
                };
        }

        private static IItemVisual CloneVisual(IItemVisual visual)
        {
            return new ItemVisual
            {
                Slot        = visual.Slot,
                DisplayId   = visual.DisplayId,
                ColourSetId = visual.ColourSetId,
                DyeData     = visual.DyeData
            };
        }

        [SpellEffectHandler(SpellEffectType.PersonalDmgHealMod)]
        public static void HandleEffectPersonalDmgHealMod(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectPersonalDmgHealModSemantics personalMod = SpellEffectInterpreter.Interpret(info).PersonalDmgHealMod;
            if (personalMod == null)
                return;

            if (!TryResolvePersonalDmgHealModProperty(personalMod, out Property property, out string skippedReason))
            {
                SpellEffectDiagnostics.TracePersonalDmgHealMod(spell, target, personalMod, null, false, skippedReason);
                return;
            }

            if (!float.IsFinite(personalMod.Multiplier) || personalMod.Multiplier <= 0f)
            {
                SpellEffectDiagnostics.TracePersonalDmgHealMod(spell, target, personalMod, property, false, "invalid-multiplier");
                return;
            }

            var modifier = new SpellPropertyModifier(
                property,
                personalMod.Priority,
                personalMod.Multiplier,
                0f,
                0f);
            target.AddSpellModifierProperty(modifier, info.EffectId, spell.Parameters.SpellInfo.Entry.Id, info.Entry.Id, spell.CastingId);

            SpellEffectDiagnostics.TracePersonalDmgHealMod(spell, target, personalMod, property, true, null);
        }

        [SpellEffectHandler(SpellEffectType.UnitPropertyModifier)]
        public static void HandleEffectPropertyModifier(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectUnitPropertyModifierSemantics propertyModifier = SpellEffectInterpreter.Interpret(info).UnitPropertyModifier;
            if (propertyModifier == null)
                return;

            SpellPropertyModifier modifier =
                new SpellPropertyModifier(propertyModifier.Property,
                    propertyModifier.Priority,
                    propertyModifier.PercentageValue,
                    propertyModifier.FlatValue,
                    propertyModifier.LevelScaleValue);
            target.AddSpellModifierProperty(modifier, info.EffectId, spell.Parameters.SpellInfo.Entry.Id, info.Entry.Id, spell.CastingId);

            // Timed removal is scheduled centrally by Spell after the handler succeeds.
        }

        private static void ApplyScale(IUnitEntity target, float previousScale, float targetScale, uint transitionTimeMs)
        {
            if (transitionTimeMs > 0u && MathF.Abs(previousScale - targetScale) > 0.0001f)
            {
                target.MovementManager.SetScaleKeys(
                    new List<uint> { 0u, transitionTimeMs },
                    new List<float> { previousScale, targetScale });
                return;
            }

            target.MovementManager.SetScale(targetScale);
        }

        private static float ResolveTransferenceRate(SpellEffectTransferenceSemantics transference)
        {
            if (!float.IsFinite(transference.TransferRate) || transference.TransferRate <= 0f)
                return 1f;

            return transference.TransferRate;
        }

        private static uint CalculateTransferenceHealAmount(uint damageAmount, float transferRate)
        {
            if (damageAmount == 0u || !float.IsFinite(transferRate) || transferRate <= 0f)
                return 0u;

            double amount = Math.Ceiling(damageAmount * (double)transferRate);
            if (amount >= uint.MaxValue)
                return uint.MaxValue;

            return (uint)amount;
        }

        private static uint ResolveDispelCount(SpellEffectDispelSemantics dispel)
        {
            if (dispel.CountA == uint.MaxValue || dispel.CountB == uint.MaxValue)
                return uint.MaxValue;

            uint count = Math.Max(dispel.CountA, dispel.CountB);
            return count == 0u ? 1u : count;
        }

        private static bool MatchesDispelClass(uint spell4Id, SpellEffectDispelSemantics dispel)
        {
            if (!TryGetSpellBaseInfo(spell4Id, out ISpellBaseInfo spellBaseInfo))
                return false;

            if (dispel.SpellClass != 0u && Enum.IsDefined(typeof(SpellClass), (int)dispel.SpellClass))
                return spellBaseInfo.SpellClass == (SpellClass)dispel.SpellClass;

            return spellBaseInfo.IsDispellable;
        }

        private static bool TryCreateForceRemovePredicate(
            SpellEffectForceRemoveSemantics forceRemove,
            out Func<uint, bool> predicate,
            out string removeScope,
            out string skippedReason)
        {
            predicate      = null;
            removeScope   = null;
            skippedReason = null;

            if (forceRemove.Spell4Id == 0u)
            {
                skippedReason = "missing-remove-target";
                return false;
            }

            switch (forceRemove.RemoveType)
            {
                case 3u:
                    removeScope = "spell4-base";
                    predicate = spell4Id =>
                    {
                        if (!TryGetSpellBaseInfo(spell4Id, out ISpellBaseInfo spellBaseInfo))
                            return false;

                        return spellBaseInfo.Entry.Id == forceRemove.Spell4Id;
                    };
                    return true;
                case 2u:
                    removeScope = "spell4";
                    predicate = spell4Id => spell4Id == forceRemove.Spell4Id;
                    return true;
                default:
                    removeScope = "spell4";
                    predicate = spell4Id => spell4Id == forceRemove.Spell4Id;
                    return true;
            }
        }

        private static bool TryLearnCollectionSpell(IPlayer player, uint spell4Id, out uint spell4BaseId, out string skippedReason)
        {
            spell4BaseId  = 0u;
            skippedReason = null;

            if (spell4Id == 0u)
            {
                skippedReason = "missing-spell4";
                return false;
            }

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
            {
                skippedReason = "unknown-spell4";
                return false;
            }

            spell4BaseId = spell4Entry.Spell4BaseIdBaseSpell;
            if (spell4BaseId == 0u)
            {
                skippedReason = "missing-base-spell";
                return false;
            }

            if (player.SpellManager.GetSpell(spell4BaseId) != null)
            {
                skippedReason = "already-known";
                return false;
            }

            try
            {
                player.SpellManager.AddSpell(spell4BaseId);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                skippedReason = "invalid-base-spell";
                return false;
            }
        }

        private static IPlayer GetPlayerSpellOwner(ISpell spell, IUnitEntity target)
        {
            return target as IPlayer ?? spell.Caster as IPlayer;
        }

        private static uint CalculateLevelScaledXp(IPlayer player, SpellEffectGrantLevelScaledXpSemantics levelScaledXp, out string skippedReason)
        {
            skippedReason = null;

            if (!float.IsFinite(levelScaledXp.PercentOfLevel) || levelScaledXp.PercentOfLevel <= 0f)
            {
                skippedReason = "invalid-percent";
                return 0u;
            }

            uint maxLevel = levelScaledXp.MaxLevel == 0u ? 50u : levelScaledXp.MaxLevel;
            if (player.Level >= maxLevel)
            {
                skippedReason = "at-or-above-level-cap";
                return 0u;
            }

            XpPerLevelEntry currentLevel = GameTableManager.Instance.XpPerLevel.GetEntry(player.Level);
            XpPerLevelEntry nextLevel = GameTableManager.Instance.XpPerLevel.GetEntry(player.Level + 1u);
            if (currentLevel == null || nextLevel == null)
            {
                skippedReason = "missing-xp-level-entry";
                return 0u;
            }

            if (nextLevel.MinXpForLevel <= currentLevel.MinXpForLevel)
            {
                skippedReason = "invalid-xp-level-span";
                return 0u;
            }

            float levelSpan = nextLevel.MinXpForLevel - currentLevel.MinXpForLevel;
            float resolvedAmount = levelSpan * (levelScaledXp.PercentOfLevel / 100f);
            if (!float.IsFinite(resolvedAmount) || resolvedAmount <= 0f)
            {
                skippedReason = "invalid-resolved-xp";
                return 0u;
            }

            return (uint)MathF.Ceiling(resolvedAmount);
        }

        internal static bool TryResolvePersonalDmgHealModProperty(
            SpellEffectPersonalDmgHealModSemantics personalMod,
            out Property property,
            out string skippedReason)
        {
            skippedReason = null;

            switch (personalMod.ModifierType)
            {
                case 3:
                    property = Property.DamageDealtMultiplierPhysical;
                    return true;
                case 4:
                    property = Property.DamageDealtMultiplierTech;
                    return true;
                case 5:
                    property = Property.DamageDealtMultiplierMagic;
                    return true;
                case 6:
                    property = Property.DamageTakenMultiplierPhysical;
                    return true;
                case 7:
                    property = Property.DamageTakenMultiplierTech;
                    return true;
                case 8:
                    property = Property.DamageTakenMultiplierMagic;
                    return true;
                case 12:
                    property = Property.HealingMultiplierIncoming;
                    return true;
                case 13:
                    property = Property.HealingMultiplierOutgoing;
                    return true;
                default:
                    property = default;
                    skippedReason = "evidence-gap-modifier-type";
                    return false;
            }
        }

        internal static bool TryResolveRewardPropertyModifier(
            SpellEffectRewardPropertyModifierSemantics rewardProperty,
            out RewardPropertyEntry entry,
            out float value,
            out string skippedReason)
        {
            entry         = null;
            value         = 0f;
            skippedReason = null;

            if (rewardProperty.RewardPropertyId == 0u)
            {
                skippedReason = "missing-reward-property";
                return false;
            }

            entry = GameTableManager.Instance.RewardProperty.GetEntry(rewardProperty.RewardPropertyId);
            if (entry == null)
            {
                skippedReason = "unknown-reward-property";
                return false;
            }

            if (float.IsFinite(rewardProperty.ValueFloat02) && rewardProperty.ValueFloat02 != 0f)
                value = rewardProperty.ValueFloat02;
            else if (float.IsFinite(rewardProperty.ValueFloat03) && rewardProperty.ValueFloat03 != 0f)
                value = rewardProperty.ValueFloat03;
            else
            {
                skippedReason = "zero-or-invalid-value";
                return false;
            }

            return true;
        }

        private static uint ResolveSpell4Id(params uint[] candidates)
        {
            foreach (uint candidate in candidates)
            {
                if (candidate <= 1000u)
                    continue;

                if (GameTableManager.Instance.Spell4.GetEntry(candidate) != null)
                    return candidate;
            }

            return 0u;
        }

        private static bool TryResolveCooldownMutation(SpellEffectModifySpellCooldownSemantics cooldown, double beforeCooldown, out double afterCooldown, out string action, out string skippedReason)
        {
            afterCooldown = beforeCooldown;
            action = null;
            skippedReason = null;

            if (float.IsFinite(cooldown.DataFloat03) && cooldown.DataFloat03 <= -1f)
            {
                afterCooldown = Math.Max(0d, beforeCooldown - Math.Abs(cooldown.DataFloat03) / 1000d);
                action = "reduce-float-ms";
                return true;
            }

            int signedDataBits05 = unchecked((int)cooldown.DataBits05);
            if (signedDataBits05 < 0)
            {
                afterCooldown = Math.Max(0d, beforeCooldown - Math.Abs(signedDataBits05) / 1000d);
                action = "reduce-int-ms";
                return true;
            }

            if (float.IsFinite(cooldown.DataFloat03) && cooldown.DataFloat03 >= 1f)
            {
                afterCooldown = cooldown.DataFloat03 / 1000d;
                action = "set-float-ms";
                return true;
            }

            if (cooldown.DataFloat03 == 0f && cooldown.Operation == 0u)
            {
                afterCooldown = 0d;
                action = "reset";
                return true;
            }

            skippedReason = "evidence-gap-cooldown-mode";
            return false;
        }

        private static void SendTrackedStateRemovalMessages(
            ISpell spell,
            IUnitEntity target,
            ISpellTargetEffectInfo info,
            IReadOnlyCollection<SpellStateRemoval> removals,
            bool addDispelCombatLogs)
        {
            if (removals.Count == 0)
                return;

            foreach (IGrouping<uint, SpellStateRemoval> group in removals.GroupBy(r => r.Spell4Id))
            {
                if (addDispelCombatLogs)
                {
                    info.AddCombatLog(new CombatLogDispel
                    {
                        BRemovesSingleInstance = group.Count() == 1,
                        InstancesRemoved       = (uint)group.Count(),
                        SpellRemovedId         = group.Key
                    });
                }

                SpellStateRemoval removal = group.FirstOrDefault(r => r.CastingId != 0u) ?? group.First();
                if (group.Any(r => r.Kind != SpellStateRemovalKind.CrowdControl))
                    SendSpellBuffRemove(target, group.Key, removal.CastingId == 0u ? spell.CastingId : removal.CastingId);
            }

            foreach (SpellStateRemoval removal in removals.Where(r => r.Kind == SpellStateRemovalKind.CrowdControl && r.CCState.HasValue))
                SendCCStateRemove(target, removal.CCState.Value, removal.EffectId, removal.CastingId == 0u ? spell.CastingId : removal.CastingId);

            if (removals.Any(r => r.Kind == SpellStateRemovalKind.Stealth) && !target.IsStealthed)
            {
                target.CreateFlags &= ~EntityCreateFlag.IsStealthed;
                info.AddCombatLog(new CombatLogStealth
                {
                    UnitId   = target.Guid,
                    BExiting = true
                });
            }
        }

        private static void SendCCStateRemove(IUnitEntity target, CCState state, uint effectId, uint castingId)
        {
            target.EnqueueToVisible(new ServerEntityCCStateRemove
            {
                UnitId              = target.Guid,
                CCType              = state,
                SpellCastUniqueId   = castingId,
                SpellEffectUniqueId = effectId,
                Removed             = true
            }, true);
        }

        private static void SendSpellBuffRemove(IUnitEntity target, uint spell4Id, uint castingId)
        {
            if (!TryGetSpellBaseInfo(spell4Id, out ISpellBaseInfo spellBaseInfo) || !spellBaseInfo.HasIcon)
                return;

            target.EnqueueToVisible(new ServerSpellBuffRemove
            {
                CastingId = castingId,
                CasterId  = target.Guid
            }, true);
        }

        private static bool TryGetSpellBaseInfo(uint spell4Id, out ISpellBaseInfo spellBaseInfo)
        {
            spellBaseInfo = null;

            Spell4Entry spell4Entry = GameTableManager.Instance.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
                return false;

            try
            {
                spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4Entry.Spell4BaseIdBaseSpell);
                return spellBaseInfo != null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
