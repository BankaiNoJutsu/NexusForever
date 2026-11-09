using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Template.Event;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Boss-independent tests for the timed NPC yell script event (Phase A of
    /// INSTANCE_ENCOUNTER_VIDEO_GOAL_PROMPT.md).
    /// </summary>
    public class EntityYellEventTests
    {
        [Fact]
        public void InvokeYellsTheSuppliedMessageAtDefaultRange()
        {
            IWorldEntity owner = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> proxy);

            var yell = new EntityYellEvent();
            yell.Initialise(owner, "Boss line");
            yell.Invoke();

            var calls = proxy.GetInvocations(nameof(IWorldEntity.NpcYell));
            Assert.Single(calls);
            Assert.Equal("Boss line", calls[0].Arguments[0]);
        }

        [Fact]
        public void EventManagerInvokesTheYellOnlyAfterItsDelay()
        {
            IWorldEntity owner = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> proxy);
            IScriptEventManager manager = new ScriptEventManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<IScriptEventManager>.Instance);

            var yell = new EntityYellEvent();
            yell.Initialise(owner, "Delayed line");
            manager.EnqueueEvent(TimeSpan.FromSeconds(5), yell);

            manager.Update(4d);
            Assert.Empty(proxy.GetInvocations(nameof(IWorldEntity.NpcYell)));

            manager.Update(1.5d);
            Assert.Single(proxy.GetInvocations(nameof(IWorldEntity.NpcYell)));
        }

        [Fact]
        public void CancelledYellNeverFires()
        {
            IWorldEntity owner = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> proxy);
            IScriptEventManager manager = new ScriptEventManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<IScriptEventManager>.Instance);

            var yell = new EntityYellEvent();
            yell.Initialise(owner, "Never");
            manager.EnqueueEvent(TimeSpan.FromSeconds(1), yell);
            manager.CancelEvents<IEntityYellEvent>();

            manager.Update(5d);
            Assert.Empty(proxy.GetInvocations(nameof(IWorldEntity.NpcYell)));
        }
    }
}
