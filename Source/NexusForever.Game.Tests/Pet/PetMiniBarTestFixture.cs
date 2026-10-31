using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.Game.Tests.Pet;

public interface IMiniBarTestPlayer : IPlayer, IPlayerMapAdmission, IPlayerPetMiniBar { }

internal sealed class PetMiniBarTestFixture
{
    public IMiniBarTestPlayer Owner { get; }
    public RecordingDispatchProxy<IMiniBarTestPlayer> Player { get; }
    public RecordingDispatchProxy<IGameSession> Session { get; }
    public RecordingDispatchProxy<IBaseMap> Map { get; }
    public RecordingDispatchProxy<IGameTableManager> Tables { get; }
    public Dictionary<uint, IGridEntity> Entities { get; } = [];
    public List<object> Packets { get; } = [];
    public List<uint> Removals { get; } = [];
    public PetMiniBarManager Manager { get; }
    public EntitySummonFactory Factory { get; }
    public ulong Generation = 1;
    public bool AdmissionCurrent = true;
    public Action<object> PacketHook;

    public PetMiniBarTestFixture()
    {
        Owner = RecordingDispatchProxy<IMiniBarTestPlayer>.Create(out var player);
        Player = player;
        var session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        Session = sessionProxy;
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        Map = mapProxy;
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        Tables = tableProxy;
        Tables.SetProperty(nameof(IGameTableManager.ActionBarShortcutSet), PlayerBindPointTests.Table(DismissRow()));
        Factory = new EntitySummonFactory(RecordingDispatchProxy<IEntityFactory>.Create(out _));
        Factory.Initialise(Owner);
        Player.SetProperty(nameof(IPlayer.Guid), 42u);
        Player.SetProperty(nameof(IPlayer.Map), map);
        Player.SetProperty(nameof(IPlayer.Session), session);
        Player.SetProperty(nameof(IPlayer.SummonFactory), Factory);
        Player.SetProperty(nameof(IPlayer.SpellManager), RecordingDispatchProxy<ISpellManager>.Create(out _));
        Player.SetProperty(nameof(IPlayerMapAdmission.MapAdmissionSyncRoot), new object());
        Player.SetMethodHandler("get_MapAdmissionGeneration", _ => Generation);
        Player.SetMethodHandler(nameof(IPlayerMapAdmission.IsMapAdmissionCurrent), args =>
            AdmissionCurrent && (ulong)args[0] == Generation);
        Map.SetMethodHandler(nameof(IBaseMap.GetEntity), args => Entities.GetValueOrDefault((uint)args[0]));
        Session.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            Packets.Add(args[0]);
            PacketHook?.Invoke(args[0]);
            return null;
        });
        Entities[42] = Owner;
        Manager = new PetMiniBarManager(Owner, tables);
        Player.SetMethodHandler(nameof(IPlayerPetMiniBar.TryBindPetMiniBar), args => Manager.TryBind((IWorldEntity)args[0]));
        Player.SetMethodHandler(nameof(IPlayerPetMiniBar.UnbindPetMiniBar), args => { Manager.Unbind((IWorldEntity)args[0]); return null; });
        Player.SetMethodHandler(nameof(IPlayerPetMiniBar.TryDismissPetMiniBar), args => Manager.TryDismiss((ShortcutSet)args[0], (uint)args[1]));
    }

    public IUnitEntity Pet(uint guid = 100, uint creature = 42683)
    {
        var pet = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.Guid), guid);
        proxy.SetProperty(nameof(IWorldEntity.CreatureId), creature);
        proxy.SetProperty(nameof(IWorldEntity.SummonerGuid), Owner.Guid);
        proxy.SetProperty(nameof(IGridEntity.Map), Owner.Map);
        proxy.SetProperty(nameof(IGridEntity.InWorld), true);
        proxy.SetMethodHandler(nameof(IGridEntity.RemoveFromMap), _ => { Removals.Add(guid); return null; });
        Entities[guid] = pet;
        Factory.TrackSummon(pet);
        return pet;
    }

    public static ActionBarShortcutSetEntry DismissRow() => new() { Id = 499, ShortcutType00 = 11, ObjectId00 = 2 };
    public ServerActionBarSet[] Bars => Packets.OfType<ServerActionBarSet>().ToArray();
}
