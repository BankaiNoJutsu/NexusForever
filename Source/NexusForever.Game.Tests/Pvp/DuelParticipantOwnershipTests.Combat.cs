using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Pvp;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Message.Model.Pvp;
using NexusForever.WorldServer.Network.Message.Handler.Pvp;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Pvp;

public partial class DuelParticipantOwnershipTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ParsedRealPlayerDuelControlsActualCombatPermissionThroughOriginalMapAdmission(bool changeAdmission)
    {
        using var store = new Store(null);
        var f = new Fixture();
        using var first = new Actor(store, actorSession: f.First.Session, actorEvents: f.First.Session.Events,
            actorAccount: f.First.Account, actorModel: new CharacterModel { Id = 101 });
        using var second = new Actor(store, actorSession: f.Second.Session, actorEvents: f.Second.Session.Events,
            actorAccount: f.Second.Account, actorModel: new CharacterModel { Id = 202 });
        Prepare(first, f.First); Prepare(second, f.Second);
        first.Set(nameof(Player.TargetGuid), second.Player.Guid);
        ((IDictionary)typeof(GridEntity).GetField("visibleEntities", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(first.Player)!)
            .Add(second.Player.Guid, second.Player);
        Assert.False(first.Player.CanAttack(second.Player));
        new ClientDuelInitiateHandler(NullLogger<ClientDuelInitiateHandler>.Instance, f.Manager)
            .HandleMessage(f.First.Session, Empty<ClientDuelInitiate>());
        new ClientDuelAcceptHandler(NullLogger<ClientDuelAcceptHandler>.Instance, f.Manager)
            .HandleMessage(f.Second.Session, Empty<ClientDuelAccept>());
        Assert.False(first.Player.CanAttack(second.Player));
        f.Manager.Update(3);
        Assert.True(first.Player.CanAttack(second.Player)); Assert.True(second.Player.CanAttack(first.Player));
        var replacement = new Participant(second.Player.Guid, f.Map);
        Assert.False(first.Player.CanAttack(replacement.Player));
        if (changeAdmission)
        {
            ((IPlayerMapAdmission)second.Player).BeginMapAdmission();
            Assert.False(first.Player.CanAttack(second.Player));
            f.Manager.Update(0);
            Assert.Equal(DuelFinishReason.DuelCancelled, Assert.Single(f.First.Packets<ServerDuelResult>()).Reason);
        }
        else
        {
            new ClientDuelForfeitHandler(NullLogger<ClientDuelForfeitHandler>.Instance, f.Manager)
                .HandleMessage(f.Second.Session, Empty<ClientDuelForfeit>());
            var result = Assert.Single(f.First.Packets<ServerDuelResult>());
            Assert.Equal(DuelFinishReason.Forfeited2, result.Reason); Assert.Equal(101u, result.WinnerUnitId);
            Assert.False(first.Player.CanAttack(second.Player));
        }

        void Prepare(Actor actor, Participant owner)
        {
            actor.Set(nameof(Player.Guid), owner.Player.Guid); actor.Set(nameof(Player.Map), f.Map);
            owner.Session.Player = actor.Player;
            typeof(Player).GetField("duelManager", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(actor.Player, f.Manager);
            var aggro = typeof(UnitEntity).GetField("aggroImmuneStates", BindingFlags.NonPublic | BindingFlags.Instance)!;
            aggro.SetValue(actor.Player, Activator.CreateInstance(aggro.FieldType));
            var stats = (IDictionary)typeof(WorldEntity).GetField("stats", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(actor.Player)!;
            stats.Add(Stat.Health, new StatValue(Stat.Health, 100u));
            var visible = typeof(GridEntity).GetField("visibleEntities", BindingFlags.NonPublic | BindingFlags.Instance)!;
            visible.SetValue(actor.Player, Activator.CreateInstance(visible.FieldType));
        }
    }
}
