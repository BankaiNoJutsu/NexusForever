using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Customisation;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Currency;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity.Player;

namespace NexusForever.Game.Tests.Entity;

public sealed class AppearanceChangeAdmissionTests
{
    [Theory]
    [InlineData(false, "nan")] [InlineData(true, "nan")]
    [InlineData(false, "positive-infinity")] [InlineData(true, "positive-infinity")]
    [InlineData(false, "negative-infinity")] [InlineData(true, "negative-infinity")]
    [InlineData(false, "duplicate-label")] [InlineData(true, "duplicate-label")]
    [InlineData(false, "too-many-bones")] [InlineData(true, "too-many-bones")]
    public void MalformedParsedRequestCannotChargeEitherWalletOrChangeAppearance(bool tokens, string fault)
    {
        var f = new Fixture();
        var customisations = fault == "duplicate-label" ? new[] { (7u, 20u), (7u, 30u) } : new[] { (7u, 20u) };
        var request = Read(tokens, customisations, Bones(fault));

        Exception error = Record.Exception(() => new ClientAppearanceChangeHandler(f.Customisations)
            .HandleMessage(f.Session, request));
        if (error == null)
            Assert.Fail($"Malformed appearance was accepted: credits={f.Credits.Single().Amount}, tokens={f.Tokens.GetCurrencyAmount(AccountCurrencyType.ServiceToken)}, sex={f.Player.Sex}, bones={string.Join(',', f.Appearance.GetBones().Select(bone => bone.BoneValue))}.");
        Assert.IsType<InvalidPacketValueException>(error);

        f.AssertUnchanged();
        Assert.Empty(f.Catalog.GetInvocations(nameof(ICustomisationManager.CalculateCostCredits)));
        Assert.Empty(f.Catalog.GetInvocations(nameof(ICustomisationManager.CalculateCostTokens)));
    }

    [Theory]
    [InlineData("nan")] [InlineData("positive-infinity")] [InlineData("negative-infinity")]
    [InlineData("too-many-bones")] [InlineData("duplicate-label")]
    [InlineData("null-bones")] [InlineData("null-customisations")]
    public void DirectAppearanceUpdateRejectsInvalidInputBeforeAnyMutation(string fault)
    {
        var f = new Fixture();
        IList<(uint Label, uint Value)> customisations = fault == "null-customisations" ? null
            : fault == "duplicate-label" ? [(7, 20), (7, 30)] : [(7, 20)];
        IList<float> bones = fault == "null-bones" ? null : Bones(fault);

        Assert.ThrowsAny<ArgumentException>(() => f.Appearance.Update(Race.Human, Sex.Female, customisations, bones));

        f.AssertUnchanged();
        Assert.Empty(f.Catalog.GetInvocations(nameof(ICustomisationManager.GetItemVisuals)));
    }

