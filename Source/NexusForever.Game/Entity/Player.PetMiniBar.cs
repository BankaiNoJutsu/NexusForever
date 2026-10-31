using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Entity;

public partial class Player : IPlayerPetMiniBar
{
    private PetMiniBarManager petMiniBars;
    private PetMiniBarManager PetMiniBars => System.Threading.LazyInitializer.EnsureInitialized(
        ref petMiniBars, () => new PetMiniBarManager(this, gameTableManager));

    public bool TryBindPetMiniBar(IWorldEntity pet) => PetMiniBars.TryBind(pet);
    public void UnbindPetMiniBar(IWorldEntity pet) => petMiniBars?.Unbind(pet);
    public bool TryDismissPetMiniBar(ShortcutSet shortcutSet, uint slotIndex) =>
        petMiniBars?.TryDismiss(shortcutSet, slotIndex) == true;

    private void ClearPetMiniBars(bool sendPackets) => petMiniBars?.Clear(sendPackets);
}
