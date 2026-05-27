using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Abstract.Story;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Housing;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Housing;

public class HousingBranchScriptTests
{
    [Theory]
    [InlineData(54400u, 2291u)]
    [InlineData(65296u, 2291u)]
    [InlineData(54401u, 2294u)]
    [InlineData(65297u, 2294u)]
    [InlineData(54403u, 2292u)]
    [InlineData(65298u, 2292u)]
    [InlineData(54404u, 2293u)]
    [InlineData(65299u, 2293u)]
    public void CreatureStoryPanelIds_ContainsBranchHousingIntroMappings(uint creatureId, uint storyPanelId)
    {
        Assert.True(HousingIntroEntityScript.CreatureStoryPanelIds.TryGetValue(creatureId, out uint actualStoryPanelId));
        Assert.Equal(storyPanelId, actualStoryPanelId);
    }

    [Fact]
    public void HousingIntro_OnActivateSuccess_SendsMappedStoryPanel()
    {
        IStoryBuilder storyBuilder = RecordingDispatchProxy<IStoryBuilder>.Create(out RecordingDispatchProxy<IStoryBuilder> storyBuilderProxy);
        IPlayer player = CreatePlayer(out _);

        var script = new HousingIntroEntityScript(
            storyBuilder,
            NullLogger<HousingIntroEntityScript>.Instance);
        script.OnLoad(CreateOwner(65298u));

        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IStoryBuilder>.Invocation invocation = Assert.Single(storyBuilderProxy.GetInvocations(nameof(IStoryBuilder.SendServerStoryPanelShow)));
        Assert.Same(player, invocation.Arguments[0]);
        Assert.Equal(2292u, (uint)invocation.Arguments[1]);
    }

    [Fact]
    public void HousingPortal_OnActivateSuccess_GrantsMissingHousingSpellsAndCastsDialog()
    {
        ICharacterSpell existingSpell = RecordingDispatchProxy<ICharacterSpell>.Create(out _);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);
        spellManagerProxy.SetMethodHandler(nameof(ISpellManager.GetSpell), args =>
            (uint)args[0] == 25520u ? existingSpell : null);

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        ISpellParameters spellParameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> spellParametersProxy);
        IFactory<ISpellParameters> spellParametersFactory = CreateSpellParametersFactory(spellParameters);

        var script = new HousingPortalEntityScript(
            spellParametersFactory,
            NullLogger<HousingPortalEntityScript>.Instance);
        script.OnLoad(CreateOwner(26350u));

        script.OnActivateSuccess(player);

        RecordingDispatchProxy<ISpellManager>.Invocation addSpellInvocation = Assert.Single(spellManagerProxy.GetInvocations(nameof(ISpellManager.AddSpell)));
        Assert.Equal(22919u, (uint)addSpellInvocation.Arguments[0]);

        RecordingDispatchProxy<IPlayer>.Invocation castInvocation = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
        Assert.Equal(39111u, (uint)castInvocation.Arguments[0]);
        Assert.Same(spellParameters, castInvocation.Arguments[1]);

        Assert.Contains(spellParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.PrimaryTargetId)), i => (uint)i.Arguments[0] == 42u);
        Assert.Contains(spellParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.IgnoreGlobalCooldown)), i => (bool)i.Arguments[0]);
        Assert.Contains(spellParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.CancelActiveTrade)), i => (bool)i.Arguments[0]);
        Assert.Contains(spellParametersProxy.GetInvocations("set_" + nameof(ISpellParameters.ClientRequestSource)), i => (string)i.Arguments[0] == nameof(HousingPortalEntityScript));
    }

    [Fact]
    public void HousingPortal_HousingTrainingSpellBaseIds_MatchesBranchTrainingSet()
    {
        Assert.Equal([22919u, 25520u], HousingPortalEntityScript.HousingTrainingSpellBaseIds);
    }

    private static ICreatureEntity CreateOwner(uint creatureId)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        ownerProxy.SetProperty(nameof(IWorldEntity.Guid), 900u);
        ownerProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        return owner;
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        return player;
    }

    private static IFactory<ISpellParameters> CreateSpellParametersFactory(ISpellParameters spellParameters)
    {
        IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
        factoryProxy.SetMethodReturn(nameof(IFactory<ISpellParameters>.Resolve), spellParameters);
        return factory;
    }
}
