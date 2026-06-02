namespace NexusForever.Game.Static.Crafting
{
    /// <summary>
    /// Client context-tree service key ids resolved by <c>Crafting_GetStationServiceKeyForSchematic</c> (0x1405926a0).
    /// The native names for these service keys are still unmapped; keep them diagnostic.
    /// Crafting aux opcodes <c>0x084B</c>/<c>0855</c> emit intent remains blocked (see <c>ServerCraftingAux*</c> models).
    /// </summary>
    public static class CraftingStationServiceKey
    {
        public const uint DefaultSchematic          = 0x2C;
        public const uint RunecraftingTradeSkill    = 0x4F;
        public const uint TierZeroFlaggedSchematic  = 0x57;

        public const uint RunecraftingTradeSkillId = 22u;

        public static uint GetForSchematic(uint tradeSkillId, uint tier, uint flags)
        {
            if (tradeSkillId == RunecraftingTradeSkillId)
                return RunecraftingTradeSkill;

            if ((flags & 4u) != 0u && tier == 0u)
                return TierZeroFlaggedSchematic;

            return DefaultSchematic;
        }
    }
}
