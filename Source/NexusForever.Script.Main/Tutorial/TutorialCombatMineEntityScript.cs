using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NetworkPosition = NexusForever.Network.World.Entity.Position;
using SharedInitialPosition = NexusForever.Network.World.Message.Model.Shared.InitialPosition;
using SharedTargetInfo = NexusForever.Network.World.Message.Model.Shared.TargetInfo;
using SharedTelegraphPosition = NexusForever.Network.World.Message.Model.Shared.TelegraphPosition;

namespace NexusForever.Script.Main.Tutorial
{
    [ScriptFilterCreatureId(TutorialCombatMineEasyCreatureId, TutorialCombatMineMediumCreatureId, TutorialCombatMineHardCreatureId)]
    public class TutorialCombatMineEntityScript : IWorldEntityScript, IOwnedScript<ISimpleCollidableEntity>
    {
        private const ushort TutorialWorldId = 3460;
        private const ushort ExileCombatQuestId = 10518;
        private const ushort DominionCombatQuestId = 10524;
        private const uint TutorialCombatMineEasyCreatureId = 73463u;
        private const uint TutorialCombatMineMediumCreatureId = 73667u;
        private const uint TutorialCombatMineHardCreatureId = 73668u;
        private const uint TutorialMineActivateSpellId = 85452u;
        private const uint TutorialMineEasyDangerZoneSpellId = 85430u;
        private const uint TutorialMineMediumDangerZoneSpellId = 85629u;
        private const uint TutorialMineHardDangerZoneSpellId = 85630u;
        private const uint TutorialMineEasyDamageEffectId = 225138u;
        private const uint TutorialMineMediumDamageEffectId = 225151u;
        private const uint TutorialMineHardDamageEffectId = 225155u;

        private readonly Dictionary<uint, PendingMineDetonation> pendingDetonations = [];

        private readonly IGameTableManager gameTableManager;
        private readonly IGlobalSpellManager globalSpellManager;
        private readonly ILogger<TutorialCombatMineEntityScript> log;

        private ISimpleCollidableEntity owner;

        public TutorialCombatMineEntityScript(
            ILogger<TutorialCombatMineEntityScript> log,
            IGameTableManager gameTableManager,
            IGlobalSpellManager globalSpellManager)
        {
            this.log                = log;
            this.gameTableManager   = gameTableManager;
            this.globalSpellManager = globalSpellManager;
        }

        public void OnLoad(ISimpleCollidableEntity owner)
        {
            this.owner = owner;
            owner.CreateFlags |= EntityCreateFlag.Immediate | EntityCreateFlag.HasInteractionPrereq;
            log.LogDebug("Starter tutorial combat mine script loaded for entity {EntityGuid}: creature={CreatureId}, position=({X}, {Y}, {Z}).",
                owner.Guid,
                owner.CreatureId,
                owner.Position.X,
                owner.Position.Y,
                owner.Position.Z);
        }

