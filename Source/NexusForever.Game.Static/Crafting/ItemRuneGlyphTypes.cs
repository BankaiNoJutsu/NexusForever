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

        // Category 175 glyphs: 352 Fire, 353 Water, 354 Earth, 355 Air, 356 Life, 357 Logic, 358 Fusion.
        public const uint Category175Id           = 175u;
        public const uint Category175TypeIdMin    = 352u;
        public const uint Category175TypeIdMax    = 358u;

        // Category 177 signs: 339 Fire, 340 Water, 341 Earth, 342 Air, 343 Life, 344 Logic, 345 Fusion.
        public const uint Category177Id           = 177u;
        public const uint Category177TypeIdMin    = 339u;
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
                    return TryGetCategory175RuneType(item2TypeId, out runeType);
                case Category177Id:
                    if (TryGetCategory177RuneType(item2TypeId, out runeType))
                        return true;

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

        private static bool TryGetCategory175RuneType(uint item2TypeId, out RuneType runeType)
        {
            runeType = item2TypeId switch
            {
                352u => RuneType.Fire,
                353u => RuneType.Water,
                354u => RuneType.Earth,
                355u => RuneType.Air,
                356u => RuneType.Life,
                357u => RuneType.Logic,
                358u => RuneType.Fusion,
                _    => default
            };
            return runeType != default;
        }

        private static bool TryGetCategory177RuneType(uint item2TypeId, out RuneType runeType)
        {
            runeType = item2TypeId switch
            {
                339u => RuneType.Fire,
                340u => RuneType.Water,
                341u => RuneType.Earth,
                342u => RuneType.Air,
                343u => RuneType.Life,
                344u => RuneType.Logic,
                345u => RuneType.Fusion,
                _    => default
            };
            return runeType != default;
        }

        /// <summary>Category 176 sigil band only (legacy helper).</summary>
        public static bool TryGetRuneTypeFromSigilItem2Type(uint item2TypeId, out RuneType runeType)
        {
            return TryGetRuneTypeFromRunecraftingGlyph(Category176Id, item2TypeId, out runeType);
        }
    }
}
