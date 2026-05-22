namespace NexusForever.Game.Retail
{
    /// <summary>
    /// Build 16042 deserter debuff Spell4 ids from <c>wildstar_client.Spell4</c> (description match).
    /// </summary>
    public static class RetailMatchingDeserterSpells
    {
        public const uint PvpDeserterSpell4Id = 45444u;

        public const uint PveDeserterTier1Spell4Id = 45453u;
        public const uint PveDeserterTier2Spell4Id = 45553u;
        public const uint PveDeserterTier3Spell4Id = 45554u;

        public static uint GetPveDeserterSpell4Id(double completionRatio)
        {
            double scale = Math.Clamp(1d - completionRatio, 0d, 1d);
            if (scale > 0.66d)
                return PveDeserterTier3Spell4Id;
            if (scale > 0.33d)
                return PveDeserterTier2Spell4Id;
            return PveDeserterTier1Spell4Id;
        }
    }
}