        public void OnActivateSuccess(IPlayer activator)
        {
            if (activator == null || !TryGetMineProfile(owner.CreatureId, out MineProfile profile))
                return;

            if (!CanArmMine(activator))
            {
                log.LogDebug("Skipped Rider's Reef combat mine detonation: player={PlayerGuid}, mine={MineGuid}, creature={CreatureId}, reason=quest-or-map-gate.",
                    activator.Guid,
                    owner.Guid,
                    owner.CreatureId);
                return;
            }

            if (pendingDetonations.ContainsKey(activator.Guid))
            {
                log.LogDebug("Skipped duplicate Rider's Reef combat mine detonation: player={PlayerGuid}, mine={MineGuid}, creature={CreatureId}.",
                    activator.Guid,
                    owner.Guid,
                    owner.CreatureId);
                return;
            }

            Spell4Entry dangerSpell = gameTableManager.Spell4.GetEntry(profile.DangerZoneSpellId);
            if (dangerSpell == null)
            {
                log.LogWarning("Unable to arm Rider's Reef combat mine {MineGuid}: danger-zone spell {Spell4Id} is missing.",
                    owner.Guid,
                    profile.DangerZoneSpellId);
                return;
            }

            uint castingId = globalSpellManager.NextCastingId;
            uint busyEffectId = globalSpellManager.NextEffectId;
            TimeSpan detonationDelay = TimeSpan.FromMilliseconds(Math.Max(GetDangerZoneWarningDurationMs(dangerSpell), 1u));

            owner.AddBusy(busyEffectId, TutorialMineActivateSpellId, castingId, 1u, activator.Guid, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u);
            SendDangerZoneStart(castingId, profile);

            pendingDetonations.Add(activator.Guid, new PendingMineDetonation(
                activator.Guid,
                castingId,
                busyEffectId,
                profile,
                DateTime.UtcNow + detonationDelay));

            log.LogDebug("Armed Rider's Reef combat mine: player={PlayerGuid}, mine={MineGuid}, creature={CreatureId}, dangerSpell={Spell4Id}, castingId={CastingId}, detonationMs={DetonationMs}.",
                activator.Guid,
                owner.Guid,
                owner.CreatureId,
                profile.DangerZoneSpellId,
                castingId,
                detonationDelay.TotalMilliseconds);
        }

        public void Update(double lastTick)
        {
            if (pendingDetonations.Count == 0)
                return;

            DateTime now = DateTime.UtcNow;
            foreach (PendingMineDetonation detonation in pendingDetonations.Values.Where(d => d.DetonatesAt <= now).ToList())
                Detonate(detonation);
        }

        private void Detonate(PendingMineDetonation detonation)
        {
            pendingDetonations.Remove(detonation.PlayerGuid);

            if (owner?.InWorld != true)
                return;

            IReadOnlyCollection<MineDamageResult> damageResults = ApplyMineDamage(detonation.Profile);
            SendDangerZoneGo(detonation.CastingId, detonation.Profile, damageResults);
            owner.EnqueueToVisible(new ServerSpellFinish
            {
                ServerUniqueId = detonation.CastingId
            }, true);

            bool removedBusy = owner.RemoveBusy(detonation.BusyEffectId);
            log.LogDebug("Detonated Rider's Reef combat mine: player={PlayerGuid}, mine={MineGuid}, creature={CreatureId}, dangerSpell={Spell4Id}, castingId={CastingId}, damageTargets={DamageTargets}, damageAmount={DamageAmount}, radius={Radius}, removedBusy={RemovedBusy}.",
                detonation.PlayerGuid,
                owner.Guid,
                owner.CreatureId,
                detonation.Profile.DangerZoneSpellId,
                detonation.CastingId,
                damageResults.Count,
                detonation.Profile.DamageAmount,
                detonation.Profile.Radius,
                removedBusy);
        }

        private bool CanArmMine(IPlayer player)
        {
            if (owner?.Map?.Entry?.Id != TutorialWorldId || player.Map?.Entry?.Id != TutorialWorldId)
                return false;

            return player.QuestManager.GetQuestState(ExileCombatQuestId) is QuestState.Accepted or QuestState.Achieved or QuestState.Completed
                || player.QuestManager.GetQuestState(DominionCombatQuestId) is QuestState.Accepted or QuestState.Achieved or QuestState.Completed;
        }

        private void SendDangerZoneStart(uint castingId, MineProfile profile)
        {
            var spellStart = new ServerSpellStart
            {
                CastingId       = castingId,
                Spell4Id        = profile.DangerZoneSpellId,
                RootSpell4Id    = profile.DangerZoneSpellId,
                ParentSpell4Id  = TutorialMineActivateSpellId,
                CasterId        = owner.Guid,
                PrimaryTargetId = owner.Guid,
                FieldPosition   = new NetworkPosition(owner.Position),
                Yaw             = owner.Rotation.X
            };

            spellStart.InitialPositionData.Add(new ServerSpellStart.InitialPosition
            {
                UnitId      = owner.Guid,
                TargetFlags = 3,
                Position    = new NetworkPosition(owner.Position),
                Yaw         = owner.Rotation.X
            });

            foreach (TelegraphDamageEntry telegraph in globalSpellManager.GetTelegraphDamageEntries(profile.DangerZoneSpellId))
            {
                spellStart.TelegraphPositionData.Add(new ServerSpellStart.TelegraphPosition
                {
                    TelegraphId    = (ushort)telegraph.Id,
                    AttachedUnitId = owner.Guid,
                    TargetFlags    = 3,
                    Position       = new NetworkPosition(owner.Position),
                    Yaw            = owner.Rotation.X
                });
            }

            owner.EnqueueToVisible(spellStart, true);
        }

