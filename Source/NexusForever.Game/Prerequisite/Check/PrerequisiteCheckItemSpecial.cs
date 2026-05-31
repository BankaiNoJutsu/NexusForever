using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    [PrerequisiteCheck(PrerequisiteType.ItemSpecial)]
    public class PrerequisiteCheckItemSpecial : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (parameters.Item == null)
                return false;

            // Client Prerequisite_CheckItemSpecial 1404a0dd0: item-eval +0x114/+0x118 both zero => Equal fails.
            // NF proxy until runtime special-slot fields are stored on IItem.
            bool hasSpecial = parameters.Item.Info.Entry.ItemSpecialId00 != 0
                || parameters.Item.MicrochipIds.Count > 0;

            return comparison switch
            {
                PrerequisiteComparison.Equal    => hasSpecial,
                PrerequisiteComparison.NotEqual => !hasSpecial,
                _                               => false
            };
        }
    }
}
