using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Option;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Option;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.Option;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Option;

namespace NexusForever.Game.Tests.Option;

public class OptionHandlerTests
{
    [Fact]
    public void CombatOptions_UpdatesMappedPlayerPreferences()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientCombatOptionsHandler(NullLogger<ClientCombatOptionsHandler>.Instance);

        handler.HandleMessage(session, new ClientCombatOptions
        {
            CastingOptions = CastingOptionFlags.ButtonDownForAbilities | CastingOptionFlags.HoldToContinueCasting,
            DisableOtherPlayersLogging = true,
            CombatLogDisableFlags = CombatLogOptions.DisableDamage | CombatLogOptions.DisableHeal
        });

        Assert.Equal(CastingOptionFlags.ButtonDownForAbilities | CastingOptionFlags.HoldToContinueCasting, session.Player.CastingOptions);
        Assert.True(session.Player.DisableOtherPlayersCombatLogs);
        Assert.Equal(CombatLogOptions.DisableDamage | CombatLogOptions.DisableHeal, session.Player.CombatLogDisableFlags);
        Assert.Single(playerProxy.GetInvocations("set_" + nameof(IPlayer.CastingOptions)));
        Assert.Single(playerProxy.GetInvocations("set_" + nameof(IPlayer.DisableOtherPlayersCombatLogs)));
        Assert.Single(playerProxy.GetInvocations("set_" + nameof(IPlayer.CombatLogDisableFlags)));
    }

    [Fact]
    public void CombatOptions_WithUnknownBitsThrowsBeforeUpdatingPlayer()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientCombatOptionsHandler(NullLogger<ClientCombatOptionsHandler>.Instance);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, new ClientCombatOptions
        {
            CastingOptions = (CastingOptionFlags)0x10u,
            DisableOtherPlayersLogging = true,
            CombatLogDisableFlags = CombatLogOptions.DisableDamage
        }));

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.CastingOptions)));
        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.DisableOtherPlayersCombatLogs)));
        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.CombatLogDisableFlags)));
    }

    [Fact]
    public void Options_CastingUpdatesCastingFlags()
    {
        IWorldSession session = CreateSession(out _);
        var handler = new ClientOptionsHandler(NullLogger<ClientOptionsHandler>.Instance);

        handler.HandleMessage(session, CreateOptions(OptionType.Casting, (uint)CastingOptionFlags.AutoTargetting));

        Assert.Equal(CastingOptionFlags.AutoTargetting, session.Player.CastingOptions);
    }

    [Theory]
    [InlineData(0u, false)]
    [InlineData(1u, true)]
    public void Options_SharedChallengeUpdatesPreference(uint value, bool expected)
    {
        IWorldSession session = CreateSession(out _);
        var handler = new ClientOptionsHandler(NullLogger<ClientOptionsHandler>.Instance);

        handler.HandleMessage(session, CreateOptions(OptionType.SharedChallenge, value));

        Assert.Equal(expected, session.Player.SharedChallengeEnabled);
    }

    [Theory]
    [InlineData((uint)OptionType.Casting, 0x10u)]
    [InlineData((uint)OptionType.SharedChallenge, 2u)]
    [InlineData(999u, 0u)]
    public void Options_WithInvalidTypeOrValueThrows(uint optionType, uint value)
    {
        IWorldSession session = CreateSession(out _);
        var handler = new ClientOptionsHandler(NullLogger<ClientOptionsHandler>.Instance);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateOptions((OptionType)optionType, value)));
    }

    [Fact]
    public void CombatLogDisableOthers_UpdatesPreference()
    {
        IWorldSession session = CreateSession(out _);
        var handler = new ClientCombatLogDisableOthersHandler(NullLogger<ClientCombatLogDisableOthersHandler>.Instance);

        handler.HandleMessage(session, new ClientCombatLogDisableOthers
        {
            DisableOtherPlayers = true
        });

        Assert.True(session.Player.DisableOtherPlayersCombatLogs);
    }

    [Fact]
    public void CombatLogDisableOthers_WithInvalidBooleanValueThrowsBeforeUpdatingPlayer()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientCombatLogDisableOthersHandler(NullLogger<ClientCombatLogDisableOthersHandler>.Instance);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, new ClientCombatLogDisableOthers
        {
            DisableOtherPlayersValue = 2u
        }));

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.DisableOtherPlayersCombatLogs)));
    }

    [Fact]
    public void CombatLogDisables_UpdatesDisableFlags()
    {
        IWorldSession session = CreateSession(out _);
        var handler = new ClientCombatLogDisablesHandler(NullLogger<ClientCombatLogDisablesHandler>.Instance);

        handler.HandleMessage(session, new ClientCombatLogDisables
        {
            DisableFlags = CombatLogOptions.DisableAbsorption | CombatLogOptions.DisableDeath
        });

        Assert.Equal(CombatLogOptions.DisableAbsorption | CombatLogOptions.DisableDeath, session.Player.CombatLogDisableFlags);
    }

    [Fact]
    public void CombatLogDisables_WithUnknownBitsThrowsBeforeUpdatingPlayer()
    {
        IWorldSession session = CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientCombatLogDisablesHandler(NullLogger<ClientCombatLogDisablesHandler>.Instance);

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, new ClientCombatLogDisables
        {
            DisableFlags = (CombatLogOptions)0x4000u
        }));

        Assert.Empty(playerProxy.GetInvocations("set_" + nameof(IPlayer.CombatLogDisableFlags)));
    }

    [Fact]
    public void RequestInputKeySet_WithDifferentCharacterIdThrowsBeforeReadback()
    {
        IWorldSession session = CreateKeybindingSession(
            123ul,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<ICharacterKeybindingManager> characterKeybindingProxy,
            out RecordingDispatchProxy<IAccountKeybindingManager> accountKeybindingProxy);
        var handler = new ClientRequestInputKeySetHandler();

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, CreateRequestInputKeySet(456ul)));

        Assert.Empty(characterKeybindingProxy.GetInvocations(nameof(ICharacterKeybindingManager.Build)));
        Assert.Empty(accountKeybindingProxy.GetInvocations(nameof(IAccountKeybindingManager.Build)));
        Assert.Empty(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void UpdateInputKeySet_WithDifferentCharacterIdThrowsBeforeMutatingManagers()
    {
        IWorldSession session = CreateKeybindingSession(
            123ul,
            out _,
            out RecordingDispatchProxy<ICharacterKeybindingManager> characterKeybindingProxy,
            out RecordingDispatchProxy<IAccountKeybindingManager> accountKeybindingProxy);
        var handler = new BiInputKeySetHandler();

        Assert.Throws<InvalidPacketValueException>(() => handler.HandleMessage(session, new BiInputKeySet
        {
            CharacterId = 456ul
        }));

        Assert.Empty(characterKeybindingProxy.GetInvocations(nameof(ICharacterKeybindingManager.Update)));
        Assert.Empty(accountKeybindingProxy.GetInvocations(nameof(IAccountKeybindingManager.Update)));
    }

    private static IWorldSession CreateSession(out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static ClientOptions CreateOptions(OptionType type, uint value)
    {
        var options = (ClientOptions)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientOptions));
        typeof(ClientOptions).GetProperty(nameof(ClientOptions.Type))!.SetValue(options, type);
        typeof(ClientOptions).GetProperty(nameof(ClientOptions.NewValue))!.SetValue(options, value);
        return options;
    }

    private static IWorldSession CreateKeybindingSession(
        ulong characterId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterKeybindingManager> characterKeybindingProxy,
        out RecordingDispatchProxy<IAccountKeybindingManager> accountKeybindingProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IAccount account = RecordingDispatchProxy<IAccount>.Create(out RecordingDispatchProxy<IAccount> accountProxy);
        ICharacterKeybindingManager characterKeybindings = RecordingDispatchProxy<ICharacterKeybindingManager>.Create(out characterKeybindingProxy);
        IAccountKeybindingManager accountKeybindings = RecordingDispatchProxy<IAccountKeybindingManager>.Create(out accountKeybindingProxy);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IPlayer.KeybindingManager), characterKeybindings);
        accountProxy.SetProperty(nameof(IAccount.KeybindingManager), accountKeybindings);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetProperty(nameof(IWorldSession.Account), account);

        return session;
    }

    private static ClientRequestInputKeySet CreateRequestInputKeySet(ulong characterId)
    {
        var request = (ClientRequestInputKeySet)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ClientRequestInputKeySet));
        typeof(ClientRequestInputKeySet).GetProperty(nameof(ClientRequestInputKeySet.CharacterId))!.SetValue(request, characterId);
        return request;
    }
}
