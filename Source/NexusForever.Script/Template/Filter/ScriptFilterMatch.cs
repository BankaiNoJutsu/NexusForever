namespace NexusForever.Script.Template.Filter
{
    public class ScriptFilterMatch : IScriptFilterMatch
    {
        /// <summary>
        /// Returns if <see cref="IScriptFilterSearch"/> can match with <see cref="IScriptFilterParameters"/>.
        /// </summary>
        public bool Match(IScriptFilterSearch search, IScriptFilterParameters script)
        {
            if (search.ScriptType != null && !search.ScriptType.IsAssignableFrom(script.ScriptType))
                return false;

            if (search.Id != null && script.Id != null
                && !script.Id.Contains(search.Id.Value))
                return false;

            if (search.CreatureId != null && script.CreatureId != null
                && !script.CreatureId.Contains(search.CreatureId.Value))
                return false;

            if (script.ActivePropId != null)
            {
                if (search.ActivePropId == null)
                    return false;

                if (!script.ActivePropId.Contains(search.ActivePropId.Value))
                    return false;
            }

            if (search.ScriptNames != null)
            {
                if (script.ScriptName != null && !search.ScriptNames.Contains(script.ScriptName))
                    return false;
            }
            else if (script.ScriptName != null)
                return false;

            return true;
        }
    }
}
