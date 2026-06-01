namespace NexusForever.Game.Static.Crafting
{
    /// <summary>
    /// Runecrafting glyph Item2 type-id bands correlated from <c>wildstar_client</c>
    /// (<c>Item2Category.tradeSkillId=22</c>). Client <c>RuneInstall_ValidateRuneMatchesSocket</c>
    /// 14040f1b0 gates gear item-eval <c>+0x114</c> against the target socket-type bit; tbl bands below
    /// supply glyph <c>RuneType</c> for NF element match (not present in the exported 14040f1b0 body).
    /// </summary>
    public static class ItemRuneGlyphTypes
    {
        public const uint RunecraftingTradeSkillId = (uint)TradeskillType.Runecrafting;

        // Category 176 sigils: item2TypeId 419–424 => RuneType 7–12 (Air..Life).
        public const uint Category176Id           = 176u;
        public const uint Category176TypeIdBase   = 412u;
        public const uint Category176TypeIdMin    = 419u;
        public const uint Category176TypeIdMax    = 424u;

        // Category 175 glyphs: item2TypeId 352–358 => RuneType 7–13 (Air..Fusion).
        public const uint Category175Id           = 175u;
        public const uint Category175TypeIdBase   = 345u;
        public const uint Category175TypeIdMin    = 352u;
        public const uint Category175TypeIdMax    = 358u;

        // Category 177 elemental band: item2TypeId 340–345 => RuneType 7–12 (Air..Life).
        public const uint Category177Id           = 177u;
        public const uint Category177TypeIdBase   = 333u;
        public const uint Category177TypeIdMin    = 340u;
        public const uint Category177TypeIdMax    = 345u;

        // Category 177 fusion-tier materials: types 515/516/530/563 (and empty 514/517) in wildstar_client;
        // each satisfies item2TypeId - 13 in {502,503,517,550} — correlated, not client-decompiled.
        public const uint Category177FusionTypeIdMin = 502u;

        public static bool TryGetRuneTypeFromRunecraftingGlyph(uint item2CategoryId, uint item2TypeId, out RuneType runeType)
        {
            uint typeBase;
            uint typeMin;
            uint typeMax;

            switch (item2CategoryId)
            {
                case Category176Id:
                    typeBase = Category176TypeIdBase;
                    typeMin  = Category176TypeIdMin;
                    typeMax  = Category176TypeIdMax;
                    break;
                case Category175Id:
                    typeBase = Category175TypeIdBase;
                    typeMin  = Category175TypeIdMin;
                    typeMax  = Category175TypeIdMax;
                    break;
                case Category177Id:
                    if (item2TypeId >= Category177TypeIdMin && item2TypeId <= Category177TypeIdMax)
                    {
                        typeBase = Category177TypeIdBase;
                        typeMin  = Category177TypeIdMin;
                        typeMax  = Category177TypeIdMax;
                        break;
                    }

                    if (item2TypeId >= Category177FusionTypeIdMin)
                    {
                        runeType = RuneType.Fusion;
                        return true;
                    }

                    runeType = default;
                    return false;
                default:
                    runeType = default;
                    return false;
            }

            if (item2TypeId < typeMin || item2TypeId > typeMax)
            {
                runeType = default;
                return false;
            }

            runeType = (RuneType)(item2TypeId - typeBase);
            return runeType >= RuneType.Air && runeType <= RuneType.Fusion;
        }

        /// <summary>Category 176 sigil band only (legacy helper).</summary>
        public static bool TryGetRuneTypeFromSigilItem2Type(uint item2TypeId, out RuneType runeType)
        {
            return TryGetRuneTypeFromRunecraftingGlyph(Category176Id, item2TypeId, out runeType);
        }
    }
}
