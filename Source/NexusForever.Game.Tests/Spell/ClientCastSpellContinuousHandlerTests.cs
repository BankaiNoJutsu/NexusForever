using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Spell;

namespace NexusForever.Game.Tests.Spell;

public class ClientCastSpellContinuousHandlerTests
{
    private const ushort AbilityBagIndex = 4;
    private const uint AbilityItemId = 777u;
    private const uint PrimaryTargetId = 12345u;
    private const uint ClientContextToken = 54321u;

    [Fact]
    public void HandleMessage_ForwardsClientResolvedTargetToContinuousCast()
    {
        var handler = new ClientCastSpellContinuousHandler();
        IWorldSession session = CreateSession(out RecordingDispatchProxy<ICharacterSpell> characterSpellProxy);
        ClientCastSpellContinuous request = CreateRequest(AbilityBagIndex, PrimaryTargetId, buttonPressed: true);

        handler.HandleMessage(session, request);

        RecordingDispatchProxy<ICharacterSpell>.Invocation invocation =
            Assert.Single(characterSpellProxy.GetInvocations(nameof(ICharacterSpell.Cast)));
        Assert.Equal(4, invocation.Arguments.Length);
        Assert.Equal(true, invocation.Arguments[0]);
        Assert.Equal(PrimaryTargetId, invocation.Arguments[1]);
        Assert.Equal(0u, invocation.Arguments[2]);
        Assert.Equal(nameof(ClientCastSpellContinuous), invocation.Arguments[3]);
    }

    [Fact]
    public void CastContinuous_WithClientTarget_UsesExplicitPrimaryTarget()
    {
        CharacterSpell characterSpell = CreateCharacterSpell(out RecordingDispatchProxy<IPlayer> playerProxy);

        characterSpell.Cast(true, PrimaryTargetId, ClientContextToken, nameof(ClientCastSpellContinuous));

        RecordingDispatchProxy<IPlayer>.Invocation invocation =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
        Assert.Equal(20u, Assert.IsType<uint>(invocation.Arguments[0]));
        SpellParameters parameters = Assert.IsType<SpellParameters>(invocation.Arguments[1]);
        Assert.Same(characterSpell, parameters.CharacterSpell);
        Assert.Equal(PrimaryTargetId, parameters.PrimaryTargetId);
        Assert.Equal(ClientContextToken, parameters.ClientContextToken);
        Assert.Equal(nameof(ClientCastSpellContinuous), parameters.ClientRequestSource);
        Assert.True(parameters.UserInitiatedSpellCast);
    }

    [Fact]
    public void CastContinuous_ButtonRelease_DoesNotStartCast()
    {
        CharacterSpell characterSpell = CreateCharacterSpell(out RecordingDispatchProxy<IPlayer> playerProxy);

        characterSpell.Cast(false, PrimaryTargetId, ClientContextToken, nameof(ClientCastSpellContinuous));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<ICharacterSpell> characterSpellProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out RecordingDispatchProxy<IInventory> inventoryProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        ICharacterSpell characterSpell = RecordingDispatchProxy<ICharacterSpell>.Create(out characterSpellProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        itemProxy.SetProperty(nameof(IItem.Id), AbilityItemId);

        inventoryProxy.SetMethodReturn(nameof(IInventory.GetItem), item);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpell), characterSpell);
        return session;
    }

    private static CharacterSpell CreateCharacterSpell(out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        IItem item = RecordingDispatchProxy<IItem>.Create(out _);

        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 10u });
        baseInfoProxy.SetMethodReturn(nameof(ISpellBaseInfo.GetSpellInfo), spellInfo);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id = 20u,
            Spell4BaseIdBaseSpell = 10u,
            TierIndex = 1u
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);

        return new CharacterSpell(player, baseInfo, 1, item);
    }

    private static ClientCastSpellContinuous CreateRequest(ushort bagIndex, uint primaryTargetId, bool buttonPressed)
    {
        byte[] payload =
        [
            (byte)bagIndex,
            (byte)(bagIndex >> 8),
            (byte)primaryTargetId,
            (byte)(primaryTargetId >> 8),
            (byte)(primaryTargetId >> 16),
            (byte)(primaryTargetId >> 24),
            buttonPressed ? (byte)1 : (byte)0
        ];

        using var stream = new MemoryStream(payload);
        using var reader = new GamePacketReader(stream);
        var request = new ClientCastSpellContinuous();
        request.Read(reader);
        return request;
    }
}
