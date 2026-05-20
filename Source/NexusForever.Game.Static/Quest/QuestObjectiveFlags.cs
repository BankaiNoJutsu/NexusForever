namespace NexusForever.Game.Static.Quest
{
    [Flags]
    public enum QuestObjectiveFlags
    {
        None                      = 0x0000,
        RequiresPreviousObjectives = 0x0001,
        Sequential                = 0x0002,
        Hidden                    = 0x0008,
        Optional                  = 0x0020,
        DisablesDynamicProgress   = 0x0200
    }
}
