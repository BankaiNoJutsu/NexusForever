using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Tests.Network;

public class PlayerWorldEntryOrderingTests
{
    [Fact]
    public void OnEnteredWorld_SendsPathAndQuestRuntimeAfterPlayerEnteredWorld()
    {
        var order = new List<string>();
        Player player = CreatePlayer(order);

        player.OnEnteredWorld();

        Assert.Equal(
            [
                nameof(ServerPlayerEnteredWorld),
                nameof(IPathManager.SendInitialPackets),
                nameof(IQuestManager.SendInitialPackets)
            ],
            order.Take(3));
    }

    private static Player CreatePlayer(List<string> order)
    {
        Player player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IGameSession.Events), new EventQueue());
        sessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args is [IWritable message])
                order.Add(message.GetType().Name);

            return null;
        });

        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out RecordingDispatchProxy<IPathManager> pathManagerProxy);
        pathManagerProxy.SetMethodHandler(nameof(IPathManager.SendInitialPackets), _ =>
        {
            order.Add(nameof(IPathManager.SendInitialPackets));
            return null;
        });

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.SendInitialPackets), _ =>
        {
            order.Add(nameof(IQuestManager.SendInitialPackets));
            return null;
        });

        SetField(player, typeof(GridEntity), "visibleEntities", new Dictionary<uint, IGridEntity>());
        SetBackingField(player, nameof(Player.Session), session);
        SetBackingField(player, nameof(Player.PathManager), pathManager);
        SetBackingField(player, nameof(Player.QuestManager), questManager);
        player.IsLoading = true;

        return player;
    }

    private static void SetBackingField<T>(Player player, string propertyName, T value)
    {
        SetField(player, typeof(Player), $"<{propertyName}>k__BackingField", value);
    }

    private static void SetField<T>(Player player, Type declaringType, string fieldName, T value)
    {
        FieldInfo field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {declaringType.Name}.{fieldName}.");

        field.SetValue(player, value);
    }
}
