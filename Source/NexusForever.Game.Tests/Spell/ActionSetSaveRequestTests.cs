using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Spell;

namespace NexusForever.Game.Tests.Spell;

public class ActionSetSaveRequestTests
{
    [Fact]
    public void AddShortcut_RequestsOwnerSave()
    {
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy);

        actionSet.AddShortcut(UILocation.LAS1, ShortcutType.GameCommand, 1u, 0);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void UpdateSpellShortcut_RequestsOwnerSave()
    {
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy);
        actionSet.AddShortcut(CreateShortcutModel(UILocation.LAS1, 1234u, tier: 1));
        playerProxy.Invocations.Clear();

        actionSet.UpdateSpellShortcut(1234u, 3);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void RemovePersistedShortcut_RequestsOwnerSave()
    {
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy);
        actionSet.AddShortcut(CreateShortcutModel(UILocation.LAS1, 1234u, tier: 1));
        playerProxy.Invocations.Clear();

        actionSet.RemoveShortcut(UILocation.LAS1);

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void AddShortcutFromExistingModel_DoesNotRequestOwnerSave()
    {
        ActionSet actionSet = CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy);

        actionSet.AddShortcut(CreateShortcutModel(UILocation.LAS1, 1234u, tier: 1));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void ClientSetSpecHandler_RequestsOwnerSaveWhenSpecChanges()
    {
        IWorldSession session = CreateSetSpecSession(SpecError.Ok, activeActionSet: 1, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientSetSpecHandler();

        handler.HandleMessage(session, ReadSetSpec(1));

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void ClientSetSpecHandler_DoesNotRequestOwnerSaveWhenSpecChangeFails()
    {
        IWorldSession session = CreateSetSpecSession(SpecError.InCombat, activeActionSet: 0, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientSetSpecHandler();

        handler.HandleMessage(session, ReadSetSpec(1));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
    }

    [Fact]
    public void ClientRequestActionSetChangesHandler_RequestsOwnerSaveWhenUseSetClearsSlot()
    {
        IWorldSession session = CreateUseSetSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientRequestActionSetChangesHandler(CreateMatchManager());

        handler.HandleMessage(session, ReadActionSetChanges(new uint[ClientRequestActionSetChanges.ActionCount]));

        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.RequestSave)));
        IReadOnlyList<RecordingDispatchProxy<IWorldSession>.Invocation> encryptedMessages = sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted));
        Assert.Contains(encryptedMessages, i => i.Arguments[0] is ServerActionSetClearCache);
        Assert.Contains(encryptedMessages, i => i.Arguments[0] is ServerActionSet);
    }

    private static ActionSet CreateActionSet(out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);

        return new ActionSet(0, player);
    }

    private static CharacterActionSetShortcutModel CreateShortcutModel(UILocation location, uint objectId, byte tier)
    {
        return new CharacterActionSetShortcutModel
        {
            Id           = 42ul,
            SpecIndex    = 0,
            Location     = (ushort)location,
            ShortcutType = (byte)ShortcutType.SpellbookItem,
            ObjectId     = objectId,
            Tier         = tier
        };
    }

    private static IWorldSession CreateSetSpecSession(
        SpecError specError,
        byte activeActionSet,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);

        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.SetActiveActionSet), specError);
        spellManagerProxy.SetProperty(nameof(ISpellManager.ActiveActionSet), activeActionSet);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }

    private static IWorldSession CreateUseSetSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new Identity { RealmId = 1, Id = 42ul });
        playerProxy.SetProperty(nameof(IPlayer.IsAlive), true);
        playerProxy.SetProperty(nameof(IPlayer.InCombat), false);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        var actionSet = new ActionSet(0, player);
        actionSet.AddShortcut(CreateShortcutModel(UILocation.LAS1, 1u, tier: 1));

        spellManagerProxy.SetProperty(nameof(ISpellManager.ActiveActionSet), (byte)0);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetActionSet), actionSet);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        return session;
    }

    private static IMatchManager CreateMatchManager()
    {
        IMatchManager matchManager = RecordingDispatchProxy<IMatchManager>.Create(out RecordingDispatchProxy<IMatchManager> matchManagerProxy);
        IMatchCharacter matchCharacter = RecordingDispatchProxy<IMatchCharacter>.Create(out RecordingDispatchProxy<IMatchCharacter> matchCharacterProxy);
        matchCharacterProxy.SetProperty(nameof(IMatchCharacter.Match), null);
        matchManagerProxy.SetMethodReturn(nameof(IMatchManager.GetMatchCharacter), matchCharacter);
        return matchManager;
    }

    private static ClientSetSpec ReadSetSpec(byte specIndex)
    {
        byte[] packetData;
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(specIndex, 3u);
            writer.FlushBits();
            packetData = stream.ToArray();
        }

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientSetSpec();
        packet.Read(reader);
        return packet;
    }

    private static ClientRequestActionSetChanges ReadActionSetChanges(IReadOnlyList<uint> actions)
    {
        byte[] packetData;
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write((byte)actions.Count, 4u);
            foreach (uint action in actions)
                writer.Write(action);

            writer.Write((byte)0, 3u);
            writer.Write((byte)0, 5u);
            writer.Write((byte)0, 7u);
            writer.FlushBits();
            packetData = stream.ToArray();
        }

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        var packet = new ClientRequestActionSetChanges();
        packet.Read(reader);
        return packet;
    }
}
