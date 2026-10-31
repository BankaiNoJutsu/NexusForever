using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

public class BasicPrerequisiteClientTests
{
    [Theory]
    [InlineData(Sex.Male, 0u, true)]
    [InlineData(Sex.Male, 1u, false)]
    [InlineData(Sex.Female, 0u, false)]
    [InlineData(Sex.Female, 1u, true)]
    [InlineData(Sex.Male, 256u, false)]
    [InlineData(Sex.Female, 257u, false)]
    [InlineData(Sex.Female, uint.MaxValue, false)]
    public void GenderComparesFullValueAndDoesNotWrapByteEnum(Sex sex, uint value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Sex), sex);
        var check = new PrerequisiteCheckGender();
        Assert.Equal(expected, check.Meets(player, PrerequisiteComparison.Equal, value, 999, null));
        Assert.Equal(!expected, check.Meets(player, PrerequisiteComparison.NotEqual, value, 0, null));
    }

    [Theory]
    [InlineData(4u, ItemProficiency.LightArmor, true)]
    [InlineData(4u, ItemProficiency.MediumArmor, false)]
    [InlineData(5u, ItemProficiency.GreatWeapon, true)]
    [InlineData(5u, ItemProficiency.BattleArmor | ItemProficiency.MediumArmor, false)]
    [InlineData(30u, ItemProficiency.Resonators, true)]
    [InlineData(30u, ItemProficiency.BatteryPack, true)]
    [InlineData(30u, ItemProficiency.LightArmor, false)]
    public void ProficiencyResolvesTableRowIdBeforeTestingAnyBit(uint id, ItemProficiency owned, bool expected)
    {
        var tables = Tables(new ItemProficiencyEntry { Id = 4, BitMask = 8 }, new ItemProficiencyEntry { Id = 5, BitMask = 16 }, new ItemProficiencyEntry { Id = 30, BitMask = 0x300 });
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetMethodReturn(nameof(IPlayer.GetItemProficiencies), owned);
        var check = new PrerequisiteCheckItemProficiency(tables);
        Assert.Equal(expected, check.Meets(player, PrerequisiteComparison.Equal, id, 999, null));
        Assert.Equal(!expected, check.Meets(player, PrerequisiteComparison.NotEqual, id, 0, null));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidTableWithoutRequiredMaskMeansNoRestriction(bool missingRow)
    {
        var tables = missingRow ? Tables() : Tables(new ItemProficiencyEntry { Id = 1, BitMask = 0 });
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        var check = new PrerequisiteCheckItemProficiency(tables);
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1, 0, null));
        Assert.False(check.Meets(player, PrerequisiteComparison.NotEqual, 1, 0, null));
        Assert.Empty(proxy.GetInvocations(nameof(IPlayer.GetItemProficiencies)));
    }

    [Fact]
    public void MissingPlayerOrUnloadedTableStillFailsClosed()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        foreach (var mode in new[] { PrerequisiteComparison.Equal, PrerequisiteComparison.NotEqual })
        {
            Assert.False(new PrerequisiteCheckGender().Meets(null, mode, 0, 0, null));
            Assert.False(new PrerequisiteCheckItemProficiency(tables).Meets(player, mode, 1, 0, null));
            Assert.False(new PrerequisiteCheckItemProficiency(Tables()).Meets(null, mode, 1, 0, null));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void UnsupportedModesFailClosed(int mode)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        Assert.False(new PrerequisiteCheckGender().Meets(player, (PrerequisiteComparison)mode, 0, 0, null));
        Assert.False(new PrerequisiteCheckItemProficiency(Tables()).Meets(player, (PrerequisiteComparison)mode, 0, 0, null));
    }

    [Fact]
    public void BothChecksAreRegisteredForTheirNativeTypeIds()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGameTableManager>(Tables());
        services.AddGamePrerequisite();
        using var provider = services.BuildServiceProvider();
        Assert.IsType<PrerequisiteCheckItemProficiency>(provider.GetRequiredKeyedService<IPrerequisiteCheck>((PrerequisiteType)8));
        Assert.IsType<PrerequisiteCheckGender>(provider.GetRequiredKeyedService<IPrerequisiteCheck>((PrerequisiteType)10));
    }

    private static IGameTableManager Tables(params ItemProficiencyEntry[] entries)
    {
        var table = (GameTable<ItemProficiencyEntry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<ItemProficiencyEntry>));
        typeof(GameTable<ItemProficiencyEntry>).GetProperty(nameof(table.Entries))!.SetValue(table, entries);
        uint max = entries.Select(e => e.Id).DefaultIfEmpty().Max();
        typeof(GameTable<ItemProficiencyEntry>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table,
            new GameTableHeader { MaxId = max + 1u });
        int[] lookup = Enumerable.Repeat(-1, (int)max + 1).ToArray();
        for (int i = 0; i < entries.Length; i++) lookup[entries[i].Id] = i;
        typeof(GameTable<ItemProficiencyEntry>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, lookup);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.ItemProficiency), table);
        return tables;
    }
}
