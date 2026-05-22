using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Storefront;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Account;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Account.Inventory;

public class StorefrontPurchaseHandlerTests
{
    [Fact]
    public void CharacterPurchase_WithMismatchedTarget_ReturnsStoreError()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientStorefrontPurchaseCharacterHandler(
            NullLogger<ClientStorefrontPurchaseCharacterHandler>.Instance,
            null);

        ClientStorefrontPurchaseCharacter purchase = ReadCharacterPurchase(
            offerId: 77u,
            currencyId: AccountCurrencyType.NCoin,
            target: new NetworkIdentity
            {
                RealmId = 1,
                Id      = 999ul
            });

        handler.HandleMessage(session, purchase);

        ServerStoreError error = Assert.Single(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Equal(StoreError.GenericFail, error.Error);
    }

    [Fact]
    public void CharacterPurchase_WithUnknownOffer_ReturnsInvalidOfferStoreError()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IGlobalStorefrontManager storefrontManager = RecordingDispatchProxy<IGlobalStorefrontManager>.Create(out _);
        var handler = new ClientStorefrontPurchaseCharacterHandler(
            NullLogger<ClientStorefrontPurchaseCharacterHandler>.Instance,
            storefrontManager);

        ClientStorefrontPurchaseCharacter purchase = ReadCharacterPurchase(
            offerId: 1234u,
            currencyId: AccountCurrencyType.NCoin,
            target: new NetworkIdentity());

        handler.HandleMessage(session, purchase);

        ServerStoreError error = Assert.Single(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Equal(StoreError.InvalidOffer, error.Error);
    }

    [Fact]
    public void AccountGiftPurchase_WithUnknownRecipient_ReturnsIneligibleGiftRecipientStoreError()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IGlobalStorefrontManager storefrontManager = RecordingDispatchProxy<IGlobalStorefrontManager>.Create(out _);
        ICharacterManager characterManager = RecordingDispatchProxy<ICharacterManager>.Create(out _);
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out _);
        var handler = new ClientStorefrontPurchaseAccountHandler(
            NullLogger<ClientStorefrontPurchaseAccountHandler>.Instance,
            storefrontManager,
            characterManager,
            playerManager);

        ClientStorefrontPurchaseAccount purchase = ReadAccountPurchase(
            offerId: 1234u,
            currencyId: AccountCurrencyType.NCoin,
            target: new NetworkIdentity(),
            accountTarget: new NetworkIdentity(),
            recipientName: "Missing Recipient");

        handler.HandleMessage(session, purchase);

        ServerStoreError error = Assert.Single(GetMessages<ServerStoreError>(sessionProxy));
        Assert.Equal(StoreError.IneligibleGiftRecipient, error.Error);
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 321u);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity
        {
            RealmId = 1,
            Id      = 100ul
        });

        return session;
    }

    private static ClientStorefrontPurchaseCharacter ReadCharacterPurchase(
        uint offerId,
        AccountCurrencyType currencyId,
        NetworkIdentity target)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(offerId);
            writer.Write(0u, 5u);
            writer.Write(0u);
            writer.Write((uint)currencyId, 14u);
            writer.Write(0u);
            target.Write(writer);
            writer.Write(0u);
            writer.FlushBits();
        }

        byte[] packetData = stream.ToArray();
        using var packetStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(packetStream);
        var purchase = new ClientStorefrontPurchaseCharacter();
        purchase.Read(reader);
        return purchase;
    }

    private static ClientStorefrontPurchaseAccount ReadAccountPurchase(
        uint offerId,
        AccountCurrencyType currencyId,
        NetworkIdentity target,
        NetworkIdentity accountTarget,
        string recipientName)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(offerId);
            writer.Write(0u, 5u);
            writer.Write(0u);
            writer.Write((uint)currencyId, 14u);
            writer.Write(0u);
            target.Write(writer);
            writer.Write(0u);
            writer.Write(0u);
            accountTarget.Write(writer);
            writer.WriteStringWide(recipientName);
            writer.FlushBits();
        }

        byte[] packetData = stream.ToArray();
        using var packetStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(packetStream);
        var purchase = new ClientStorefrontPurchaseAccount();
        purchase.Read(reader);
        return purchase;
    }

    private static IReadOnlyList<T> GetMessages<T>(RecordingDispatchProxy<IWorldSession> sessionProxy)
        where T : class, IWritable
    {
        return sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Where(i => i.Arguments.Length == 1)
            .Select(i => i.Arguments[0])
            .OfType<T>()
            .ToList();
    }
}
