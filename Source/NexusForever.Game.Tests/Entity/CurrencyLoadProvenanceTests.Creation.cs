using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Tests.Entity;

public sealed partial class CurrencyLoadProvenanceTests
{
    private sealed class CreationEnrollment(Action<CharacterContext> enroll) : IDatabaseCharacter
    {
        public void Save(CharacterContext context) => enroll(context);
    }

    private static CharacterModel NewCharacter(Fixture fixture)
    {
        using var clear = new CharacterContext(fixture.First);
        clear.Character.Where(row => row.Id == 42).ExecuteDelete();
        var model = new CharacterModel { Id = 42, AccountId = 12, Name = "Created origin", OriginalName = "Created origin" };
        model.Currency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 });
        return model;
    }

    [Theory]
    [InlineData("same")] [InlineData("normalized")] [InlineData("foreign")]
    public async Task CreatedRosterModelRetainsItsConfirmedCreateDatabase(string route)
    {
        using var f = new Fixture(true); var model = NewCharacter(f);
        bool validated = false;
        await f.Database.CreateCharacter(model, [new CreationEnrollment(context => context.OnSaveValidation(phase =>
        {
            Assert.Null(CharacterLoadOrigin.Get(model));
            if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit) validated = true;
        }))]);
        Assert.True(validated); Assert.NotNull(CharacterLoadOrigin.Get(model));
        var manager = f.Manager(model); Assert.Single(manager).Amount = 90;
        f.Database.Initialise(route == "foreign" ? f.Second : route == "normalized" ? f.NormalizedFirst : f.First);
        using var save = f.Database.CreateContext();
        var error = await Record.ExceptionAsync(async () => { manager.Save(save); await save.SaveChangesAsync(); });
        Assert.Equal((ulong?)(route == "foreign" ? 100 : 90), Fixture.Stored(f.First));
        Assert.Equal((ulong?)100, Fixture.Stored(f.Second));
        if (route == "foreign") { Assert.IsType<InvalidOperationException>(error); Assert.True(save.IsSaveRejected); }
        else Assert.Null(error);
    }

    [Fact]
    public async Task CreatedOriginUsesTheExecutingContextWhenTheFactoryChanges()
    {
        using var f = new Fixture(true); var model = NewCharacter(f);
        f.Database.Initialise(new SwitchDuringConfiguration(f.First, () => f.Database.Initialise(f.Second)));
        await f.Database.CreateCharacter(model, []);
        using var original = new CharacterContext(f.First);
        Assert.Equal(CharacterDatabaseIdentity.Get(original), CharacterLoadOrigin.Get(model).Database);
        var manager = f.Manager(model); Assert.Single(manager).Amount = 90;
        using var foreign = f.Database.CreateContext();
        Assert.Throws<InvalidOperationException>(() => manager.Save(foreign));
        Assert.Equal((ulong?)100, Fixture.Stored(f.First)); Assert.Equal((ulong?)100, Fixture.Stored(f.Second));
    }

    [Theory]
    [InlineData("premature")] [InlineData("foreign-row")] [InlineData("commit")]
    public async Task FailedCreationDoesNotAuthorizeTheRosterModel(string fault)
    {
        using var f = new Fixture(true); var model = NewCharacter(f);
        var error = await Record.ExceptionAsync(() => f.Database.CreateCharacter(model, [new CreationEnrollment(context =>
        {
            if (fault == "premature") context.SaveChanges();
            if (fault == "foreign-row") context.Character.Add(new() { Id = 43, AccountId = 12, Name = "Other", OriginalName = "Other" });
            if (fault == "commit") context.OnSaveValidation(phase =>
            {
                if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit) throw new InvalidOperationException("Injected create rollback after flush.");
            });
        })]));
        Assert.IsType<InvalidOperationException>(error);
        Assert.Null(CharacterLoadOrigin.Get(model)); Assert.Null(CharacterLoadOrigin.GetCurrencyDatabase(Assert.Single(model.Currency)));
        using var read = new CharacterContext(f.First); Assert.Empty(read.Character); Assert.Empty(read.CharacterCurrency);
        Assert.Equal((ulong?)100, Fixture.Stored(f.Second));
    }

    [Fact]
    public async Task PostCommitModelMutationCannotManufactureCreationProvenance()
    {
        using var f = new Fixture(true); var model = NewCharacter(f);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Database.CreateCharacter(model,
            [new CreationEnrollment(context => context.OnSaveSettled(committed =>
            {
                if (committed) Assert.Single(model.Currency).Amount = 999;
            }))]));
        Assert.Null(CharacterLoadOrigin.Get(model)); Assert.Equal((ulong?)100, Fixture.Stored(f.First));
    }
}
