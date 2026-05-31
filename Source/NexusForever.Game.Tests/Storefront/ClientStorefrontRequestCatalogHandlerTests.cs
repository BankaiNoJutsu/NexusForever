using NexusForever.Game.Abstract.Account;

using NexusForever.Game.Abstract.Account.Inventory;

using NexusForever.Game.Abstract.Entity;

using NexusForever.Game.Abstract.Storefront;

using NexusForever.Game.Tests.TestSupport;

using NexusForever.Network.World.Message.Model;

using NexusForever.Shared.Game.Events;

using NexusForever.WorldServer.Network;

using NexusForever.WorldServer.Network.Message.Handler.Account;



namespace NexusForever.Game.Tests.Storefront;



public class ClientStorefrontRequestCatalogHandlerTests

{

    [Fact]

    public void HandleMessage_DefersCatalogUntilPregameAccountPacketsSent()

    {

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);

        IAccountInventoryManager inventoryManager = RecordingDispatchProxy<IAccountInventoryManager>.Create(out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);

        IGlobalStorefrontManager storefrontManager = RecordingDispatchProxy<IGlobalStorefrontManager>.Create(out RecordingDispatchProxy<IGlobalStorefrontManager> storefrontProxy);

        var events = new EventQueue();



        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        sessionProxy.SetProperty(nameof(IWorldSession.Events), events);

        sessionProxy.SetProperty(nameof(IWorldSession.HasSentPregameAccountPackets), false);



        accountProxy.SetProperty(nameof(IAccount.Id), 42u);

        accountProxy.SetProperty(nameof(IAccount.Session), session);

        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventoryManager);



        var handler = new ClientStorefrontRequestCatalogHandler(storefrontManager);



        handler.HandleMessage(session, new ClientStorefrontRequestCatalog());



        Assert.Empty(storefrontProxy.GetInvocations(nameof(IGlobalStorefrontManager.HandleCatalogRequest)));

        Assert.Empty(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.SendInitialPackets)));

        Assert.True(events.PendingEvents);



        sessionProxy.SetProperty(nameof(IWorldSession.HasSentPregameAccountPackets), true);



        events.Update(0d);



        Assert.Single(storefrontProxy.GetInvocations(nameof(IGlobalStorefrontManager.HandleCatalogRequest)));

        Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.SendInitialPackets)));

        Assert.False(events.PendingEvents);

    }



    [Fact]

    public void HandleMessage_WhenPregameAccountPacketsSent_ProcessesWithoutPlayerOrCharacterList()

    {

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);

        IAccountInventoryManager inventoryManager = RecordingDispatchProxy<IAccountInventoryManager>.Create(out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);

        IGlobalStorefrontManager storefrontManager = RecordingDispatchProxy<IGlobalStorefrontManager>.Create(out RecordingDispatchProxy<IGlobalStorefrontManager> storefrontProxy);

        var events = new EventQueue();



        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        sessionProxy.SetProperty(nameof(IWorldSession.Events), events);

        sessionProxy.SetProperty(nameof(IWorldSession.HasSentPregameAccountPackets), true);

        sessionProxy.SetProperty(nameof(IWorldSession.HasSentCharacterListPackets), false);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), null);



        accountProxy.SetProperty(nameof(IAccount.Id), 42u);

        accountProxy.SetProperty(nameof(IAccount.Session), session);

        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventoryManager);



        var handler = new ClientStorefrontRequestCatalogHandler(storefrontManager);



        handler.HandleMessage(session, new ClientStorefrontRequestCatalog());



        Assert.Single(storefrontProxy.GetInvocations(nameof(IGlobalStorefrontManager.HandleCatalogRequest)));

        Assert.Single(storefrontProxy.GetInvocations(nameof(IGlobalStorefrontManager.MarkAccountCatalogRequestedBeforeWorldLogin)));

        Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.SendInitialPackets)));

        Assert.False(events.PendingEvents);

    }



    [Fact]

    public void HandleMessage_WhenPlayerStillLoading_DefersCatalogUntilLoadingCompletes()

    {

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);

        IAccountInventoryManager inventoryManager = RecordingDispatchProxy<IAccountInventoryManager>.Create(out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);

        IGlobalStorefrontManager storefrontManager = RecordingDispatchProxy<IGlobalStorefrontManager>.Create(out RecordingDispatchProxy<IGlobalStorefrontManager> storefrontProxy);

        var events = new EventQueue();



        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        sessionProxy.SetProperty(nameof(IWorldSession.Events), events);

        sessionProxy.SetProperty(nameof(IWorldSession.HasSentPregameAccountPackets), true);

        sessionProxy.SetProperty(nameof(IWorldSession.HasSentCharacterListPackets), true);



        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.IsLoading), true);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);



        accountProxy.SetProperty(nameof(IAccount.Id), 42u);

        accountProxy.SetProperty(nameof(IAccount.Session), session);

        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventoryManager);



        var handler = new ClientStorefrontRequestCatalogHandler(storefrontManager);



        handler.HandleMessage(session, new ClientStorefrontRequestCatalog());



        Assert.Empty(storefrontProxy.GetInvocations(nameof(IGlobalStorefrontManager.HandleCatalogRequest)));

        Assert.Empty(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.SendInitialPackets)));

        Assert.True(events.PendingEvents);



        playerProxy.SetProperty(nameof(IPlayer.IsLoading), false);

        events.Update(0d);



        Assert.Single(storefrontProxy.GetInvocations(nameof(IGlobalStorefrontManager.HandleCatalogRequest)));

        Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.SendInitialPackets)));

        Assert.False(events.PendingEvents);

    }



    [Fact]

    public void HandleMessage_WhenPregameAccountPacketsSentAndPlayerReady_ProcessesImmediately()

    {

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);

        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);

        IAccountInventoryManager inventoryManager = RecordingDispatchProxy<IAccountInventoryManager>.Create(out RecordingDispatchProxy<IAccountInventoryManager> inventoryProxy);

        IGlobalStorefrontManager storefrontManager = RecordingDispatchProxy<IGlobalStorefrontManager>.Create(out RecordingDispatchProxy<IGlobalStorefrontManager> storefrontProxy);

        var events = new EventQueue();



        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        sessionProxy.SetProperty(nameof(IWorldSession.Events), events);

        sessionProxy.SetProperty(nameof(IWorldSession.HasSentPregameAccountPackets), true);

        sessionProxy.SetProperty(nameof(IWorldSession.HasSentCharacterListPackets), true);



        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.IsLoading), false);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);



        accountProxy.SetProperty(nameof(IAccount.Id), 42u);

        accountProxy.SetProperty(nameof(IAccount.Session), session);

        accountProxy.SetProperty(nameof(IAccount.InventoryManager), inventoryManager);



        var handler = new ClientStorefrontRequestCatalogHandler(storefrontManager);



        handler.HandleMessage(session, new ClientStorefrontRequestCatalog());



        Assert.Single(storefrontProxy.GetInvocations(nameof(IGlobalStorefrontManager.HandleCatalogRequest)));

        Assert.Single(inventoryProxy.GetInvocations(nameof(IAccountInventoryManager.SendInitialPackets)));

        Assert.False(events.PendingEvents);

    }

}


