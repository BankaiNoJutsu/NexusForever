using NexusForever.Database;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Tests.Account.Inventory;

public class StorePurchaseHistoryManagerTests
{
    [Fact]
    public void ToClientPurchaseHistoryDate_EncodesDaysSinceWindowsEpoch()
    {
        var purchasedUtc = new DateTime(2026, 6, 10, 14, 30, 0, DateTimeKind.Utc);
        uint encodedDate = StorePurchaseHistoryManager.ToClientPurchaseHistoryDate(purchasedUtc);

        uint expectedDays = (uint)(purchasedUtc.Date - new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;
        Assert.Equal(expectedDays, encodedDate);
        Assert.True(encodedDate > 0u);
    }

    [Fact]
    public void ToClientPurchaseHistoryDate_EpochOrOlder_ReturnsZero()
    {
        Assert.Equal(0u, StorePurchaseHistoryManager.ToClientPurchaseHistoryDate(new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(0u, StorePurchaseHistoryManager.ToClientPurchaseHistoryDate(new DateTime(1600, 12, 31, 23, 59, 59, DateTimeKind.Utc)));
    }

    [Fact]
    public void SendPurchaseHistory_WithoutAuthDatabase_SendsEmptyReadyPacket()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), 1u);
        accountProxy.SetProperty(nameof(IAccount.Session), session);

        StorePurchaseHistoryManager.SendPurchaseHistory(account);

        IReadOnlyList<ServerStorePurchaseHistoryReady> messages = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<ServerStorePurchaseHistoryReady>()
            .ToList();

        ServerStorePurchaseHistoryReady message = Assert.Single(messages);
        Assert.Empty(message.Rows);
    }

    [Fact]
    public void SendPurchaseHistory_WithUnavailableDatabaseManager_SendsEmptyReadyPacket()
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> sessionProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        IDatabaseManager databaseManager = RecordingDispatchProxy<IDatabaseManager>.Create(out RecordingDispatchProxy<IDatabaseManager> databaseProxy);
        accountProxy.SetProperty(nameof(IAccount.Id), 1u);
        accountProxy.SetProperty(nameof(IAccount.Session), session);
        databaseProxy.SetMethodHandler(nameof(IDatabaseManager.GetDatabase), _ => throw new InvalidOperationException("Auth database is not ready."));

        StorePurchaseHistoryManager.SendPurchaseHistory(account, databaseManager);

        IReadOnlyList<ServerStorePurchaseHistoryReady> messages = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<ServerStorePurchaseHistoryReady>()
            .ToList();

        ServerStorePurchaseHistoryReady message = Assert.Single(messages);
        Assert.Empty(message.Rows);
    }
}
