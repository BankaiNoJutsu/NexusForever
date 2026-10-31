using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class ContractLiveCompletionTests
{
    [Theory]
    [InlineData("disconnect", false)] [InlineData("disconnect", true)]
    [InlineData("logout", false)] [InlineData("logout", true)]
    [InlineData("finished", false)] [InlineData("finished", true)]
    public async Task NativeDisconnectAndLogoutFlagsRefuseAdmissionAndPreventLateCommittedClaimPublication(string state, bool afterBegin)
    {
        var gate = new QueueClaimPause();
        using var f = new Fixture(options, rewardContexts: inner => inner.Context(gate), claimQueue: true);
        var session = new ContractNativeSession(); f.Actor.Set(nameof(Player.Session), session);
        f.Inner.Source.PlayerProxy.SetProperty(nameof(IPlayer.Session), session);
        ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.Session), session);
        uint reward = await SeedQueuedClaim(f); int messages = session.Messages.Count;
        if (afterBegin) { Assert.True(f.Actor.Quests.QueueContractRewardClaim(reward, 0)); await AwaitClaimGate(f, gate); }
        if (state == "disconnect") session.ForceDisconnect();
        else f.Actor.Player.LogoutManager.State = state == "logout" ? LogoutState.Logout : LogoutState.Finished;
        if (!afterBegin) Assert.False(f.Actor.Quests.QueueContractRewardClaim(reward, 0));
        gate.Release.TrySetResult(true); await f.Actor.Drain(allowErrors: afterBegin);
        using var read = f.Inner.Context(); Assert.Equal(afterBegin ? 1 : 0, read.CharacterContractRewardClaim.Count());
        Assert.Equal(afterBegin ? 2500100ul : 100ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(100ul, f.Actor.Player.CurrencyManager.Single().Amount); Assert.Equal(messages, session.Messages.Count);
        if (afterBegin)
        {
            Assert.True(f.Actor.Quests.RequiresAuthoritativeReload);
            Assert.True(Assert.IsType<CurrencyManager>(f.Actor.Player.CurrencyManager).RequiresContractCurrencyReload);
        }
    }

    private sealed class ContractNativeSession : NetworkSession, IGameSession
    {
        internal readonly List<IWritable> Messages = [];
        public bool CanProcessIncomingPackets { get; set; }
        public bool CanProcessOutgoingPackets { get; set; }
        public void EnqueueMessage(IWritable message) => Messages.Add(message);
        public void EnqueueMessageEncrypted(IWritable message) => Messages.Add(message);
        public void EnqueueMessageEncrypted(uint opcode, string hex) => throw new NotSupportedException();
        public void HandlePacket(ClientGamePacket packet) => throw new NotSupportedException();
        public void FlushPackets() { }
        protected override uint OnData(byte[] buffer, int offset, int count) => 0;
    }
}
