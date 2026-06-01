using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite
{
    internal static class PrerequisiteItemTypes
    {
        public static bool RequiresItemContext(PrerequisiteType type)
        {
            return type switch
            {
                >= PrerequisiteType.ItemStatData and <= PrerequisiteType.ItemRolledPropertyValue => true,
                PrerequisiteType.ItemIsSelfCraftedWeapon => true,
                PrerequisiteType.ItemTradeSkillLevel     => true,
                _                                      => false
            };
        }
    }
}
