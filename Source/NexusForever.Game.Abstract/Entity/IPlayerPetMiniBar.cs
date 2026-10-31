using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Abstract.Entity;

/// <summary>Optional owner of concrete summoned-pet dismiss shortcut bindings.</summary>
public interface IPlayerPetMiniBar
{
    bool TryBindPetMiniBar(IWorldEntity pet);
    void UnbindPetMiniBar(IWorldEntity pet);
    bool TryDismissPetMiniBar(ShortcutSet shortcutSet, uint slotIndex);
}
