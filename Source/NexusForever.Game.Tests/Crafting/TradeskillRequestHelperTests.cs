using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.WorldServer.Network.Message.Handler.Crafting;

namespace NexusForever.Game.Tests.Crafting;

public class TradeskillRequestHelperTests
{
    private const uint TradeskillBonusId = 4008u;
    private const uint TradeskillTierId = 4011u;

    [Fact]
    public void ValidateTradeskill_WhenTradeskillTableMissingThrowsInvalidPacket()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);

        Assert.Throws<InvalidPacketValueException>(() => TradeskillRequestHelper.ValidateTradeskill(tables, TradeskillType.Armorer));
    }

    [Fact]
    public void ValidateTradeskill_WhenNoneAllowedAndTableMissingDoesNotThrow()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);

        TradeskillRequestHelper.ValidateTradeskill(tables, 0, allowNone: true);
    }

    [Fact]
    public void ValidateBonusForTradeskill_WhenBonusTableMissingThrowsInvalidPacket()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);

        Assert.Throws<InvalidPacketValueException>(() => TradeskillRequestHelper.ValidateBonusForTradeskill(tables, TradeskillType.Armorer, TradeskillBonusId));
    }

    [Fact]
    public void ValidateBonusForTradeskill_WhenTalentTierTableMissingThrowsInvalidPacket()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.TradeskillBonus), CreateGameTable(new TradeskillBonusEntry
        {
            Id = TradeskillBonusId,
            TradeSkillTierId = TradeskillTierId
        }));

        Assert.Throws<InvalidPacketValueException>(() => TradeskillRequestHelper.ValidateBonusForTradeskill(tables, TradeskillType.Armorer, TradeskillBonusId));
    }

    [Fact]
    public void ValidateBonusForTradeskill_WhenTierContainsBonusDoesNotThrow()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.TradeskillBonus), CreateGameTable(new TradeskillBonusEntry
        {
            Id = TradeskillBonusId,
            TradeSkillTierId = TradeskillTierId
        }));
        proxy.SetProperty(nameof(IGameTableManager.TradeskillTalentTier), CreateGameTable(new TradeskillTalentTierEntry
        {
            Id = TradeskillTierId,
            TradeSkillId = (uint)TradeskillType.Armorer,
            TradeSkillBonusId00 = TradeskillBonusId
        }));

        TradeskillRequestHelper.ValidateBonusForTradeskill(tables, TradeskillType.Armorer, TradeskillBonusId);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Public | BindingFlags.Instance);
        return idField == null ? 0u : (uint)idField.GetValue(entry)!;
    }

    private static void SetAutoProperty<T>(T target, string propertyName, object value)
    {
        PropertyInfo property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property?.SetValue(target, value);
    }

    private static void SetPrivateField<T>(T target, string fieldName, object value)
    {
        FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}
