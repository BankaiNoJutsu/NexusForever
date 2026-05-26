using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Script;

public class ScriptFilterMatchTests
{
    [Fact]
    public void Match_WithEntityScriptNames_StillLoadsGenericScripts()
    {
        Assert.True(Matches<GenericScript>(new ScriptFilterSearch()
            .FilterByScriptType<IScript>()
            .FilterByScriptNames(["NamedScript"])));
    }

    [Fact]
    public void Match_WithEntityScriptNames_LoadsMatchingNamedScript()
    {
        Assert.True(Matches<NamedScript>(new ScriptFilterSearch()
            .FilterByScriptType<IScript>()
            .FilterByScriptNames(["NamedScript"])));
    }

    [Fact]
    public void Match_WithEntityScriptNames_RejectsDifferentNamedScript()
    {
        Assert.False(Matches<DifferentNamedScript>(new ScriptFilterSearch()
            .FilterByScriptType<IScript>()
            .FilterByScriptNames(["NamedScript"])));
    }

    [Fact]
    public void Match_WithEntityScriptNames_StillHonoursCreatureFilters()
    {
        Assert.True(Matches<CreatureSpecificScript>(new ScriptFilterSearch()
            .FilterByScriptType<IScript>()
            .FilterByCreatureId(42u)
            .FilterByScriptNames(["NamedScript"])));

        Assert.False(Matches<CreatureSpecificScript>(new ScriptFilterSearch()
            .FilterByScriptType<IScript>()
            .FilterByCreatureId(99u)
            .FilterByScriptNames(["NamedScript"])));
    }

    private static bool Matches<TScript>(IScriptFilterSearch search)
        where TScript : IScript
    {
        var parameters = new ScriptFilterParameters(null);
        parameters.Initialise(typeof(TScript));

        return new ScriptFilterMatch().Match(search, parameters);
    }

    private sealed class GenericScript : IScript
    {
    }

    [ScriptFilterScriptName("NamedScript")]
    private sealed class NamedScript : IScript
    {
    }

    [ScriptFilterScriptName("DifferentNamedScript")]
    private sealed class DifferentNamedScript : IScript
    {
    }

    [ScriptFilterCreatureId(42u)]
    private sealed class CreatureSpecificScript : IScript
    {
    }
}
