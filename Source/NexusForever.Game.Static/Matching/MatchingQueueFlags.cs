namespace NexusForever.Game.Static.Matching
{
    [Flags]
    public enum MatchingQueueFlags
    {
        None            = 0x00,
        GroupIsQueued   = 0x04,
        AsMercenary     = 0x20,
        SoloMatch       = 0x80,                      
        Veteran         = 0x100,
        /// <summary>Group Finder "My Realm Only" (winter beta); verify against client 16042 if mismatched.</summary>
        RealmOnly       = 0x200,
    }
}
