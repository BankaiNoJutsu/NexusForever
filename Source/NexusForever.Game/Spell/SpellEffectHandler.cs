using System.Numerics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.Force;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Combat;
using NexusForever.Game.Combat.CrowdControl;
using NexusForever.Game.Entity;
using NexusForever.Game;
using NexusForever.Game.Housing;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Lock;
using NexusForever.Game.Map.Search;
using NexusForever.Game.Spell.Effect;
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
    public static partial class SpellHandler
    {
        private const uint RelentlessStrikesTelegraphSpell4Id = 70033u;
        private const uint RelentlessStrikesAddCellSpell4Id = 53865u;
        private const uint VitalModifierSentinel = 2147483647u;
        private const uint VitalModifierMaxConservativeFlatAmount = 100000000u;
        private const uint ItemVisualSlotPacketMax = 0x7Fu;
        private const uint ItemVisualDisplayPacketMax = 0x7FFFu;
        private const uint ItemVisualColourSetPacketMax = 0x3FFFu;
        private const uint ActionBarShortcutSetPacketMax = 0x3FFFu;
        private const uint OutfitInfoPacketMax = 0x7FFFu;
        private const uint StarterTutorialScanSpellId = 81662u;
        private const uint EngineerArtillerybotExileCreatureId = 42683u;
        private const uint EngineerArtillerybotDominionCreatureId = 59846u;
        private const uint EngineerArtillerybotMaxActive = 2u;
        private const uint EngineerArtillerybotSummonBaseSpell4Id = 27002u;
        private const uint EngineerArtillerybotPetSwitchBaseSpell4Id = 34051u;
        private const uint EngineerArtillerybotPlayerBarrageBaseSpell4Id = 20884u;
        private const string GenericUnlockEntryTableName = "GenericUnlockEntry.tbl";
        private const string Spell4TableName = "Spell4.tbl";
        private const string PetFlairTableName = "PetFlair.tbl";
        private const string CharacterTitleTableName = "CharacterTitle.tbl";

        private static readonly uint[] EngineerArtillerybotCreatureIds =
        [
            EngineerArtillerybotExileCreatureId,
            EngineerArtillerybotDominionCreatureId
        ];

        private static ISpellEffectDependencyResolver dependencyResolver;

        internal static ISpellEffectDependencyResolver InitialiseDependencyResolver(ISpellEffectDependencyResolver resolver)
        {
            ISpellEffectDependencyResolver previous = dependencyResolver;
            dependencyResolver = resolver;
            return previous;
        }

        private static IDamageCalculator CreateDamageCalculator()
        {
            IDamageCalculator damageCalculator = dependencyResolver?.CreateDamageCalculator();
            if (damageCalculator != null)
                return damageCalculator;

            throw new InvalidOperationException("Spell effect dependency resolver has not been initialised.");
        }

        private static IEntityFactory GetEntityFactory()
        {
            return dependencyResolver?.GetEntityFactory();
        }

        private static IForcedMovementGenerator GetForcedMovementGenerator()
        {
            return dependencyResolver?.GetForcedMovementGenerator();
        }

        private static IAssetManager GetAssetManager()
        {
            return dependencyResolver?.GetAssetManager();
        }

        private static IGlobalAchievementManager GetGlobalAchievementManager()
        {
            return dependencyResolver?.GetGlobalAchievementManager();
        }

        private static IGlobalResidenceManager GetGlobalResidenceManager()
        {
            return dependencyResolver?.GetGlobalResidenceManager()
                ?? throw new InvalidOperationException("Spell effect dependency resolver has not been initialised.");
        }

        private static IGlobalLootManager GetGlobalLootManager()
        {
            return dependencyResolver?.GetGlobalLootManager();
        }

        private static IMapLockManager GetMapLockManager()
        {
            return dependencyResolver?.GetMapLockManager()
                ?? throw new InvalidOperationException("Spell effect dependency resolver has not been initialised.");
        }

        private static IGlobalSpellManager GetGlobalSpellManager()
        {
            return dependencyResolver?.GetGlobalSpellManager()
                ?? throw new InvalidOperationException("Spell effect dependency resolver has not been initialised.");
        }

        private static IGameTableManager GetGameTableManager()
        {
            return dependencyResolver?.GetGameTableManager()
                ?? throw new InvalidOperationException("Spell effect dependency resolver has not been initialised.");
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

            if (!TryResolveVitalModifierAmount(spell.Caster, target, interpretation, vitalModifier, out float amount, out string mode, out string skippedReason))
            {
                SpellEffectDiagnostics.TraceVitalModifier(spell, target, vitalModifier, mode, amount, 0f, false, skippedReason);
                return;
            }

            bool applied = target.TryModifyVital(vitalModifier.Vital, amount, out float appliedAmount, spell.Caster);
            SpellEffectDiagnostics.TraceVitalModifier(spell, target, vitalModifier, mode, amount, appliedAmount, applied, applied ? string.Empty : "unknown-vital");
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
            IUnitEntity caster,
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

            float resolvedAmount = 0f;
            List<string> modes = [];
            if (TryResolveSpellEffectParameterAmount(caster, target, interpretation.Parameters, out float parameterAmount))
            {
                resolvedAmount += parameterAmount;
                modes.Add("parameter");
            }

            if (TryResolveSignedRangeAmount(vitalModifier.DataBits01, vitalModifier.DataBits02, out float primaryRangeAmount))
            {
                resolvedAmount += primaryRangeAmount;
                modes.Add(vitalModifier.DataBits01 == vitalModifier.DataBits02 ? "flat" : "range");
            }

            if (TryResolveSignedRangeAmount(vitalModifier.DataBits03, vitalModifier.DataBits04, out float secondaryRangeAmount))
            {
                resolvedAmount += secondaryRangeAmount;
                modes.Add(vitalModifier.DataBits03 == vitalModifier.DataBits04 ? "secondary-flat" : "secondary-range");
            }

            if (TryResolvePercentVitalModifierAmount(target, vitalModifier, out float percentAmount, out skippedReason))
            {
                resolvedAmount += percentAmount;
                modes.Add("percent-max");
            }
            else if (!string.IsNullOrEmpty(skippedReason) && modes.Count == 0)
            {
                return false;
            }

            if (!float.IsFinite(resolvedAmount) || MathF.Abs(resolvedAmount) < 0.0001f)
            {
                skippedReason = "zero-amount";
                return false;
            }

            amount = resolvedAmount;
            mode   = string.Join("+", modes);
            return true;
        }

        private static bool TryResolveSignedRangeAmount(uint rawMin, uint rawMax, out float amount)
        {
            amount = 0f;
            if (rawMin == 0u && rawMax == 0u)
                return false;

            int signedMin = unchecked((int)rawMin);
            int signedMax = unchecked((int)rawMax);
            int low = Math.Min(signedMin, signedMax);
            int high = Math.Max(signedMin, signedMax);

            amount = low == high
                ? low
                : (float)Random.Shared.NextInt64(low, (long)high + 1L);
            return true;
        }

        private static bool TryResolvePercentVitalModifierAmount(IUnitEntity target, SpellEffectVitalModifierSemantics vitalModifier, out float amount, out string skippedReason)
        {
            amount        = 0f;
            skippedReason = string.Empty;

            if (!float.IsFinite(vitalModifier.DataFloat05) || vitalModifier.DataFloat05 <= 0f)
            {
                return false;
            }

            if (!target.TryGetVitalMax(vitalModifier.Vital, out float maxValue))
            {
                skippedReason = "unknown-vital-max";
                return false;
            }

            float fraction = vitalModifier.DataFloat05 <= 1f
                ? vitalModifier.DataFloat05
                : vitalModifier.DataFloat05 / 100f;

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

            if (!TryResolveSapVitalAmount(spell.Caster, target, interpretation, sapVital, out float amount, out string mode, out string amountSource, out string skippedReason))
            {
                SpellEffectDiagnostics.TraceSapVital(spell, target, sapVital, mode, amountSource, amount, 0f, false, skippedReason);
                return;
            }

            bool applied = target.TryModifyVital(sapVital.Vital, amount, out float appliedAmount, spell.Caster, info.Entry.DamageType);
            SpellEffectDiagnostics.TraceSapVital(spell, target, sapVital, mode, amountSource, amount, appliedAmount, applied, applied ? string.Empty : "unknown-vital");
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
            IUnitEntity caster,
            IUnitEntity target,
            SpellEffectInterpretation interpretation,
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

            TryResolveSpellEffectParameterAmount(caster, target, interpretation.Parameters, out float parameterAmount);
            if (!TrySelectSapVitalScalar(sapVital, parameterAmount, out float scalar, out amountSource, out skippedReason))
                return false;

            float magnitude = MathF.Abs(scalar);
            bool flatAmount = amountSource is "parameter" ||
                amountSource == "dataFloat01" && magnitude > 1f ||
                amountSource == "dataFloat02" && magnitude > 100f;
            float resolvedAmount;
            if (flatAmount)
            {
                resolvedAmount = magnitude;
                mode = "flat";
            }
            else
            {
                if (!target.TryGetVitalMax(sapVital.Vital, out float maxValue))
                {
                    skippedReason = "unknown-vital-max";
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

                resolvedAmount = maxValue * fraction;
                mode = "percent-max";
            }

            if (!float.IsFinite(resolvedAmount) || resolvedAmount <= 0f)
            {
                skippedReason = "invalid-amount";
                return false;
            }

            if (sapVital.Mode == 1u && scalar > 0f)
            {
                amount = resolvedAmount;
                mode   = $"restore-{mode}";
            }
            else
            {
                amount = -resolvedAmount;
                mode   = sapVital.Mode == 1u ? $"signed-drain-{mode}" : $"drain-{mode}";
            }

            return true;
        }

        private static bool TrySelectSapVitalScalar(SpellEffectSapVitalSemantics sapVital, float parameterAmount, out float scalar, out string amountSource, out string skippedReason)
        {
            scalar        = 0f;
            amountSource  = "none";
            skippedReason = string.Empty;

            if (float.IsFinite(parameterAmount) && MathF.Abs(parameterAmount) >= 0.0001f)
            {
                scalar       = parameterAmount;
                amountSource = "parameter";
                return true;
            }

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

        private static bool TryResolveSpellEffectParameterAmount(IUnitEntity caster, IUnitEntity target, IReadOnlyList<SpellEffectParameter> parameters, out float amount)
        {
            amount = 0f;
            if (parameters == null || parameters.All(p => p.Type == SpellEffectParameterType.None))
                return false;

            GameFormulaEntry formulaEntry = GetGameTableManager().GameFormula.GetEntry(1266);
            foreach (SpellEffectParameter parameter in parameters.Where(p => p.Type != SpellEffectParameterType.None))
            {
                float intermediateValue = parameter.Type switch
                {
                    SpellEffectParameterType.Brutality               => caster.GetPropertyValue(Property.Strength),
                    SpellEffectParameterType.Finesse                 => caster.GetPropertyValue(Property.Dexterity),
                    SpellEffectParameterType.Tech                    => caster.GetPropertyValue(Property.Technology),
                    SpellEffectParameterType.Moxie                   => caster.GetPropertyValue(Property.Magic),
                    SpellEffectParameterType.Insight                 => caster.GetPropertyValue(Property.Wisdom),
                    SpellEffectParameterType.Grit                    => caster.GetPropertyValue(Property.Stamina),
                    SpellEffectParameterType.AssaultPower            => caster.GetPropertyValue(Property.AssaultRating) * (formulaEntry?.Datafloat0 ?? 0.25f),
                    SpellEffectParameterType.SupportPower            => caster.GetPropertyValue(Property.SupportRating) * (formulaEntry?.Datafloat01 ?? 0.25f),
                    SpellEffectParameterType.TargetMaxHealth         => target.MaxHealth,
                    SpellEffectParameterType.CasterMaxHealth         => caster.MaxHealth,
                    SpellEffectParameterType.CasterShieldCapacity    => caster.Shield,
                    SpellEffectParameterType.TargetShieldCapacity    => target.Shield,
                    SpellEffectParameterType.CasterMaxShieldCapacity => caster.MaxShieldCapacity,
                    SpellEffectParameterType.TargetMaxShieldCapacity => target.MaxShieldCapacity,
                    SpellEffectParameterType.ItemBudget              => parameter.Value,
                    SpellEffectParameterType.TargetCurrentHealth     => target.Health,
                    SpellEffectParameterType.TargetMissingHealth     => target.MaxHealth - target.Health,
                    SpellEffectParameterType.TargetMissingShields    => target.MaxShieldCapacity - target.Shield,
                    SpellEffectParameterType.CasterCurrentHealth     => caster.Health,
                    SpellEffectParameterType.CasterMissingHealth     => caster.MaxHealth - caster.Health,
                    SpellEffectParameterType.CasterMissingShields    => caster.MaxShieldCapacity - caster.Shield,
                    SpellEffectParameterType.PerLevel                => caster.Level,
                    SpellEffectParameterType.Weapon                  => parameter.Value,
                    SpellEffectParameterType.WeaponDPS               => parameter.Value,
                    _                                                => 0f
                };

                amount += intermediateValue * parameter.Value;
            }

            if (!float.IsFinite(amount) || MathF.Abs(amount) < 0.0001f)
                return false;

            amount = amount >= 0f ? MathF.Ceiling(amount) : MathF.Floor(amount);
            return true;
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

            if (GetGameTableManager().Creature2.GetEntry(summonCreature.CreatureId) == null)
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

        [SpellEffectHandler(SpellEffectType.SummonPet)]
        public static void HandleEffectSummonPet(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectSummonPetSemantics summonPet = SpellEffectInterpreter.Interpret(info).SummonPet;
            if (summonPet == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            Vector3 position = ResolveSummonPetPosition(spell, target, player);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceSummonPet(spell, target, summonPet, position, 0u, false, 0u, "no-player-owner");
                return;
            }

            if (summonPet.CreatureId == 0u)
            {
                SpellEffectDiagnostics.TraceSummonPet(spell, target, summonPet, position, player.Guid, false, 0u, "missing-creature-id");
                return;
            }

            Creature2Entry creatureEntry = GetGameTableManager().Creature2?.GetEntry(summonPet.CreatureId);
            if (creatureEntry == null)
            {
                SpellEffectDiagnostics.TraceSummonPet(spell, target, summonPet, position, player.Guid, false, 0u, "unknown-creature-id");
                return;
            }

            if (player.SummonFactory == null)
            {
                SpellEffectDiagnostics.TraceSummonPet(spell, target, summonPet, position, player.Guid, false, 0u, "missing-summon-factory");
                return;
            }

            var map = player.Map ?? target.Map ?? spell.Caster.Map;
            if (map == null)
            {
                SpellEffectDiagnostics.TraceSummonPet(spell, target, summonPet, position, player.Guid, false, 0u, "target-not-in-world");
                return;
            }

            if (IsSummonPetActiveCapReached(player.SummonFactory, summonPet))
            {
                SpellEffectDiagnostics.TraceSummonPet(spell, target, summonPet, position, player.Guid, false, 0u, "active-cap-reached");
                return;
            }

            IEntityFactory factory = GetEntityFactory();
            if (factory == null)
            {
                SpellEffectDiagnostics.TraceSummonPet(spell, target, summonPet, position, player.Guid, false, 0u, "missing-entity-factory");
                return;
            }

            IWorldEntity summoned = factory.CreateWorldEntity((EntityType)creatureEntry.CreationTypeEnum);
            if (summoned == null)
            {
                SpellEffectDiagnostics.TraceSummonPet(spell, target, summonPet, position, player.Guid, false, 0u, "unsupported-entity-type");
                return;
            }

            summoned.Initialise(summonPet.CreatureId);
            if (IsEngineerArtillerybotCreature(summonPet.CreatureId))
                ApplyEngineerArtillerybotSummonerLevel(summoned, player, creatureEntry);

            summoned.Rotation     = player.Rotation;
            summoned.SummonerGuid = player.Guid;
            summoned.Faction1     = player.Faction1;
            summoned.Faction2     = player.Faction2;

            var mapPosition = new MapPosition
            {
                Position = position
            };

            if (!map.CanEnter(summoned, mapPosition))
            {
                SpellEffectDiagnostics.TraceSummonPet(spell, target, summonPet, position, player.Guid, false, summoned.Guid, "map-rejected-position");
                return;
            }

            map.EnqueueAdd(summoned, mapPosition);
            info.AddCreatedEntity(summoned);
            if (IsEngineerArtillerybotCreature(summonPet.CreatureId))
                RegisterEngineerArtillerybotBarrageAction(player, spell.Parameters.SpellInfo?.Entry);

            SpellEffectDiagnostics.TraceSummonPet(spell, target, summonPet, position, player.Guid, true, summoned.Guid, null);
        }

        private static bool IsSummonPetActiveCapReached(IEntitySummonFactory summonFactory, SpellEffectSummonPetSemantics summonPet)
        {
            if (!IsEngineerArtillerybotCreature(summonPet.CreatureId))
                return false;

            uint activeCount = 0u;
            foreach (uint creatureId in EngineerArtillerybotCreatureIds)
                activeCount += summonFactory.GetSummonCreatureCount(creatureId);

            return activeCount >= EngineerArtillerybotMaxActive;
        }

        private static bool IsEngineerArtillerybotCreature(uint creatureId)
        {
            return creatureId is EngineerArtillerybotExileCreatureId or EngineerArtillerybotDominionCreatureId;
        }

        private static void ApplyEngineerArtillerybotSummonerLevel(IWorldEntity summoned, IPlayer player, Creature2Entry creatureEntry)
        {
            uint minLevel = creatureEntry.MinLevel == 0u ? 1u : creatureEntry.MinLevel;
            uint maxLevel = creatureEntry.MaxLevel >= minLevel ? creatureEntry.MaxLevel : minLevel;
            uint playerLevel = player.Level == 0u ? minLevel : player.Level;
            uint summonLevel = Math.Min(Math.Max(playerLevel, minLevel), maxLevel);

            summoned.Level = summonLevel;
            summoned.RecalculateCreatureProperties();
        }

        private static void RegisterEngineerArtillerybotBarrageAction(IPlayer player, Spell4Entry summonSpellEntry)
        {
            if (summonSpellEntry == null
                || summonSpellEntry.Spell4BaseIdBaseSpell != EngineerArtillerybotSummonBaseSpell4Id
                || summonSpellEntry.Spell4IdPetSwitch == 0u)
                return;

            Spell4Entry barrageEntry = ResolveSpell4Entry(
                EngineerArtillerybotPlayerBarrageBaseSpell4Id,
                summonSpellEntry.TierIndex);
            if (barrageEntry == null)
                return;

            player.SpellManager.SetActivePetActionSpell(summonSpellEntry.Spell4IdPetSwitch, barrageEntry.Id, summonSpellEntry.Id);
            ShowEngineerArtillerybotBarrageAction(player, summonSpellEntry);
        }

        public static void UnregisterEngineerArtillerybotBarrageAction(IPlayer player, IWorldEntity entity)
        {
            if (player == null || entity == null || !IsEngineerArtillerybotCreature(entity.CreatureId))
                return;

            uint activeCount = 0u;
            foreach (uint creatureId in EngineerArtillerybotCreatureIds)
                activeCount += player.SummonFactory?.GetSummonCreatureCount(creatureId) ?? 0u;

            if (activeCount != 0u)
                return;

            player.SpellManager.ClearActivePetActionSpells();
            HideEngineerArtillerybotBarrageAction(player);
        }

        private static void ShowEngineerArtillerybotBarrageAction(IPlayer player, Spell4Entry summonSpellEntry)
        {
            Spell4Entry petSwitchEntry = GetGameTableManager().Spell4?.GetEntry(summonSpellEntry.Spell4IdPetSwitch);
            if (petSwitchEntry == null || petSwitchEntry.Spell4BaseIdBaseSpell != EngineerArtillerybotPetSwitchBaseSpell4Id)
                return;

            SendEngineerArtillerybotBarrageSpellUpdate(
                player,
                petSwitchEntry.Spell4BaseIdBaseSpell,
                (byte)Math.Clamp(petSwitchEntry.TierIndex, 1u, (uint)byte.MaxValue),
                true);
            SendEngineerArtillerybotActionSetSwap(player, petSwitchEntry.Spell4BaseIdBaseSpell);
        }

        private static void HideEngineerArtillerybotBarrageAction(IPlayer player)
        {
            if (player.Session == null)
                return;

            SendEngineerArtillerybotBarrageSpellUpdate(
                player,
                EngineerArtillerybotPetSwitchBaseSpell4Id,
                0,
                false);

            IActionSet actionSet = player.SpellManager.GetActionSet(player.SpellManager.ActiveActionSet);
            ServerActionSet packet = actionSet?.BuildServerActionSet();
            if (packet != null)
                player.Session?.EnqueueMessageEncrypted(packet);
        }

        private static void SendEngineerArtillerybotBarrageSpellUpdate(IPlayer player, uint spell4BaseId, byte tierIndex, bool activated)
        {
            if (player.Session == null)
                return;

            player.Session.EnqueueMessageEncrypted(new ServerSpellUpdate
            {
                Spell4BaseId = spell4BaseId,
                TierIndex    = tierIndex,
                SpecIndex    = player.SpellManager.ActiveActionSet,
                Activated    = activated
            });
        }

        private static void SendEngineerArtillerybotActionSetSwap(IPlayer player, uint petSwitchSpell4BaseId)
        {
            if (player.Session == null)
                return;

            IActionSet actionSet = player.SpellManager.GetActionSet(player.SpellManager.ActiveActionSet);
            if (actionSet == null)
                return;

            var packet = new ServerActionSet
            {
                SpecIndex = actionSet.Index,
                Unlocked  = 1,
                Result    = LimitedActionSetResult.Ok
            };

            bool replaced = false;
            for (byte slot = 0; slot < ActionSet.MaxActionCount; slot++)
            {
                var location = (UILocation)slot;
                IActionSetShortcut shortcut = actionSet.GetShortcut(location);
                bool replace = shortcut?.ShortcutType == ShortcutType.SpellbookItem
                    && shortcut.ObjectId == EngineerArtillerybotSummonBaseSpell4Id;
                if (replace)
                    replaced = true;

                packet.Actions.Add(new ServerActionSet.Action
                {
                    ShortcutType = shortcut?.ShortcutType ?? ShortcutType.None,
                    ObjectId     = replace ? petSwitchSpell4BaseId : shortcut?.ObjectId ?? 0u,
                    Location     = new NexusForever.Network.World.Message.Model.Shared.ItemLocation
                    {
                        Location = shortcut == null ? (InventoryLocation)300 : InventoryLocation.Ability,
                        BagIndex = slot
                    }
                });
            }

            if (replaced)
                player.Session.EnqueueMessageEncrypted(packet);
        }

        private static Spell4Entry ResolveSpell4Entry(uint spell4BaseId, uint tierIndex)
        {
            if (spell4BaseId == 0u || tierIndex == 0u)
                return null;

            return GetGameTableManager().Spell4?.Entries?
                .FirstOrDefault(e => e?.Spell4BaseIdBaseSpell == spell4BaseId && e.TierIndex == tierIndex);
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

            Creature2Entry creatureEntry = GetGameTableManager().Creature2.GetEntry(summonVehicle.CreatureId);
            if (creatureEntry == null)
            {
                SpellEffectDiagnostics.TraceSummonVehicle(spell, target, summonVehicle, position, false, false, 0u, "unknown-creature-id");
                return;
            }

            uint unitVehicleId = summonVehicle.UnitVehicleId != 0u
                ? summonVehicle.UnitVehicleId
                : creatureEntry.UnitVehicleId;
            if (unitVehicleId == 0u || GetGameTableManager().UnitVehicle.GetEntry(unitVehicleId) == null)
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

            bool creatureExists = GetGameTableManager().Creature2.GetEntry(summonTrap.CreatureId) != null;
            bool triggerSpellExists = summonTrap.TriggerSpell4Id == 0u
                || GetGameTableManager().Spell4.GetEntry(summonTrap.TriggerSpell4Id) != null;
            SummonTrapEvidenceBoundarySnapshot boundary = SummonTrapEvidenceBoundary.Describe(
                summonTrap.CreatureId,
                summonTrap.TriggerSpell4Id,
                creatureExists,
                triggerSpellExists);
            if (!boundary.IsConservativelyCreateSupported)
            {
                SpellEffectDiagnostics.TraceSummonTrap(spell, target, summonTrap, position, false, 0u, boundary.BlockedReason);
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

        private static Vector3 ResolveSummonPetPosition(ISpell spell, IUnitEntity target, IPlayer player)
        {
            if (player?.Map != null)
                return player.Position;

            return target.Map != null
                ? target.Position
                : spell.Caster.Position;
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

            RavelSignalReceiverEvidenceBoundarySnapshot boundary =
                RavelSignalReceiverEvidenceBoundary.Describe(ravelSignal.Mode);
            if (!boundary.IsConservativelyDispatchSupported)
            {
                SpellEffectDiagnostics.TraceRavelSignal(spell, target, info, ravelSignal, boundary.BlockedReason);
                return;
            }

            target.SendSignal(ravelSignal.SignalId);
            SpellEffectDiagnostics.TraceRavelSignal(spell, target, info, ravelSignal, null);
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
                case 130u:
                    TraceThreatMultiplier(spell, target, threat, info.Entry.ThreatMultiplier);
                    break;
                default:
                    ApplyThreatDelta(spell, target, threat, ResolveThreatAmount(threat), $"add-mode-{threat.Mode}");
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

        private static void TraceThreatMultiplier(ISpell spell, IUnitEntity target, SpellEffectThreatModificationSemantics threat, float multiplier)
        {
            (IUnitEntity owner, IUnitEntity hated) = ResolveThreatOwnerAndHated(spell, target);
            uint beforeThreat = owner.ThreatManager.GetHostile(hated.Guid)?.Threat ?? 0u;
            string skippedReason = float.IsFinite(multiplier) && multiplier >= 0f
                ? string.Empty
                : "invalid-threat-multiplier";
            SpellEffectDiagnostics.TraceThreatModification(spell, target, threat, $"damage-threat-multiplier:{multiplier:R}", owner.Guid, hated.Guid, beforeThreat, beforeThreat, skippedReason);
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

            IForcedMovementGenerator forcedMovementGenerator = GetForcedMovementGenerator();
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
            SpellEffectProxySemantics proxy = SpellEffectInterpreter.Interpret(info).Proxy;
            if (ShouldRouteProxyToOriginalCaster(spell, proxy))
            {
                HandleProxySpell(spell, spell.Caster, spell.Caster, info, proxy);
                return;
            }

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

        [SpellEffectHandler(SpellEffectType.ProxyRandomExclusive)]
        public static void HandleEffectProxyRandomExclusive(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectProxySemantics proxy = SelectProxyRandomExclusive(SpellEffectInterpreter.Interpret(info).ProxyRandomExclusive);
            HandleProxySpell(spell, target, target, info, proxy);
        }

        public static void HandleEffectProxyRandomExclusiveWorld(ISpell spell, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectProxySemantics proxy = SelectProxyRandomExclusive(SpellEffectInterpreter.Interpret(info).ProxyRandomExclusive);
            HandleProxySpell(spell, spell.Caster, target, info, proxy);
        }

        [SpellEffectHandler(SpellEffectType.PetCastSpell)]
        public static void HandleEffectPetCastSpell(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectPetCastSpellSemantics petCastSpell = SpellEffectInterpreter.Interpret(info).PetCastSpell;
            if (petCastSpell == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TracePetCastSpell(spell, target, petCastSpell, 0u, 0u, 0u, [], false, "no-player-owner");
                return;
            }

            if (petCastSpell.RequiredSummonSpell4Id == 0u)
            {
                SpellEffectDiagnostics.TracePetCastSpell(spell, target, petCastSpell, player.Guid, 0u, 0u, [], false, "missing-required-summon-spell");
                return;
            }

            if (GetGameTableManager().Spell4?.GetEntry(petCastSpell.RequiredSummonSpell4Id) == null)
            {
                SpellEffectDiagnostics.TracePetCastSpell(spell, target, petCastSpell, player.Guid, 0u, 0u, [], false, "unknown-required-summon-spell");
                return;
            }

            if (petCastSpell.PetSpell4Id == 0u || GetGameTableManager().Spell4?.GetEntry(petCastSpell.PetSpell4Id) == null)
            {
                SpellEffectDiagnostics.TracePetCastSpell(spell, target, petCastSpell, player.Guid, 0u, 0u, [], false, "unknown-pet-spell");
                return;
            }

            IReadOnlyList<uint> summonCreatureIds = ResolvePetCastSummonCreatureIds(petCastSpell.RequiredSummonSpell4Id);
            if (summonCreatureIds.Count == 0)
            {
                SpellEffectDiagnostics.TracePetCastSpell(spell, target, petCastSpell, player.Guid, 0u, 0u, summonCreatureIds, false, "missing-summon-pet-link");
                return;
            }

            if (!TryGetActivePetCastSource(player, summonCreatureIds, out IUnitEntity petCaster))
            {
                SpellEffectDiagnostics.TracePetCastSpell(spell, target, petCastSpell, player.Guid, 0u, 0u, summonCreatureIds, false, "missing-active-pet");
                return;
            }

            uint primaryTargetId = spell.Parameters.PrimaryTargetId != 0u
                ? spell.Parameters.PrimaryTargetId
                : target.Guid;
            petCaster.CastSpell(petCastSpell.PetSpell4Id, new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                PrimaryTargetId        = primaryTargetId,
                UserInitiatedSpellCast = false,
                ClientContextToken     = spell.Parameters.ClientContextToken,
                ClientRequestSource    = spell.Parameters.ClientRequestSource
            });

            SpellEffectDiagnostics.TracePetCastSpell(spell, target, petCastSpell, player.Guid, petCaster.Guid, primaryTargetId, summonCreatureIds, true, null);
        }

        [SpellEffectHandler(SpellEffectType.SettlerCampfire)]
        public static void HandleEffectSettlerCampfire(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectSettlerCampfireSemantics campfire = SpellEffectInterpreter.Interpret(info).SettlerCampfire;
            if (campfire == null)
                return;

            if (!SettlerCampfireSpell.TryGetBackInActionSpell4Id(campfire.TierIndex, out uint backInActionSpell4Id))
            {
                SpellEffectDiagnostics.TraceSettlerCampfire(spell, target, campfire, 0u, false, "unknown-tier");
                return;
            }

            if (GetGameTableManager().Spell4.GetEntry(backInActionSpell4Id) == null)
            {
                SpellEffectDiagnostics.TraceSettlerCampfire(spell, target, campfire, backInActionSpell4Id, false, "unknown-back-in-action-spell");
                return;
            }

            SpellEffectDiagnostics.TraceSettlerCampfire(spell, target, campfire, backInActionSpell4Id, true, null);
            spell.Caster.CastSpell(backInActionSpell4Id, new SpellParameters
            {
                ParentSpellInfo        = spell.Parameters.SpellInfo,
                RootSpellInfo          = spell.Parameters.RootSpellInfo,
                PrimaryTargetId        = target.Guid,
                UserInitiatedSpellCast = false,
                ClientContextToken     = spell.Parameters.ClientContextToken,
                ClientRequestSource    = spell.Parameters.ClientRequestSource
            });
        }

        private static void HandleProxySpell(ISpell spell, IUnitEntity proxyCaster, IWorldEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectProxySemantics proxy = SpellEffectInterpreter.Interpret(info).Proxy;
            HandleProxySpell(spell, proxyCaster, target, info, proxy);
        }

        private static void HandleProxySpell(ISpell spell, IUnitEntity proxyCaster, IWorldEntity target, ISpellTargetEffectInfo info, SpellEffectProxySemantics proxy)
        {
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

        private static SpellEffectProxySemantics SelectProxyRandomExclusive(SpellEffectProxyRandomExclusiveSemantics randomExclusive)
        {
            if (randomExclusive?.Candidates == null || randomExclusive.Candidates.Count == 0)
                return null;

            List<SpellEffectProxyRandomExclusiveCandidate> weightedCandidates = randomExclusive.Candidates
                .Where(c => c.Spell4Id != 0u && c.Weight > 0u)
                .ToList();

            if (weightedCandidates.Count == 0)
            {
                SpellEffectProxyRandomExclusiveCandidate fallback = randomExclusive.Candidates.FirstOrDefault(c => c.Spell4Id != 0u);
                return fallback.Spell4Id == 0u ? null : new SpellEffectProxySemantics(fallback.Spell4Id);
            }

            long totalWeight = weightedCandidates.Sum(c => (long)c.Weight);
            long roll = Random.Shared.NextInt64(totalWeight);
            foreach (SpellEffectProxyRandomExclusiveCandidate candidate in weightedCandidates)
            {
                if (roll < candidate.Weight)
                    return new SpellEffectProxySemantics(candidate.Spell4Id);

                roll -= candidate.Weight;
            }

            return new SpellEffectProxySemantics(weightedCandidates[^1].Spell4Id);
        }

        private static bool ShouldRouteProxyToOriginalCaster(ISpell spell, SpellEffectProxySemantics proxy)
        {
            return spell.Parameters.SpellInfo.Entry.Id == RelentlessStrikesTelegraphSpell4Id
                && proxy?.Spell4Id == RelentlessStrikesAddCellSpell4Id
                && spell.Caster is IPlayer;
        }

        private static IReadOnlyList<uint> ResolvePetCastSummonCreatureIds(uint requiredSummonSpell4Id)
        {
            IGlobalSpellManager globalSpellManager = dependencyResolver?.GetGlobalSpellManager();
            if (globalSpellManager == null)
                return [];

            IEnumerable<Spell4EffectsEntry> entries = globalSpellManager.GetSpell4EffectEntries(requiredSummonSpell4Id) ?? [];
            return entries
                .Where(e => e.EffectType == SpellEffectType.SummonPet && e.DataBits00 != 0u)
                .Select(e => e.DataBits00)
                .Distinct()
                .ToList();
        }

        private static bool TryGetActivePetCastSource(IPlayer player, IEnumerable<uint> summonCreatureIds, out IUnitEntity petCaster)
        {
            petCaster = null;
            IEntitySummonFactory summonFactory = player?.SummonFactory;
            if (summonFactory == null)
                return false;

            foreach (uint creatureId in summonCreatureIds)
            {
                if (!summonFactory.TryGetSummonCreature(creatureId, out IWorldEntity summon))
                    continue;

                if (summon is IUnitEntity unit)
                {
                    petCaster = unit;
                    return true;
                }
            }

            return false;
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

            if (spell.Parameters.DeferActivateEffectObjectiveCredit)
            {
                IReadOnlyCollection<uint> deferredTargetGroupIds = GetAssetManager()?.GetTargetGroupsForCreatureId(activatedEntity.CreatureId);
                SpellEffectDiagnostics.TraceActivate(spell, target, activate, player.Guid, activatedEntity.CreatureId, deferredTargetGroupIds?.Count ?? 0);
                return;
            }

            player.RecordStarterTutorialDepartureTerminal(activatedEntity.CreatureId);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity, activatedEntity.CreatureId, 1u);
            player.QuestManager.ObjectiveUpdate(QuestObjectiveType.ActivateEntity2, activatedEntity.CreatureId, 1u);

            IReadOnlyCollection<uint> targetGroupIds = GetAssetManager()?.GetTargetGroupsForCreatureId(activatedEntity.CreatureId);
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
            SpellEffectUnitStateSetSemantics unitState = SpellEffectInterpreter.Interpret(info).UnitStateSet;
            if (unitState == null)
                return;

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
            SpellEffectSetBusySemantics setBusy = SpellEffectInterpreter.Interpret(info).SetBusy;
            if (setBusy == null)
                return;

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
            Creature2Entry creature2 = GetGameTableManager().Creature2.GetEntry(info.Entry.DataBits02);
            if (creature2 == null)
                return;

            Creature2DisplayGroupEntryEntry displayGroupEntry = GetGameTableManager().Creature2DisplayGroupEntry.Entries.FirstOrDefault(d => d.Creature2DisplayGroupId == creature2.Creature2DisplayGroupId);
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

            if (GetGameTableManager().Creature2.GetEntry(info.Entry.DataBits00) == null)
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

            player.CastSpell(52539, new SpellParameters
            {
                PrimaryTargetId        = player.Guid,
                UserInitiatedSpellCast = false,
                IgnoreGlobalCooldown   = true,
                CancelActiveTrade      = true,
                ClientRequestSource    = nameof(HandleEffectSummonMount)
            });
            player.CastSpell(80530, new SpellParameters
            {
                PrimaryTargetId        = player.Guid,
                UserInitiatedSpellCast = false,
                IgnoreGlobalCooldown   = true,
                CancelActiveTrade      = true,
                ClientRequestSource    = nameof(HandleEffectSummonMount)
            });
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

            ActionBarShortcutSetEntry actionBarShortcutSetEntry = GetGameTableManager().ActionBarShortcutSet.GetEntry(actionBarSet.ActionBarShortcutSetId);
            if (actionBarShortcutSetEntry == null)
            {
                SpellEffectDiagnostics.TraceActionBarSet(spell, target, actionBarSet, player.Guid, target.Guid, ShortcutSet.FloatingSpellBar, false, "unknown-shortcut-set-id");
                return;
            }

            const ShortcutSet shortcutSet = ShortcutSet.FloatingSpellBar;
            uint associatedUnitId = target.Guid != 0u ? target.Guid : player.Guid;
            player.Session.EnqueueMessageEncrypted(new ServerActionBarSet
            {
                ShortcutSet            = shortcutSet,
                ActionBarShortcutSetId = (ushort)actionBarSet.ActionBarShortcutSetId,
                AssociatedUnitId       = associatedUnitId
            });
            player.SpellManager.SetActiveFloatingActionBarShortcutSet(actionBarSet.ActionBarShortcutSetId, spell.Parameters.SpellInfo.Entry.Id);
            SpellEffectDiagnostics.TraceActionBarSet(spell, target, actionBarSet, player.Guid, associatedUnitId, shortcutSet, true, null);
        }

        [SpellEffectHandler(SpellEffectType.Teleport)]
        public static void HandleEffectTeleport(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectTeleportSemantics teleport = SpellEffectInterpreter.Interpret(info).Teleport;
            if (teleport == null || teleport.WorldLocation2Id == 0u)
                return;

            WorldLocation2Entry locationEntry = GetGameTableManager().WorldLocation2.GetEntry(teleport.WorldLocation2Id);
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

            if (!player.CanTeleport())
            {
                SpellEffectDiagnostics.TraceHousingTeleport(spell, target, housingTeleport, escapeVariant, player.Guid, false, "pending-teleport");
                return;
            }

            IGlobalResidenceManager globalResidenceManager = GetGlobalResidenceManager();
            IResidence residence = globalResidenceManager.GetResidenceByOwner(player.Name)
                ?? globalResidenceManager.CreateResidence(player);
            if (residence == null)
            {
                SpellEffectDiagnostics.TraceHousingTeleport(spell, target, housingTeleport, escapeVariant, player.Guid, false, "missing-residence");
                return;
            }

            IResidenceEntrance entrance;
            try
            {
                entrance = globalResidenceManager.GetResidenceEntrance(residence.PropertyInfoId);
            }
            catch (HousingException)
            {
                SpellEffectDiagnostics.TraceHousingTeleport(spell, target, housingTeleport, escapeVariant, player.Guid, false, "missing-entrance");
                return;
            }

            IMapLock mapLock = GetMapLockManager().GetResidenceLock(residence.Parent ?? residence);
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
            TaxiNodeEntry taxiNode = GetGameTableManager().TaxiNode.GetEntry(spell.Parameters.TaxiNode);
            if (taxiNode == null)
                return;

            WorldLocation2Entry worldLocation = GetGameTableManager().WorldLocation2.GetEntry(taxiNode.WorldLocation2Id);
            if (worldLocation == null)
                return;

            if (target is not IPlayer player)
                return;

            if (!player.CanTeleport())
                return;

            var rotation = new Quaternion(worldLocation.Facing0, worldLocation.Facing1, worldLocation.Facing2, worldLocation.Facing3);
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

            GenericUnlockEntryEntry entry = GetGameTableManager().GenericUnlockEntry?.GetEntry(info.Entry.DataBits00);
            if (entry == null)
            {
                ReportMissingCollectionData(
                    GetGameTableManager().GenericUnlockEntry == null,
                    GenericUnlockEntryTableName,
                    info.Entry.DataBits00,
                    nameof(HandleEffectLearnDyeColor),
                    "Cannot resolve dye color generic unlock.");
            }

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

            if (!TryLearnCollectionSpell(player, info.Entry.DataBits00, "Spell mount unlock", out uint spell4BaseId, out string skippedReason))
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

            if (GetGameTableManager().PetFlair?.GetEntry(info.Entry.DataBits00) == null)
            {
                MissingGameDataDiagnostics.ReportSkippedGrant(
                    "Spell pet flair unlock",
                    PetFlairTableName,
                    info.Entry.DataBits00,
                    nameof(SpellHandler) + "." + nameof(HandleEffectUnlockPetFlair));
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

            if (!TryLearnCollectionSpell(player, info.Entry.DataBits00, "Spell vanity pet unlock", out uint spell4BaseId, out string skippedReason))
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

            if (GetGameTableManager().Creature2.GetEntry(info.Entry.DataBits00) == null)
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

            if (GetGameTableManager().CharacterTitle?.GetEntry(info.Entry.DataBits00) == null)
            {
                MissingGameDataDiagnostics.ReportSkippedGrant(
                    "Spell title grant",
                    CharacterTitleTableName,
                    info.Entry.DataBits00,
                    nameof(SpellHandler) + "." + nameof(HandleEffectTitleGrant));
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

            if (GetGameTableManager().CharacterTitle?.GetEntry(info.Entry.DataBits00) == null)
            {
                ReportMissingCollectionData(
                    GetGameTableManager().CharacterTitle == null,
                    CharacterTitleTableName,
                    info.Entry.DataBits00,
                    nameof(HandleEffectTitleRevoke),
                    "Cannot resolve title revoke target.");
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

        [SpellEffectHandler(SpellEffectType.SetMatchingEligibility)]
        public static void HandleEffectSetMatchingEligibility(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            if (target is not IPlayer player || player.Session == null)
                return;

            uint flags = info.Entry.DataBits00;
            if (player is Player playerEntity)
                playerEntity.MatchingEligibilityFlagMask = flags;

            player.Session.EnqueueMessageEncrypted(new ServerMatchingEligibilityChanged
            {
                MatchingEligibilityFlags = flags
            });
        }

        [SpellEffectHandler(SpellEffectType.Proc)]
        public static void HandleEffectProc(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectProcSemantics proc = SpellEffectInterpreter.Interpret(info).Proc;
            if (proc == null)
                return;

            if (proc.TriggerSpell4Id == 0u || GetGameTableManager().Spell4.GetEntry(proc.TriggerSpell4Id) == null)
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

            if (ShouldSuppressStarterTutorialScanCrowdControl(spell, ccState))
            {
                info.DropEffect = true;
                return;
            }

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

        internal static bool ShouldSuppressStarterTutorialScanCrowdControl(ISpell spell, SpellEffectCCStateSemantics ccState)
        {
            return spell.Parameters.SpellInfo.Entry.Id == StarterTutorialScanSpellId
                && ccState.State == CCState.Disable;
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
            return (ushort)(GetGameTableManager().CCStates?.GetEntry((uint)state)?.CcStateDiminishingReturnsId ?? 0u);
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

            Spell4Entry spell4Entry = GetGameTableManager().Spell4.GetEntry(charges.Spell4Id);
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
            Spell4Entry spell4Entry = spell4Id == 0u ? null : GetGameTableManager().Spell4.GetEntry(spell4Id);
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
            if (removeScope == "spell4-group" && target is IPlayer player)
            {
                player.SpellManager.ClearActiveFloatingActionBarShortcutSetForSpellGroup(forceRemove.Spell4Id);
                player.SpellManager.ClearActivePetActionSpellsForSpellGroup(forceRemove.Spell4Id);
            }

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

            Spell4Entry immuneSpell = GetGameTableManager().Spell4.GetEntry(immunity.Spell4Id);
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

            Spell4Entry spell4Entry = GetGameTableManager().Spell4.GetEntry(addSpell.Spell4Id);
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

            if (delayDeath.TriggerSpell4Id != 0u && GetGameTableManager().Spell4.GetEntry(delayDeath.TriggerSpell4Id) == null)
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
            if (vital == Vital.Invalid)
            {
                SpellEffectDiagnostics.TraceClampVital(spell, target, clampVital, vital, target.Health, target.Health, false, false, "unknown-vital");
                return;
            }

            uint valueBefore = GetVitalValueForDiagnostics(target, vital);
            target.AddVitalClamp(
                info.EffectId,
                spell.Parameters.SpellInfo.Entry.Id,
                spell.CastingId,
                vital,
                clampVital.Ratio,
                clampVital.Mode,
                clampVital.VitalMode);

            SpellEffectDiagnostics.TraceClampVital(spell, target, clampVital, vital, valueBefore, GetVitalValueForDiagnostics(target, vital), true, false, null);
        }

        internal static Vital ResolveClampVital(SpellEffectClampVitalSemantics clampVital)
        {
            // All observed ClampVital rows are health-ceiling rows. DataBits01=2 appears
            // as a mode marker on ratio-1 rows, not as Vital.Breath.
            return Vital.Health;
        }

        private static uint GetVitalValueForDiagnostics(IUnitEntity target, Vital vital)
        {
            return vital switch
            {
                Vital.Health         => target.Health,
                Vital.ShieldCapacity => target.Shield,
                _                    => 0u
            };
        }

        [SpellEffectHandler(SpellEffectType.ShieldOverload)]
        public static void HandleEffectShieldOverload(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectShieldOverloadSemantics shieldOverload = SpellEffectInterpreter.Interpret(info).ShieldOverload;
            if (shieldOverload == null)
                return;

            if (shieldOverload.DataBits00 != 0u || shieldOverload.DataBits01 != 0u || shieldOverload.DataBits02 != 0u)
            {
                SpellEffectDiagnostics.TraceShieldOverload(spell, target, shieldOverload, target.Shield, target.Shield, false, false, "non-zero-payload");
                return;
            }

            uint shieldBefore = target.Shield;
            target.AddShieldOverload(info.EffectId, spell.Parameters.SpellInfo.Entry.Id, spell.CastingId);
            target.Shield = 0u;
            SpellEffectDiagnostics.TraceShieldOverload(spell, target, shieldOverload, shieldBefore, target.Shield, true, false, null);
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
                    player.PathManager.AddXp(pathXp.Amount);
                    SpellEffectDiagnostics.TracePathXpModify(spell, target, pathXp, $"add-xp-mode-{pathXp.Mode}", true, null);
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

            uint amount = CalculateLevelScaledXp(player, levelScaledXp, out string skippedReason);
            if (amount == 0u)
            {
                SpellEffectDiagnostics.TraceGrantLevelScaledXp(spell, target, levelScaledXp, 0u, false, skippedReason);
                return;
            }

            player.XpManager.GrantXp(amount, ExpReason.Spell);
            SpellEffectDiagnostics.TraceGrantLevelScaledXp(spell, target, levelScaledXp, amount, true, null);
        }

        [SpellEffectHandler(SpellEffectType.ModifyRestedXP)]
        public static void HandleEffectModifyRestedXp(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectModifyRestedXpSemantics modifyRestedXp = SpellEffectInterpreter.Interpret(info).ModifyRestedXp;
            if (modifyRestedXp == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceModifyRestedXp(spell, target, modifyRestedXp, 0u, 0u, false, "no-player-owner");
                return;
            }

            if (float.IsNaN(modifyRestedXp.LevelSpanMultiplier) || float.IsInfinity(modifyRestedXp.LevelSpanMultiplier))
            {
                SpellEffectDiagnostics.TraceModifyRestedXp(spell, target, modifyRestedXp, player.XpManager.RestBonusXp, player.XpManager.RestBonusXp, false, "invalid-multiplier");
                return;
            }

            uint previousRestBonusXp = player.XpManager.RestBonusXp;
            uint currentRestBonusXp = player.XpManager.ModifyRestBonusXp(modifyRestedXp.LevelSpanMultiplier);
            SpellEffectDiagnostics.TraceModifyRestedXp(spell, target, modifyRestedXp, previousRestBonusXp, currentRestBonusXp, true, null);
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
            if (GetGlobalAchievementManager()?.GetAchievement(achievementId) == null)
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

            if (giveItem.Item2Id == 0u || GetGameTableManager().Item.GetEntry(giveItem.Item2Id) == null)
            {
                SpellEffectDiagnostics.TraceGiveItemToPlayer(spell, target, giveItem, player.Guid, 0u, false, "unknown-item");
                return;
            }

            uint count = giveItem.Count == 0u ? 1u : giveItem.Count;
            player.Inventory.ItemCreate(InventoryLocation.Inventory, giveItem.Item2Id, count, ItemUpdateReason.SpellEffect);
            SpellEffectDiagnostics.TraceGiveItemToPlayer(spell, target, giveItem, player.Guid, count, true, null);
        }

        [SpellEffectHandler(SpellEffectType.GiveLootTableToPlayer)]
        public static void HandleEffectGiveLootTableToPlayer(ISpell spell, IUnitEntity target, ISpellTargetEffectInfo info)
        {
            SpellEffectGiveLootTableToPlayerSemantics giveLoot = SpellEffectInterpreter.Interpret(info).GiveLootTableToPlayer;
            if (giveLoot == null)
                return;

            IPlayer player = GetPlayerSpellOwner(spell, target);
            if (player == null)
            {
                SpellEffectDiagnostics.TraceGiveLootTableToPlayer(spell, target, giveLoot, 0u, 0u, 0, false, "no-player-owner");
                return;
            }

            if (giveLoot.LootGroupId == 0u)
            {
                SpellEffectDiagnostics.TraceGiveLootTableToPlayer(spell, target, giveLoot, player.Guid, 0u, 0, false, "missing-loot-group");
                return;
            }

            IGlobalLootManager lootManager = GetGlobalLootManager();
            if (lootManager == null)
            {
                SpellEffectDiagnostics.TraceGiveLootTableToPlayer(spell, target, giveLoot, player.Guid, 0u, 0, false, "missing-loot-manager");
                return;
            }

            uint rollCount = giveLoot.RollCount == 0u ? 1u : giveLoot.RollCount;
            if (!lootManager.TryGenerateLoot(giveLoot.LootGroupId, player, rollCount, out IReadOnlyList<GeneratedLootItem> items, out string reason))
            {
                SpellEffectDiagnostics.TraceGiveLootTableToPlayer(spell, target, giveLoot, player.Guid, rollCount, 0, false, reason);
                return;
            }

            items ??= [];
            if (items.Count == 0)
            {
                SpellEffectDiagnostics.TraceGiveLootTableToPlayer(spell, target, giveLoot, player.Guid, rollCount, 0, false, "empty-generated-loot");
                return;
            }

            if (!lootManager.CanDeliverGeneratedLoot(player, items, out reason))
            {
                SpellEffectDiagnostics.TraceGiveLootTableToPlayer(spell, target, giveLoot, player.Guid, rollCount, items.Count, false, reason);
                return;
            }

            uint ownerUnitId = target.Guid != 0u ? target.Guid : player.Guid;
            lootManager.GiveGeneratedLoot(player, items, ownerUnitId, sendGrantedNotify: true, parentUnitId: spell.Caster.Guid);
            SpellEffectDiagnostics.TraceGiveLootTableToPlayer(spell, target, giveLoot, player.Guid, rollCount, items.Count, true, null);
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

            TradeskillSchematic2Entry schematicEntry = GetGameTableManager().TradeskillSchematic2.GetEntry(giveSchematic.TradeskillSchematic2Id);
            if (schematicEntry == null)
            {
                SpellEffectDiagnostics.TraceGiveSchematic(spell, target, giveSchematic, player.Guid, 0u, false, "unknown-schematic");
                return;
            }

            bool learned = player.LearnSchematic(giveSchematic.TradeskillSchematic2Id);
            SpellEffectDiagnostics.TraceGiveSchematic(spell, target, giveSchematic, player.Guid, schematicEntry.TradeSkillId, learned, learned ? null : "already-learned");
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
                itemVisualSwap.DyeData);

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

            Creature2OutfitInfoEntry outfitEntry = GetGameTableManager().Creature2OutfitInfo.GetEntry(disguiseOutfit.OutfitInfoId);
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

            if (GetGameTableManager().ItemDisplay.GetEntry(itemDisplayId) == null)
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

            Spell4StackGroupEntry stackGroup = spell.Parameters.SpellInfo.StackGroup;
            if (stackGroup != null && stackGroup.StackCap > 0u)
                target.EnforceSpellPropertyStackGroupCap(spell.Parameters.SpellInfo.Entry.Spell4StackGroupId, stackGroup.StackCap);

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
                case 1u:
                    removeScope = "spell4-group";
                    predicate = spell4Id =>
                    {
                        Spell4Entry spell4Entry = GetGameTableManager().Spell4?.GetEntry(spell4Id);
                        return spell4Entry != null && Spell4GroupListContainsSpellGroup(spell4Entry.Spell4GroupListId, forceRemove.Spell4Id);
                    };
                    return true;
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

        private static bool Spell4GroupListContainsSpellGroup(uint spell4GroupListId, uint spellGroupId)
        {
            if (spell4GroupListId == 0u || spellGroupId == 0u)
                return false;

            Spell4GroupListEntry entry = GetGameTableManager().Spell4GroupList?.GetEntry(spell4GroupListId);
            if (entry == null)
                return false;

            uint[] spellGroupIds =
            [
                entry.SpellGroupId00,
                entry.SpellGroupId01,
                entry.SpellGroupId02,
                entry.SpellGroupId03,
                entry.SpellGroupId04,
                entry.SpellGroupId05,
                entry.SpellGroupId06,
                entry.SpellGroupId07,
                entry.SpellGroupId08,
                entry.SpellGroupId09,
                entry.SpellGroupId10,
                entry.SpellGroupId11,
                entry.SpellGroupId12,
                entry.SpellGroupId13,
                entry.SpellGroupId14,
                entry.SpellGroupId15,
                entry.SpellGroupId16,
                entry.SpellGroupId17,
                entry.SpellGroupId18,
                entry.SpellGroupId19,
                entry.SpellGroupId20,
                entry.SpellGroupId21,
                entry.SpellGroupId22,
                entry.SpellGroupId23,
                entry.SpellGroupId24,
                entry.SpellGroupId25,
                entry.SpellGroupId26,
                entry.SpellGroupId27,
                entry.SpellGroupId28,
                entry.SpellGroupId29,
                entry.SpellGroupId30,
                entry.SpellGroupId31
            ];

            return spellGroupIds.Contains(spellGroupId);
        }

        private static bool TryLearnCollectionSpell(IPlayer player, uint spell4Id, string collectionGrantType, out uint spell4BaseId, out string skippedReason)
        {
            spell4BaseId  = 0u;
            skippedReason = null;

            if (spell4Id == 0u)
            {
                skippedReason = "missing-spell4";
                return false;
            }

            Spell4Entry spell4Entry = GetGameTableManager().Spell4?.GetEntry(spell4Id);
            if (spell4Entry == null)
            {
                MissingGameDataDiagnostics.ReportSkippedGrant(
                    collectionGrantType,
                    Spell4TableName,
                    spell4Id,
                    nameof(SpellHandler) + "." + nameof(TryLearnCollectionSpell));
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

        private static void ReportMissingCollectionData(bool tableMissing, string tableName, uint staticId, string handlerName, string detail)
        {
            string context = nameof(SpellHandler) + "." + handlerName;
            if (tableMissing)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    tableName,
                    context,
                    MissingGameDataSeverity.PlayerImpacting,
                    detail);
                return;
            }

            MissingGameDataDiagnostics.ReportMissingRow(
                tableName,
                staticId,
                context,
                MissingGameDataSeverity.PlayerImpacting,
                detail);
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

            XpPerLevelEntry currentLevel = GetGameTableManager().XpPerLevel.GetEntry(player.Level);
            XpPerLevelEntry nextLevel = GetGameTableManager().XpPerLevel.GetEntry(player.Level + 1u);
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
                case 0:
                    property = Property.DamageDealtMultiplierMelee;
                    return true;
                case 1:
                    property = Property.DamageDealtMultiplierRanged;
                    return true;
                case 2:
                    property = Property.DamageDealtMultiplierSpell;
                    return true;
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
                case 9:
                    property = Property.DamageMitigationPctOffset;
                    return true;
                case 12:
                    property = Property.HealingMultiplierIncoming;
                    return true;
                case 13:
                    property = Property.HealingMultiplierOutgoing;
                    return true;
                default:
                    property = default;
                    skippedReason = "unknown-modifier-type";
                    return false;
            }
        }

        internal static bool TryResolveRewardPropertyModifier(
            SpellEffectRewardPropertyModifierSemantics rewardProperty,
            out RewardPropertyEntry entry,
            out float value,
            out string skippedReason)
        {
            return TryResolveRewardPropertyModifier(GetGameTableManager(), rewardProperty, out entry, out value, out skippedReason);
        }

        internal static bool TryResolveRewardPropertyModifier(
            IGameTableManager gameTableManager,
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

            entry = gameTableManager.RewardProperty?.GetEntry(rewardProperty.RewardPropertyId);
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

                if (GetGameTableManager().Spell4.GetEntry(candidate) != null)
                    return candidate;
            }

            return 0u;
        }

        private static bool TryResolveCooldownMutation(SpellEffectModifySpellCooldownSemantics cooldown, double beforeCooldown, out double afterCooldown, out string action, out string skippedReason)
        {
            afterCooldown = beforeCooldown;
            action = null;
            skippedReason = null;

            if (cooldown.DataFloat03 == 0f && cooldown.Operation == 0u)
            {
                afterCooldown = 0d;
                action = "reset";
                return true;
            }

            if (float.IsFinite(cooldown.DataFloat03) && MathF.Abs(cooldown.DataFloat03) >= 0.0001f)
            {
                if (cooldown.DataFloat03 <= -1f)
                {
                    afterCooldown = Math.Max(0d, beforeCooldown - Math.Abs(cooldown.DataFloat03) / 1000d);
                    action = "reduce-float-ms";
                    return true;
                }

                if (cooldown.DataFloat03 < 0f)
                {
                    afterCooldown = Math.Max(0d, beforeCooldown * Math.Max(0d, 1d - Math.Abs(cooldown.DataFloat03)));
                    action = "reduce-float-ratio";
                    return true;
                }

                if (cooldown.DataFloat03 < 1f)
                {
                    afterCooldown = Math.Max(0d, beforeCooldown * cooldown.DataFloat03);
                    action = "scale-float-ratio";
                    return true;
                }

                afterCooldown = cooldown.DataFloat03 / 1000d;
                action = "set-float-ms";
                return true;
            }

            int signedDataBits04 = unchecked((int)cooldown.DataBits04);
            if (signedDataBits04 != 0)
            {
                afterCooldown = signedDataBits04 < 0
                    ? Math.Max(0d, beforeCooldown - Math.Abs(signedDataBits04) / 1000d)
                    : beforeCooldown + signedDataBits04 / 1000d;
                action = signedDataBits04 < 0 ? "reduce-int04-ms" : "extend-int04-ms";
                return true;
            }

            int signedDataBits05 = unchecked((int)cooldown.DataBits05);
            if (signedDataBits05 != 0)
            {
                afterCooldown = signedDataBits05 < 0
                    ? Math.Max(0d, beforeCooldown - Math.Abs(signedDataBits05) / 1000d)
                    : beforeCooldown + signedDataBits05 / 1000d;
                action = signedDataBits05 < 0 ? "reduce-int05-ms" : "extend-int05-ms";
                return true;
            }

            action = "no-op";
            return true;
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

            Spell4Entry spell4Entry = GetGameTableManager().Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
                return false;

            try
            {
                spellBaseInfo = GetGlobalSpellManager().GetSpellBaseInfo(spell4Entry.Spell4BaseIdBaseSpell);
                return spellBaseInfo != null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
