namespace NexusForever.Game.Static.Quest
{
    public enum TargetGroupType
    {
        CreatureIdGroup     = 1, // This is the majority of entries in TargetGroups TBL
        CreatureIdGroup2    = 2, // Unsure how this is used, possibly an exclusion list
        FactionIdGroup      = 3,
        NotFactionIdGroup   = 4, // This is used to say "Don't target this faction"
        PlayerRaceIdGroup   = 5,
        PlayerClassIdGroup  = 7,
        CreatureIdListGroup = 9, // This matches entity.CreatureId against any listed row
        OtherTargetGroup          = 10, // This is used to target other TargetGroup(s) data
        OtherTargetGroupCreatures = 11,
        CreatureRaceIdGroup       = 12, // This targets Creature2Entry.UnitRaceId
        NotCreatureRaceIdGroup    = 13
    }
}