        private IReadOnlyCollection<MineDamageResult> ApplyMineDamage(MineProfile profile)
        {
            if (owner?.Map == null || profile.DamageAmount == 0u || profile.DamageSpell4EffectId == 0u)
                return [];

            uint effectId = globalSpellManager.NextEffectId;
            float searchRadius = profile.Radius + 5f;
            var results = new List<MineDamageResult>();

            foreach (IPlayer target in owner.Map.Search(owner.Position, searchRadius, new MineDamageSearchCheck(owner.Position, searchRadius)).DistinctBy(p => p.Guid))
            {
                if (!CanDamageMineTarget(target, profile.Radius))
                    continue;

                results.Add(ApplyMineDamage(target, profile, effectId));
            }

            return results;
        }

        private bool CanDamageMineTarget(IPlayer player, float radius)
        {
            if (player == null || !player.IsAlive || !CanArmMine(player))
                return false;

            var minePosition = new Vector2(owner.Position.X, owner.Position.Z);
            var playerPosition = new Vector2(player.Position.X, player.Position.Z);
            float hitRadius = player.HitRadius * 0.5f;
            float radiusWithTarget = radius + hitRadius;

            return Vector2.DistanceSquared(minePosition, playerPosition) <= radiusWithTarget * radiusWithTarget;
        }

        private MineDamageResult ApplyMineDamage(IPlayer target, MineProfile profile, uint effectId)
        {
            uint shieldAbsorbAmount = Math.Min(target.Shield, profile.DamageAmount);
            uint healthDamage = profile.DamageAmount - shieldAbsorbAmount;
            uint overkillAmount = healthDamage > target.Health ? healthDamage - target.Health : 0u;
            bool wasAlive = target.IsAlive;

            if (shieldAbsorbAmount != 0u)
                target.Shield -= shieldAbsorbAmount;

            if (healthDamage != 0u)
                target.ModifyHealth(healthDamage, profile.DamageType, null);

            log.LogDebug("Applied Rider's Reef combat mine damage: target={TargetGuid}, mine={MineGuid}, effect={Spell4EffectId}, raw={RawDamage}, shieldAbsorb={ShieldAbsorb}, healthDamage={HealthDamage}, killed={Killed}.",
                target.Guid,
                owner.Guid,
                profile.DamageSpell4EffectId,
                profile.DamageAmount,
                shieldAbsorbAmount,
                healthDamage,
                wasAlive && !target.IsAlive);

            return new MineDamageResult(
                target.Guid,
                effectId,
                profile.DamageAmount,
                shieldAbsorbAmount,
                healthDamage,
                overkillAmount,
                wasAlive && !target.IsAlive);
        }

