using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity;

namespace NexusForever.Game.Tests.Entity;

public class ActivationInteractionGuardsTests
{
    [Fact]
    public void TryRejectOutOfRangeTarget_WithNonUnitDisplayPaddingWithinEffectiveRange_DoesNotReject()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> entityProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        playerProxy.SetProperty(nameof(IGridEntity.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IUnitEntity.HitRadius), 1f);

        entityProxy.SetProperty(nameof(IGridEntity.Position), new Vector3(5.75f, 0f, 0f));
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry
        {
            ActivateSpellMaxRange = 5f,
            ModelScale = 1f
        });
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureDisplayEntry), new Creature2DisplayInfoEntry
        {
            HitRadius = 1f
        });

        bool rejected = ActivationInteractionGuards.TryRejectOutOfRangeTarget(session, entity);

        Assert.False(rejected);
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
    }

    [Fact]
    public void TryRejectOutOfRangeTarget_WithNonUnitDisplayPaddingBeyondEffectiveRange_Rejects()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> entityProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        playerProxy.SetProperty(nameof(IGridEntity.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IUnitEntity.HitRadius), 1f);

        entityProxy.SetProperty(nameof(IGridEntity.Position), new Vector3(6.1f, 0f, 0f));
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry
        {
            ActivateSpellMaxRange = 5f,
            ModelScale = 1f
        });
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureDisplayEntry), new Creature2DisplayInfoEntry
        {
            HitRadius = 1f
        });

        bool rejected = ActivationInteractionGuards.TryRejectOutOfRangeTarget(session, entity);

        Assert.True(rejected);
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
    }
}
