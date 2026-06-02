using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Static.Prerequisite;

namespace NexusForever.Game.Prerequisite.Check
{
    /// <summary>
    /// Prerequisite type 295: live <c>PrerequisiteManager</c> case <c>0x127</c> returns true only when the
    /// evaluated entity pointer is non-null and comparison is <see cref="PrerequisiteComparison.NotEqual"/>;
    /// <c>value0</c> is ignored. Retail row <c>44550</c> is unreferenced and paired with type 292 in tbl only.
    /// </summary>
    [PrerequisiteCheck(PrerequisiteType.Unknown295)]
    public class PrerequisiteCheckUnknown295 : IPrerequisiteCheck
    {
        public bool Meets(IPlayer player, PrerequisiteComparison comparison, uint value, uint objectId, IPrerequisiteParameters parameters)
        {
            if (comparison != PrerequisiteComparison.NotEqual)
                return false;

            return parameters.Target is IUnitEntity;
        }
    }
}
