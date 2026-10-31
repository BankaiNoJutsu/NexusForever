using Microsoft.EntityFrameworkCore;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Tests.Quest;

public sealed partial class ContractLiveCompletionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InitiallyCleanOrAbsentCurrenciesKeepPostCaptureIntentForTheirOwnSave(bool existing)
    {
        using var f = new Fixture(options); await f.Actor.Load();
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Tables).SetProperty(nameof(IGameTableManager.CurrencyType),
            PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1 }, new CurrencyTypeEntry { Id = 2 }));
        using (var seed = f.Inner.Context())
        {
            if (existing) { seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 2, Amount = 10 }); seed.SaveChanges(); }
            var model = new CharacterModel { Id = 42 }; foreach (var row in seed.CharacterCurrency.AsNoTracking()) model.Currency.Add(row);
            var currency = new CurrencyManager(f.Tables); currency.Initialise(f.Actor.Player, model); f.Actor.Set(nameof(Player.CurrencyManager), currency);
        }
        f.Actor.OnAuth = () => f.Actor.Player.CurrencyManager.CurrencyAddAmount(CurrencyType.Renown, 7);
        f.Complete(); await f.Actor.Drain();
        using (var read = f.Inner.Context())
        {
            var stored = read.CharacterCurrency.SingleOrDefault(row => row.CurrencyId == 2);
            Assert.Equal(existing, stored != null); if (existing) Assert.Equal(10ul, stored.Amount);
        }
        var wallet = Assert.IsType<Currency>(f.Actor.Player.CurrencyManager.Single(row => row.Id == CurrencyType.Renown));
        Assert.True(wallet.NeedsPersistenceSave); f.Actor.OnAuth = null; await f.Actor.Player.SaveDirect();
        using var final = f.Inner.Context(); Assert.Equal(existing ? 17ul : 7ul, final.CharacterCurrency.Single(row => row.CurrencyId == 2).Amount);
        Assert.False(wallet.NeedsPersistenceSave); Assert.Single(final.CharacterContractCompletion); Assert.Equal(1, f.XpCalls);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CurrencyIntentArrivingAfterCaptureCannotRewriteTheFrozenOrdinaryReward(bool afterEnrollment)
    {
        using var f = new Fixture(options); await f.Actor.Load();
        var manager = Assert.IsType<CurrencyManager>(f.Actor.Player.CurrencyManager);
        void Later() => manager.CurrencyAddAmount(CurrencyType.Credits, 7);
        if (afterEnrollment)
            ((RecordingDispatchProxy<IAppearanceManager>)(object)f.Actor.Player.AppearanceManager).SetMethodHandler(nameof(IAppearanceManager.Save), _ => { Later(); return null; });
        else f.Actor.OnAuth = Later;
        f.Complete(); await f.Actor.Drain();
        using (var read = f.Inner.Context()) Assert.Equal(125ul, read.CharacterCurrency.Single().Amount);
        var wallet = Assert.IsType<Currency>(Assert.Single(manager)); Assert.Equal(132ul, wallet.Amount); Assert.True(wallet.NeedsPersistenceSave);
        f.Actor.OnAuth = null;
        ((RecordingDispatchProxy<IAppearanceManager>)(object)f.Actor.Player.AppearanceManager).SetMethodHandler(nameof(IAppearanceManager.Save), _ => null);
        await f.Actor.Player.SaveDirect();
        using var final = f.Inner.Context(); Assert.Equal(132ul, final.CharacterCurrency.Single().Amount); Assert.Single(final.CharacterContractCompletion);
        Assert.False(wallet.NeedsPersistenceSave); Assert.Equal(1, f.XpCalls);
    }
}
