using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Main.Quests.NorthernWilds;

namespace NexusForever.Game.Tests.PublicEvents;

public class DominionUltrabotPublicEventScriptTests
{
    [Fact]
    public void OnDeath_WithDominionUltrabot_CreditsDefeatObjective()
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out RecordingDispatchProxy<IPublicEvent> publicEventProxy);
        IUnitEntity ultrabot = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> ultrabotProxy);
        ultrabotProxy.SetProperty(nameof(IUnitEntity.CreatureId), 12526u);

        var script = new DominionUltrabotPublicEventScript();
        script.OnLoad(publicEvent);

        script.OnDeath(ultrabot);

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(371u, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
    }

    [Fact]
    public void OnDeath_WithOtherCreature_DoesNotCreditObjective()
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out RecordingDispatchProxy<IPublicEvent> publicEventProxy);
        IUnitEntity otherCreature = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> otherCreatureProxy);
        otherCreatureProxy.SetProperty(nameof(IUnitEntity.CreatureId), 11070u);

        var script = new DominionUltrabotPublicEventScript();
        script.OnLoad(publicEvent);

        script.OnDeath(otherCreature);

        Assert.Empty(publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
    }
}
