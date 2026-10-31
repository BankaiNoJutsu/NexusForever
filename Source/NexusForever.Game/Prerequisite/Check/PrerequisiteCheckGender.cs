using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.Gender)]
    public class PrerequisiteCheckGender : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (player == null)
                return false;

            // Compare the full table value; narrowing it to the byte-backed Sex
            // enum would make e.g. 256 incorrectly match Male (0).
            return comparison switch
            {
                PrerequisiteComparison.Equal => (uint)player.Sex == value,
                PrerequisiteComparison.NotEqual => (uint)player.Sex != value,
                _ => false
            };
        }
    }
}
