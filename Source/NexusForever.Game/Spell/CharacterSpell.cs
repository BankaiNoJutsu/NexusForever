using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Spell
{
    public class CharacterSpell : ICharacterSpell
    {
        // IsSelfSpellDelegate bitmask 0x85: bits 0, 2, 7 → types 0, 2, 7 are "self-spell" (no targeting cursor)
        // SpellTarget_ResolveTargetEntity client entity cases: 0,2,6,7 -> caster entity; 1,3,5,8 -> current target; 4/default -> none.
        // Server-side type 7 still resolves through the current visible target for effect application.
        private const uint TargetTypeNoExplicitTarget = 0u; // self/caster-centered (e.g., mine explosion spell 305)
        private const uint TargetTypeSingleTarget = 1u;
        private const uint TargetTypeSelfAoe = 2u;
        private const uint TargetTypeTargetAoe = 3u;
        private const uint TargetTypeItemActivation = 6u; // item activation; entity = caster (mechanic 17, e.g., spell 11820)
        private const uint TargetTypeServiceLookup = 7u; // per-spell service tree (includes auto-attack spell 339)
        private const uint TargetTypeChain = 5u;
        private const uint TargetTypeCursorOrSelectedTarget = 8u; // current-target category with auto-target fallback bitmask 0x12a (bits 1,3,5,8)

        [Flags]
        public enum UnlockedSpellSaveMask
        {
            None   = 0x0000,
            Create = 0x0001,
            Tier   = 0x0002
        }

        public IPlayer Owner { get; }
        public ISpellBaseInfo BaseInfo { get; }
        public ISpellInfo SpellInfo { get; private set; }
        public ISpellInfo AlternateSpellInfo { get; private set; }
        public IItem Item { get; }

        public byte Tier
        {
            get => tier;
            set
            {
                if (tier != value)
                {
                    SpellInfo = BaseInfo.GetSpellInfo(value);
                    AlternateSpellInfo = ResolveAlternateSpellInfo();
                }

                tier = value;
                saveMask |= UnlockedSpellSaveMask.Tier;
            }
        }
        private byte tier;

        public uint AbilityCharges { get; private set; }
        public uint MaxAbilityCharges => SpellInfo.Entry.AbilityChargeCount;
        public double AbilityRechargeTimeRemaining => rechargeTimer?.Time ?? 0d;
        public double AbilityRechargePercentRemaining => rechargeTimer is { Duration: > 0d }
            ? rechargeTimer.Time / rechargeTimer.Duration
            : 0d;

        private UnlockedSpellSaveMask saveMask;

        private UpdateTimer rechargeTimer;
        private bool continuousCastHeld;

        /// <summary>
        /// Create a new <see cref="ICharacterSpell"/> from an existing database model.
        /// </summary>
        public CharacterSpell(IPlayer player, CharacterSpellModel model, ISpellBaseInfo baseInfo, IItem item)
        {
            Owner     = player;
            BaseInfo  = baseInfo;
            Item      = item;
            tier      = model.Tier;
            SpellInfo = baseInfo.GetSpellInfo(tier);

            InitialiseAbilityCharges();
            AlternateSpellInfo = ResolveAlternateSpellInfo();
        }

        /// <summary>
        /// Create a new <see cref="ICharacterSpell"/> from a <see cref="ISpellBaseInfo"/>.
        /// </summary>
        public CharacterSpell(IPlayer player, ISpellBaseInfo baseInfo, byte tier, IItem item)
        {
            Owner     = player;
            BaseInfo  = baseInfo ?? throw new ArgumentNullException();
            SpellInfo = baseInfo.GetSpellInfo(tier);
            Item      = item;
            this.tier = tier;

            InitialiseAbilityCharges();
            AlternateSpellInfo = ResolveAlternateSpellInfo();

            saveMask = UnlockedSpellSaveMask.Create;
        }

        private void InitialiseAbilityCharges()
        {
            if (MaxAbilityCharges == 0u)
                return;

            rechargeTimer  = new UpdateTimer(SpellInfo.Entry.AbilityRechargeTime / 1000d);
            AbilityCharges = MaxAbilityCharges;
            SendChargeUpdate();
        }

        public void Update(double lastTick)
        {
            if (MaxAbilityCharges > 0 && AbilityCharges < MaxAbilityCharges)
            {
                rechargeTimer.Update(lastTick);
                if (rechargeTimer.HasElapsed)
                {
                    AbilityCharges = Math.Clamp(AbilityCharges + SpellInfo.Entry.AbilityRechargeCount, 0u, MaxAbilityCharges);
                    SendChargeUpdate();
                    rechargeTimer.Reset();
                }
            }
        }

        public void Save(CharacterContext context)
        {
            if (saveMask == UnlockedSpellSaveMask.None)
                return;

            if ((saveMask & UnlockedSpellSaveMask.Create) != 0)
            {
                var model = new CharacterSpellModel
                {
                    Id           = Owner.CharacterId,
                    Spell4BaseId = BaseInfo.Entry.Id,
                    Tier         = tier
                };

                context.Add(model);
            }
            else
            {
                var model = new CharacterSpellModel
                {
                    Id           = Owner.CharacterId,
                    Spell4BaseId = BaseInfo.Entry.Id,
                };

                EntityEntry<CharacterSpellModel> entity = context.Attach(model);
                if ((saveMask & UnlockedSpellSaveMask.Tier) != 0)
                {
                    model.Tier = tier;
                    entity.Property(p => p.Tier).IsModified = true;
                }
            }

            saveMask = UnlockedSpellSaveMask.None;
        }

        /// <summary>
        /// Used for when the client does not have continuous casting enabled
        /// </summary>
        public void Cast(string clientRequestSource = null)
        {
            CastSpell(clientRequestSource);
        }

        /// <summary>
        /// Used for continuous casting when the client has it enabled, or spells with Cast Methods like ChargeRelease
        /// </summary>
        public void Cast(bool buttonPressed, string clientRequestSource = null)
        {
            Cast(buttonPressed, 0u, 0u, clientRequestSource);
        }

        public void Cast(bool buttonPressed, uint primaryTargetId, uint clientContextToken = 0u, string clientRequestSource = null)
        {
            if (!buttonPressed)
            {
                continuousCastHeld = false;
                if (IsChargeReleaseSpell())
                    Owner.TryReleaseChargeSpell(this, SpellInfo.Entry.Id, primaryTargetId, clientContextToken, clientRequestSource);

                return;
            }

            if (continuousCastHeld)
                return;

            continuousCastHeld = true;
            CastSpell(clientRequestSource, primaryTargetId, clientContextToken);
        }

        private void CastSpell(string clientRequestSource = null, uint primaryTargetId = 0u, uint clientContextToken = 0u)
        {
            ISpellInfo spellInfoToCast = ResolveSpellInfoToCast();
            Owner.CastSpell(new SpellParameters
            {
                CharacterSpell         = this,
                SpellInfo              = spellInfoToCast,
                RootSpellInfo          = SpellInfo,
                PrimaryTargetId        = primaryTargetId != 0u ? primaryTargetId : ResolvePrimaryTargetId(),
                UserInitiatedSpellCast = true,
                ClientContextToken     = clientContextToken,
                ClientRequestSource    = clientRequestSource
            });
        }

        private bool IsChargeReleaseSpell()
        {
            return BaseInfo.CastMethod == SpellCastMethod.ChargeRelease
                && (SpellInfo.Thresholds?.Count ?? 0) != 0;
        }

        private ISpellInfo ResolveSpellInfoToCast()
        {
            if (AlternateSpellInfo != null && CheckRunnerOverride())
                return AlternateSpellInfo;

            return SpellInfo;
        }

        private ISpellInfo ResolveAlternateSpellInfo()
        {
            uint alternateSpell4Id = SpellInfo.Entry.Spell4IdMechanicAlternateSpell;
            if (alternateSpell4Id == 0u)
                return null;

            Spell4Entry alternateEntry = GameTableManager.Instance.Spell4.GetEntry(alternateSpell4Id);
            if (alternateEntry == null)
                return null;

            ISpellBaseInfo alternateBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(alternateEntry.Spell4BaseIdBaseSpell);
            return alternateBaseInfo.GetSpellInfo((byte)alternateEntry.TierIndex);
        }

        private bool CheckRunnerOverride()
        {
            foreach (PrerequisiteEntry runnerPrereq in SpellInfo.PrerequisiteRunners)
                if (runnerPrereq != null && PrerequisiteManager.Instance.Meets(Owner, runnerPrereq.Id))
                    return true;

            return false;
        }

        private uint ResolvePrimaryTargetId()
        {
            uint targetType = BaseInfo.TargetMechanics?.TargetType ?? 0u;

            if (targetType == TargetTypeSelfAoe || targetType == TargetTypeNoExplicitTarget || targetType == TargetTypeItemActivation)
                return Owner.Guid;

            if (targetType is not TargetTypeSingleTarget and not TargetTypeTargetAoe and not TargetTypeChain and not TargetTypeServiceLookup and not TargetTypeCursorOrSelectedTarget)
                return 0u;

            if (Owner.TargetGuid == null)
                return 0u;

            return Owner.GetVisible<IWorldEntity>(Owner.TargetGuid.Value) != null
                ? Owner.TargetGuid.Value
                : 0u;
        }

        public void UseCharge()
        {
            if (AbilityCharges == 0)
                throw new SpellException("No charges available.");

            ModifyAbilityCharges(-1);
        }

        public void SetAbilityCharges(uint charges)
        {
            if (MaxAbilityCharges == 0u)
                return;

            AbilityCharges = Math.Clamp(charges, 0u, MaxAbilityCharges);
            SendChargeUpdate();
        }

        public void ModifyAbilityCharges(int delta)
        {
            if (MaxAbilityCharges == 0u)
                return;

            long value = (long)AbilityCharges + delta;
            SetAbilityCharges((uint)Math.Clamp(value, 0L, (long)MaxAbilityCharges));
        }

        private void SendChargeUpdate()
        {
            Owner.Session.EnqueueMessageEncrypted(new ServerSpellAbilityCharges
            {
                SpellId            = Item.Id,
                AbilityChargeCount = AbilityCharges
            });
        }
    }
}