    [Theory]
    [InlineData(false, 0)] [InlineData(true, 0)]
    [InlineData(false, 256)] [InlineData(true, 256)]
    [InlineData(false, 3)] [InlineData(true, 3)]
    public void FiniteRequestsKeepTheExistingPaymentAndAppearanceFlow(bool tokens, int count)
    {
        var f = new Fixture();
        float[] bones = count == 3 ? [float.MinValue, 0, float.MaxValue] : Enumerable.Repeat(.75f, count).ToArray();
        var request = Read(tokens, [(7u, 20u)], bones);

        new ClientAppearanceChangeHandler(f.Customisations).HandleMessage(f.Session, request);

        Assert.Equal(tokens ? 100ul : 93ul, f.Credits.Single(row => row.Id == CurrencyType.Credits).Amount);
        Assert.Equal(tokens ? 93ul : 100ul, f.Tokens.GetCurrencyAmount(AccountCurrencyType.ServiceToken));
        Assert.Equal(Sex.Female, f.Player.Sex);
        Assert.Equal(20u, Assert.Single(f.Appearance.GetCustomisations()).Value);
        Assert.Equal((ushort)22, Assert.Single(f.Appearance.GetAppearances()).DisplayId);
        Assert.Equal(bones, f.Appearance.GetBones().Select(bone => bone.BoneValue).ToArray());
        var notice = Assert.Single(f.Owner.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
        Assert.Equal(bones, Assert.IsType<ServerEntityBoneUpdate>(notice.Arguments[0]).Bones);
        Assert.Single(f.Catalog.GetInvocations(tokens ? nameof(ICustomisationManager.CalculateCostTokens) : nameof(ICustomisationManager.CalculateCostCredits)));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void UnaffordableValidRequestKeepsBothWalletsAndAppearance(bool tokens)
    {
        var f = new Fixture();
        f.Catalog.SetMethodReturn(tokens ? nameof(ICustomisationManager.CalculateCostTokens) : nameof(ICustomisationManager.CalculateCostCredits), 101u);

        new ClientAppearanceChangeHandler(f.Customisations).HandleMessage(f.Session, Read(tokens, [(7u, 20u)], [.75f]));

        f.AssertUnchanged();
    }

    private static float[] Bones(string fault) => fault switch
    {
        "nan" => [float.NaN],
        "positive-infinity" => [float.PositiveInfinity],
        "negative-infinity" => [float.NegativeInfinity],
        "too-many-bones" => Enumerable.Repeat(.75f, 257).ToArray(),
        _ => [.75f]
    };

    private static ClientCharacterAppearanceChange Read(bool tokens, (uint Label, uint Value)[] customisations, float[] bones)
    {
        using var output = new MemoryStream();
        using (var writer = new GamePacketWriter(output))
        {
            writer.Write((uint)Race.Human, 5); writer.Write((uint)Sex.Female, 2);
            writer.Write((uint)customisations.Length);
            foreach (var pair in customisations) writer.Write(pair.Label);
            foreach (var pair in customisations) writer.Write(pair.Value);
            writer.Write((uint)bones.Length);
            foreach (float bone in bones) writer.Write(bone);
            writer.Write(tokens); writer.FlushBits();
        }
        using var input = new MemoryStream(output.ToArray()); using var reader = new GamePacketReader(input);
        var request = new ClientCharacterAppearanceChange(); request.Read(reader); return request;
    }

    private sealed class Fixture
    {
        internal readonly IWorldSession Session;
        internal readonly IPlayer Player;
        internal readonly RecordingDispatchProxy<IPlayer> Owner;
        internal readonly ICustomisationManager Customisations;
        internal readonly RecordingDispatchProxy<ICustomisationManager> Catalog;
        internal readonly CurrencyManager Credits;
        internal readonly AccountCurrencyManager Tokens;
        internal readonly AppearanceManager Appearance;
        private readonly RecordingDispatchProxy<IWorldSession> connection;

        internal Fixture()
        {
            Session = RecordingDispatchProxy<IWorldSession>.Create(out connection);
            Player = RecordingDispatchProxy<IPlayer>.Create(out Owner);
            IAccount account = RecordingDispatchProxy<IAccount>.Create(out var accountProxy);
            IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
            tableProxy.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1 }));
            tableProxy.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(new AccountCurrencyTypeEntry { Id = 9 }));
            Owner.SetProperty(nameof(IPlayer.CharacterId), 42ul); Owner.SetProperty(nameof(IPlayer.Guid), 77u);
            Owner.SetProperty(nameof(IPlayer.Race), Race.Human); Owner.SetProperty(nameof(IPlayer.Sex), Sex.Male);
            Owner.SetProperty(nameof(IPlayer.Session), Session); Owner.SetProperty(nameof(IPlayer.Account), account);
            accountProxy.SetProperty(nameof(IAccount.Id), 12u); accountProxy.SetProperty(nameof(IAccount.Session), Session);
            connection.SetProperty(nameof(IWorldSession.Player), Player);
            Customisations = RecordingDispatchProxy<ICustomisationManager>.Create(out Catalog);
            Catalog.SetMethodReturn(nameof(ICustomisationManager.Validate), true);
            Catalog.SetMethodReturn(nameof(ICustomisationManager.CalculateCostCredits), 7u);
            Catalog.SetMethodReturn(nameof(ICustomisationManager.CalculateCostTokens), 7u);
            Catalog.SetMethodReturn(nameof(ICustomisationManager.GetItemVisuals), new IItemVisual[] { new ItemVisual { Slot = ItemSlot.BodyHair, DisplayId = 22 } });
            var model = new CharacterModel { Id = 42 };
            model.Currency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
            model.Customisation.Add(new() { Id = 42, Label = 7, Value = 10 });
            model.Appearance.Add(new() { Id = 42, Slot = (byte)ItemSlot.BodyHair, DisplayId = 12 });
            model.Bone.Add(new() { Id = 42, BoneIndex = 0, Bone = .25f });
            Credits = new CurrencyManager(tables); Credits.Initialise(Player, model);
            var auth = new AccountModel { Id = 12 }; auth.AccountCurrency.Add(new() { Id = 12, CurrencyId = 9, Amount = 100 });
            Tokens = new AccountCurrencyManager(account, auth, tables);
            Owner.SetProperty(nameof(IPlayer.CurrencyManager), Credits); accountProxy.SetProperty(nameof(IAccount.CurrencyManager), Tokens);
            Appearance = new AppearanceManager(Player, model, Customisations);
            Owner.SetProperty(nameof(IPlayer.AppearanceManager), Appearance);
            Owner.Invocations.Clear(); connection.Invocations.Clear();
        }

        internal void AssertUnchanged()
        {
            Assert.Equal(100ul, Credits.Single(row => row.Id == CurrencyType.Credits).Amount);
            Assert.Equal(100ul, Tokens.GetCurrencyAmount(AccountCurrencyType.ServiceToken));
            Assert.Equal(Race.Human, Player.Race); Assert.Equal(Sex.Male, Player.Sex);
            Assert.Equal(10u, Assert.Single(Appearance.GetCustomisations()).Value);
            Assert.Equal((ushort)12, Assert.Single(Appearance.GetAppearances()).DisplayId);
            Assert.Equal(.25f, Assert.Single(Appearance.GetBones()).BoneValue);
            Assert.Empty(Owner.GetInvocations(nameof(IPlayer.AddVisual)));
            Assert.Empty(Owner.GetInvocations(nameof(IPlayer.EnqueueToVisible)));
            Assert.Empty(connection.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        }
    }
}
