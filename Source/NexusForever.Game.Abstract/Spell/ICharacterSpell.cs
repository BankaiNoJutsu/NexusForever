using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Spell
{
    public interface ICharacterSpell : IDatabaseCharacter, IUpdate
    {
        IPlayer Owner { get; }
        ISpellBaseInfo BaseInfo { get; }
        ISpellInfo SpellInfo { get; }
        IItem Item { get; }
        byte Tier { get; set; }
        uint AbilityCharges { get; }
        uint MaxAbilityCharges { get; }
        double AbilityRechargeTimeRemaining { get; }
        double AbilityRechargePercentRemaining { get; }

        /// <summary>
        /// Return the concrete spell tier to cast for this request.
        /// </summary>
        ISpellInfo GetSpellInfoForCast();

        /// <summary>
        /// Commit any pending per-cast spell state after the cast result is known.
        /// </summary>
        void CompleteSpellInfoCast(ISpellInfo spellInfo, CastResult castResult);

        /// <summary>
        /// Used for when the client does not have continuous casting enabled
        /// </summary>
        void Cast(string clientRequestSource = null);

        /// <summary>
        /// Used for continuous casting when the client has it enabled, or spells with Cast Methods like ChargeRelease
        /// </summary>
        void Cast(bool buttonPressed, string clientRequestSource = null);

        /// <summary>
        /// Used for continuous casting when the client supplied a resolved primary target.
        /// </summary>
        void Cast(bool buttonPressed, uint primaryTargetId, uint clientContextToken = 0u, string clientRequestSource = null);

        void UseCharge();
        void SetAbilityCharges(uint charges);
        void ModifyAbilityCharges(int delta);
    }
}
