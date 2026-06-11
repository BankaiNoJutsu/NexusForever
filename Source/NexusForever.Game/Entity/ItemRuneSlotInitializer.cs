using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    internal static class ItemRuneSlotInitializer
    {
        public static void ApplyDefaultSockets(IItem item, IGameTableManager gameTableManager = null)
        {
            if (item.Info == null)
                return;

            if (item.RuneSlots.Count > 0)
                return;

            uint instanceId = item.Info.Entry.ItemRuneInstanceId;
            if (instanceId == 0u)
                return;

            if (gameTableManager?.ItemRuneInstance == null)
                return;

            ItemRuneInstanceEntry instance = gameTableManager.ItemRuneInstance.GetEntry(instanceId);
            if (instance == null || instance.DefinedSocketCount == 0u)
                return;

            uint[] definedTypes =
            [
                instance.DefinedSocketType00,
                instance.DefinedSocketType01,
                instance.DefinedSocketType02,
                instance.DefinedSocketType03,
                instance.DefinedSocketType04,
                instance.DefinedSocketType05,
                instance.DefinedSocketType06,
                instance.DefinedSocketType07,
            ];

            int socketCount = (int)Math.Min(instance.DefinedSocketCount, definedTypes.Length);
            for (int i = 0; i < socketCount; i++)
            {
                if (!ItemRuneSocketTypes.TryToRuneType(definedTypes[i], out RuneType runeType))
                    continue;

                item.RuneSlots.Add(new ItemRuneSlot(runeType));
            }

            if (item.RuneSlots.Count > 0)
                item.TouchRuneSlots();
        }
    }
}
