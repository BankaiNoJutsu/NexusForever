using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.Game.Entity;

/// <summary>Ephemeral, reference-owned bindings for the verified row499 dismiss surface.</summary>
public sealed class PetMiniBarManager
{
    private sealed class Binding
    {
        public ShortcutSet Set;
        public IWorldEntity Pet;
        public uint Guid;
        public IBaseMap Map;
        public ulong? Generation;
        public bool Closing;
    }

    private readonly IPlayer owner;
    private readonly IGameTableManager tables;
    private readonly object sync;
    private readonly Dictionary<ShortcutSet, Binding> bindings = new();

    public PetMiniBarManager(IPlayer owner, IGameTableManager tables)
    {
        this.owner = owner;
        this.tables = tables;
        sync = (owner as IPlayerMapAdmission)?.MapAdmissionSyncRoot ?? new object();
    }

    private bool CanSend() => owner.Session != null && owner.Guid != 0 && owner.Map != null
        && owner.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished)
        && owner.Session is not NetworkSession { IsDisconnecting: true }
        && (owner is not IPlayerMapAdmission admission
            || admission.IsMapAdmissionCurrent(admission.MapAdmissionGeneration));

    private static bool Supported(uint id) => id is 42682 or 59845 or 42683 or 59846
        or 42684 or 59847 or 42685 or 59848;

    private bool Current(Binding binding) => CanSend()
        && ReferenceEquals(owner.Map, binding.Map)
        && ReferenceEquals(binding.Map.GetEntity<IGridEntity>(owner.Guid), owner)
        && (binding.Generation == null || owner is IPlayerMapAdmission admission
            && admission.IsMapAdmissionCurrent(binding.Generation.Value))
        && binding.Pet.Guid == binding.Guid && binding.Guid != 0
        && binding.Pet.InWorld && ReferenceEquals(binding.Pet.Map, binding.Map)
        && binding.Pet.SummonerGuid == owner.Guid && Supported(binding.Pet.CreatureId)
        && ReferenceEquals(binding.Map.GetEntity<IGridEntity>(binding.Guid), binding.Pet)
        && owner.SummonFactory?.GetSummonCreatures(binding.Pet.CreatureId)
            ?.Any(pet => ReferenceEquals(pet, binding.Pet)) == true;

    private bool HasDismissRow()
    {
        ActionBarShortcutSetEntry row = tables?.ActionBarShortcutSet?.GetEntry(499);
        if (row?.Id != 499 || row.ShortcutType00 != 11 || row.ObjectId00 != 2)
            return false;
        uint[] types = [row.ShortcutType01, row.ShortcutType02, row.ShortcutType03,
            row.ShortcutType04, row.ShortcutType05, row.ShortcutType06, row.ShortcutType07,
            row.ShortcutType08, row.ShortcutType09, row.ShortcutType10, row.ShortcutType11];
        uint[] objects = [row.ObjectId01, row.ObjectId02, row.ObjectId03, row.ObjectId04,
            row.ObjectId05, row.ObjectId06, row.ObjectId07, row.ObjectId08, row.ObjectId09,
            row.ObjectId10, row.ObjectId11];
        return types.All(value => value == 0) && objects.All(value => value == 0);
    }

    public bool TryBind(IWorldEntity pet)
    {
        lock (sync)
        {
            if (pet == null || !HasDismissRow()) return false;
            Binding existing = bindings.Values.FirstOrDefault(value => ReferenceEquals(value.Pet, pet));
            if (existing != null) return !existing.Closing && Current(existing);
            var binding = new Binding { Pet = pet, Guid = pet.Guid, Map = owner.Map,
                Generation = (owner as IPlayerMapAdmission)?.MapAdmissionGeneration };
            if (!Current(binding)) return false;
            for (int index = 2; index <= 6; index++)
            {
                binding.Set = (ShortcutSet)index;
                if (bindings.ContainsKey(binding.Set)) continue;
                bindings.Add(binding.Set, binding);
                Send(binding.Set, 499, binding.Guid);
                return true;
            }
            return false;
        }
    }

    public bool TryDismiss(ShortcutSet shortcutSet, uint slotIndex)
    {
        lock (sync)
        {
            if ((uint)shortcutSet < 2 || (uint)shortcutSet > 6 || slotIndex != 0
                || owner.IsLoading || !HasDismissRow()
                || !bindings.TryGetValue(shortcutSet, out Binding binding)
                || binding.Closing || !Current(binding)) return false;
            // Closing reserves the index until the exact entity's removal callback.
            binding.Closing = true;
            Send(binding.Set, 0, 0);
            if (bindings.TryGetValue(shortcutSet, out Binding current)
                && ReferenceEquals(current, binding) && Current(binding))
                owner.SummonFactory.Unsummon(binding.Guid);
            return true;
        }
    }

    public void Unbind(IWorldEntity pet)
    {
        lock (sync)
        {
            Binding binding = bindings.Values.FirstOrDefault(value => ReferenceEquals(value.Pet, pet));
            if (binding == null) return;
            bindings.Remove(binding.Set);
            if (!binding.Closing && CanSend() && ReferenceEquals(owner.Map, binding.Map)
                && (binding.Generation == null || owner is IPlayerMapAdmission admission
                    && admission.IsMapAdmissionCurrent(binding.Generation.Value)))
                Send(binding.Set, 0, 0);
        }
    }

    public void Clear(bool sendPackets)
    {
        lock (sync)
        {
            ulong? generation = (owner as IPlayerMapAdmission)?.MapAdmissionGeneration;
            Binding[] previous = bindings.Values.ToArray();
            bindings.Clear();
            foreach (Binding binding in previous)
                if (sendPackets && !binding.Closing && CanSend()
                    && !bindings.ContainsKey(binding.Set)
                    && (generation == null || owner is IPlayerMapAdmission admission
                        && admission.IsMapAdmissionCurrent(generation.Value))
                    && ReferenceEquals(owner.Map, binding.Map))
                    Send(binding.Set, 0, 0);
        }
    }

    private void Send(ShortcutSet set, ushort row, uint guid) =>
        owner.Session.EnqueueMessageEncrypted(new ServerActionBarSet {
            ShortcutSet = set, ActionBarShortcutSetId = row, AssociatedUnitId = guid });
}
