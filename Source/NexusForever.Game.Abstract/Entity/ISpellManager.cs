using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Entity
{
    public interface ISpellManager : IDatabaseCharacter, IUpdate
    {
        /// <summary>
        /// Index of the active <see cref="IActionSet"/>.
        /// </summary>
        byte ActiveActionSet { get; }

        void GrantSpells();

        /// <summary>
        /// Returns <see cref="ICharacterSpell"/> for an existing spell.
        /// </summary>
        ICharacterSpell GetSpell(uint spell4BaseId);

        /// <summary>
        /// Returns <see cref="ICharacterSpell"/> for an existing spell entry id.
        /// </summary>
        ICharacterSpell GetSpellForSpell4Id(uint spell4Id);

        /// <summary>
        /// Track the currently active floating action bar spell shortcuts.
        /// </summary>
        void SetActiveFloatingActionBarShortcutSet(uint actionBarShortcutSetId, uint ownerSpell4Id);

        /// <summary>
        /// Returns whether the supplied spell is currently available from the active floating action bar.
        /// </summary>
        bool IsActiveFloatingActionBarSpell(uint spell4Id);

        /// <summary>
        /// Clear the active floating action bar shortcut set.
        /// </summary>
        void ClearActiveFloatingActionBarShortcutSet();

        /// <summary>
        /// Clear the active floating action bar shortcut set when its owner spell belongs to the supplied spell group.
        /// </summary>
        bool ClearActiveFloatingActionBarShortcutSetForSpellGroup(uint spellGroupId);

        /// <summary>
        /// Track an active pet command selector and the spell that should be cast when it is selected.
        /// </summary>
        void SetActivePetActionSpell(uint petSwitchSpell4Id, uint actionSpell4Id, uint ownerSpell4Id);

        /// <summary>
        /// Resolve an active pet command selector to the spell that should be cast.
        /// </summary>
        bool TryResolveActivePetActionSpell(uint selectedSpell4Id, out uint actionSpell4Id);

        /// <summary>
        /// Resolve the only currently active pet command action, when unambiguous.
        /// </summary>
        bool TryResolveSingleActivePetActionSpell(out uint actionSpell4Id);

        /// <summary>
        /// Clear active pet command selectors.
        /// </summary>
        void ClearActivePetActionSpells();

        /// <summary>
        /// Clear a single active pet command selector and resolved action spell.
        /// </summary>
        void ClearActivePetActionSpell(uint petSwitchSpell4Id, uint actionSpell4Id);

        /// <summary>
        /// Clear active pet command selectors when their owner spell belongs to the supplied spell group.
        /// </summary>
        bool ClearActivePetActionSpellsForSpellGroup(uint spellGroupId);

        /// <summary>
        /// Add a new <see cref="ICharacterSpell"/> created from supplied spell base id and tier.
        /// </summary>
        void AddSpell(uint spell4BaseId, byte tier = 1);

        /// <summary>
        /// Update existing <see cref="ICharacterSpell"/> with supplied tier. The base tier will be updated if no action set index is supplied.
        /// </summary>
        void UpdateSpell(uint spell4BaseId, byte tier, byte? actionSetIndex);

        /// <summary>
        /// Activate or deactivate an existing ability book spell.
        /// </summary>
        bool SetSpellActivation(uint spell4Id, bool active);

        /// <summary>
        /// Return the tier for supplied spell.
        /// This will either be the <see cref="IActionSetShortcut"/> tier if placed in the active <see cref="IActionSet"/> or base tier if not.
        /// </summary>
        byte GetSpellTier(uint spell4BaseId);

        List<ICharacterSpell> GetPets();

        /// <summary>
        /// Return spell cooldown for supplied spell id in seconds.
        /// </summary>
        double GetSpellCooldown(uint spellId);

        /// <summary>
        /// Set spell cooldown in seconds for supplied spell id.
        /// </summary>
        void SetSpellCooldown(uint spell4Id, double cooldown);

        void ResetAllSpellCooldowns();
        double GetGlobalSpellCooldown();
        void SetGlobalSpellCooldown(double cooldown);
        void SetGlobalSpellCooldown(uint cooldownId, double cooldown);

        /// <summary>
        /// Add bonus AMP power to all action sets.
        /// </summary>
        void AddAmpPower(ushort amount);

        /// <summary>
        /// Add bonus ability tier points to all action sets.
        /// </summary>
        void AddAbilityTierPoints(byte amount);

        /// <summary>
        /// Return <see cref="IActionSet"/> at supplied index.
        /// </summary>
        IActionSet GetActionSet(byte actionSetIndex);

        /// <summary>
        /// Update active <see cref="IActionSet"/> with supplied index, returned <see cref="SpecError"/> is sent to the client.
        /// </summary>
        SpecError SetActiveActionSet(byte value);

        void SendInitialPackets();
        void SendServerSpellList();
        void SendServerAbilityPoints();
    }
}
