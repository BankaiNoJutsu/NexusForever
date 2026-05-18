namespace NexusForever.Game.Spell
{
    public static class SettlerCampfireSpell
    {
        private static readonly uint[] BackInActionSpell4Ids =
        [
            32766u,
            32777u,
            32778u
        ];

        public static bool TryGetBackInActionSpell4Id(uint tierIndex, out uint spell4Id)
        {
            if (tierIndex < BackInActionSpell4Ids.Length)
            {
                spell4Id = BackInActionSpell4Ids[tierIndex];
                return true;
            }

            spell4Id = 0u;
            return false;
        }
    }
}
