namespace NexusForever.Script.Template.Filter
{
    public class ScriptFilterSearch : IScriptFilterSearch
    {
        public Type ScriptType { get; private set; }
        public uint? Id { get; private set; }
        public uint? CreatureId { get; private set; }
        public uint? TargetGroupId { get; private set; }
        public ulong? ActivePropId { get; private set; }
        public List<string> ScriptNames { get; private set; }

        public IScriptFilterSearch FilterByScriptType<T>() where T : IScript
        {
            ScriptType = typeof(T);
            return this;
        }

        public IScriptFilterSearch FilterById(uint id)
        {
            if (id > 0u)
                Id = id;
            return this;
        }

        public IScriptFilterSearch FilterByCreatureId(uint id)
        {
            if (id > 0u)
                CreatureId = id;
            return this;
        }

        public IScriptFilterSearch FilterByTargetGroupId(uint id)
        {
            if (id > 0u)
                TargetGroupId = id;
            return this;
        }

        public IScriptFilterSearch FilterByActivePropId(ulong id)
        {
            if (id > 0u)
                ActivePropId = id;
            return this;
        }

        public IScriptFilterSearch FilterByScriptNames(List<string> scriptNames)
        {
            if (scriptNames != null && scriptNames.Count != 0)
                ScriptNames = scriptNames;
            return this;
        }
    }
}
