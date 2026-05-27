using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance.Expedition.RageLogic;

namespace NexusForever.Game.Tests.Instances;

public class RageLogicEventScriptTests
{
    [Fact]
    public void OnLoad_SetsWipVehicleChoicePhase()
    {
        var script = new RageLogicEventScript();
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ChooseAVehicle, invocation.Arguments[0]);
    }
}
