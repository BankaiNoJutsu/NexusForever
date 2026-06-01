namespace NexusForever.GameTable.Model
{
    public class ItemSpecialEntry
    {
        public uint Id;
        public uint PrerequisiteIdGeneric00;
        public uint LocalizedTextIdName;
        /// <summary>
        /// Client ItemSpecial row offset <c>+0x10</c> (copied to item-eval <c>+0x114</c> by 14040c310).
        /// Reference SQL treats this as Spell4 FK; only values in the rune-socket bit set (1/2/4/8/10/20/40) are socket masks.
        /// </summary>
        public uint Spell4IdOnEquip;
        public uint Spell4IdOnActivate;
    }
}
