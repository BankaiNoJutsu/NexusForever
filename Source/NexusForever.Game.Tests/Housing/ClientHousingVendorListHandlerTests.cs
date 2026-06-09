using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NexusForever.Game.Tests.Housing;

public class ClientHousingVendorListHandlerTests
{
    [Fact]
    public void HandleMessage_WhenHousingPlugItemTableMissing_SendsEmptyVendorList()
    {
        ClientHousingVendorListHandler handler = CreateHandler(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.HousingPlugItem), null);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);

        handler.HandleMessage(session, new ClientHousingVendorList());

        ServerHousingVendorList result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingVendorList>());
        Assert.Equal(0, result.ListType);
        Assert.Empty(result.PlugItems);
    }

    [Fact]
    public void HandleMessage_WhenContributionInfoTableMissing_SendsPlugWithZeroCost()
    {
        ClientHousingVendorListHandler handler = CreateHandler(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.HousingPlugItem), CreateGameTable(new HousingPlugItemEntry
        {
            Id                          = 77u,
            Flags                       = 0x20u,
            HousingContributionInfoId00 = 12u
        }));
        gameTableProxy.SetProperty(nameof(IGameTableManager.HousingContributionInfo), null);

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);

        handler.HandleMessage(session, new ClientHousingVendorList());

        ServerHousingVendorList result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingVendorList>());
        ServerHousingVendorList.PlugItem plug = Assert.Single(result.PlugItems);
        Assert.Equal(77u, plug.PlugItemId);
        Assert.Equal(0u, plug.Cost);
        Assert.Equal(0x20u, plug.PlugItemFlags);
    }

    [Fact]
    public void HandleMessage_WithContributionInfo_UsesFirstPositiveContributionCost()
    {
        ClientHousingVendorListHandler handler = CreateHandler(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        gameTableProxy.SetProperty(nameof(IGameTableManager.HousingPlugItem), CreateGameTable(new HousingPlugItemEntry
        {
            Id                          = 88u,
            Flags                       = 0x40u,
            HousingContributionInfoId00 = 12u,
            HousingContributionInfoId01 = 13u
        }));
        gameTableProxy.SetProperty(nameof(IGameTableManager.HousingContributionInfo), CreateGameTable(
            new HousingContributionInfoEntry
            {
                Id = 12u
            },
            new HousingContributionInfoEntry
            {
                Id                           = 13u,
                ContributionPointRequirement = 450u
            }));

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);

        handler.HandleMessage(session, new ClientHousingVendorList());

        ServerHousingVendorList.PlugItem plug = Assert.Single(
            Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingVendorList>()).PlugItems);
        Assert.Equal(88u, plug.PlugItemId);
        Assert.Equal(450u, plug.Cost);
        Assert.Equal(0x40u, plug.PlugItemFlags);
    }

    private static ClientHousingVendorListHandler CreateHandler(out RecordingDispatchProxy<IGameTableManager> gameTableProxy)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out gameTableProxy);
        return new ClientHousingVendorListHandler(gameTableManager);
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
