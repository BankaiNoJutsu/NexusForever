using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Spell
{
    public class CharacterSpell : ICharacterSpell
    {
        private const uint TargetTypeSingleTarget = 1u;
        private const uint TargetTypeSelfAoe = 2u;
        private const uint TargetTypeTargetAoe = 3u;
        private const uint TargetTypeChain = 5u;

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
        public IItem Item { get; }

        public byte Tier
        {
            get => tier;
            set
            {
                if (tier != value)
                    SpellInfo = BaseInfo.GetSpellInfo(value);

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
        public void Cast()
        {
            CastSpell();
        }

        /// <summary>
        /// Used for continuous casting when the client has it enabled, or spells with Cast Methods like ChargeRelease
        /// </summary>
        public void Cast(bool buttonPressed)
        {
            // TODO: Handle continuous casting of spell for Player if button remains depressed

            // If the player depresses button after the spell had exceeded its threshold, don't try and recast the spell until button is pressed down again.
            if (!buttonPressed)
                return;

            CastSpell();
        }

        private void CastSpell()
        {
            Owner.CastSpell(new SpellParameters
            {
                CharacterSpell         = this,
                SpellInfo              = SpellInfo,
                PrimaryTargetId        = ResolvePrimaryTargetId(),
                UserInitiatedSpellCast = true
            });
        }

        private uint ResolvePrimaryTargetId()
        {
            uint targetType = BaseInfo.TargetMechanics?.TargetType ?? 0u;

            if (targetType == TargetTypeSelfAoe)
                return Owner.Guid;

            if (targetType is not TargetTypeSingleTarget and not TargetTypeTargetAoe and not TargetTypeChain)
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