        private void SendDangerZoneGo(uint castingId, MineProfile profile, IReadOnlyCollection<MineDamageResult> damageResults)
        {
            var spellGo = new ServerSpellGo
            {
                ServerUniqueId     = castingId,
                BIgnoreCooldown    = true,
                PrimaryDestination = new NetworkPosition(owner.Position),
                Phase              = -1
            };

            spellGo.InitialPositionData.Add(new SharedInitialPosition
            {
                UnitId      = owner.Guid,
                TargetFlags = 3,
                Position    = new NetworkPosition(owner.Position),
                Yaw         = owner.Rotation.X
            });

            foreach (TelegraphDamageEntry telegraph in globalSpellManager.GetTelegraphDamageEntries(profile.DangerZoneSpellId))
            {
                spellGo.TelegraphPositionData.Add(new SharedTelegraphPosition
                {
                    TelegraphId    = (ushort)telegraph.Id,
                    AttachedUnitId = owner.Guid,
                    TargetFlags    = 3,
                    Position       = new NetworkPosition(owner.Position),
                    Yaw            = owner.Rotation.X
                });
            }

            foreach (MineDamageResult damageResult in damageResults)
            {
                var targetInfo = new SharedTargetInfo
                {
                    UnitId        = damageResult.TargetGuid,
                    TargetFlags   = 1,
                    InstanceCount = 1,
                    CombatResult  = CombatResult.Hit
                };

                targetInfo.EffectInfoData.Add(new SharedTargetInfo.EffectInfo
                {
                    Spell4EffectId = profile.DamageSpell4EffectId,
                    EffectUniqueId = damageResult.EffectId,
                    TimeRemaining  = -1,
                    InfoType       = 1,
                    DamageDescriptionData = new SharedTargetInfo.EffectInfo.DamageDescription
                    {
                        RawDamage          = damageResult.RawDamage,
                        RawScaledDamage    = damageResult.RawDamage,
                        ShieldAbsorbAmount = damageResult.ShieldAbsorbAmount,
                        AdjustedDamage     = damageResult.HealthDamage,
                        OverkillAmount     = damageResult.OverkillAmount,
                        KilledTarget       = damageResult.KilledTarget,
                        CombatResult       = CombatResult.Hit,
                        DamageType         = profile.DamageType
                    }
                });

                spellGo.TargetInfoData.Add(targetInfo);
            }

            owner.EnqueueToVisible(spellGo, true);
        }

        private static bool TryGetMineProfile(uint creatureId, out MineProfile profile)
        {
            profile = creatureId switch
            {
                TutorialCombatMineEasyCreatureId   => new MineProfile(TutorialMineEasyDangerZoneSpellId, TutorialMineEasyDamageEffectId, 600u, 5f, DamageType.Physical),
                TutorialCombatMineMediumCreatureId => new MineProfile(TutorialMineMediumDangerZoneSpellId, TutorialMineMediumDamageEffectId, 800u, 7f, DamageType.Physical),
                TutorialCombatMineHardCreatureId   => new MineProfile(TutorialMineHardDangerZoneSpellId, TutorialMineHardDamageEffectId, 1000u, 9f, DamageType.Physical),
                _                                  => default
            };

            return profile.DangerZoneSpellId != 0u;
        }

        private static uint GetDangerZoneWarningDurationMs(Spell4Entry dangerSpell)
        {
            return dangerSpell.CastTime != 0u
                ? dangerSpell.CastTime
                : dangerSpell.SpellDuration;
        }

        private readonly record struct MineProfile(uint DangerZoneSpellId, uint DamageSpell4EffectId, uint DamageAmount, float Radius, DamageType DamageType);
        private readonly record struct PendingMineDetonation(uint PlayerGuid, uint CastingId, uint BusyEffectId, MineProfile Profile, DateTime DetonatesAt);
        private readonly record struct MineDamageResult(uint TargetGuid, uint EffectId, uint RawDamage, uint ShieldAbsorbAmount, uint HealthDamage, uint OverkillAmount, bool KilledTarget);

        private sealed class MineDamageSearchCheck : ISearchCheck<IPlayer>
        {
            private readonly Vector2 position;
            private readonly float radius;

            public MineDamageSearchCheck(Vector3 position, float radius)
            {
                this.position = new Vector2(position.X, position.Z);
                this.radius   = radius;
            }

            public bool CheckEntity(IPlayer entity)
            {
                if (entity == null)
                    return false;

                var entityPosition = new Vector2(entity.Position.X, entity.Position.Z);
                return Vector2.DistanceSquared(position, entityPosition) <= radius * radius;
            }
        }
    }
}
