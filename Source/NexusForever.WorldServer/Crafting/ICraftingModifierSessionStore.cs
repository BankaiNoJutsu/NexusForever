using System.Collections.Generic;

using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.WorldServer.Crafting
{
    public interface ICraftingModifierSessionStore
    {
        bool TryAddModifier(IPlayer player, uint additiveItem2Id, uint catalystItem2Id);

        void ClearModifiers(IPlayer player);

        bool TryBuildModifierItemCounts(
            IPlayer player,
            IGameTableManager gameTableManager,
            TradeskillSchematic2Entry schematic,
            out IReadOnlyDictionary<uint, uint> itemCounts,
            out string reason);
    }
}
