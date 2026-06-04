using NexusForever.Game.Static.Entity;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;
using NexusForever.Game.Static.Reputation;
using NexusForever.Network.Internal.Message.Group.Shared;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.Internal.Message.Shared;
using NexusForever.WorldServer.Network.Internal.Handler.Group;
using Class = NexusForever.Game.Static.Entity.Class;
using Race = NexusForever.Game.Static.Entity.Race;
using Sex = NexusForever.Game.Static.Entity.Sex;

namespace NexusForever.Game.Tests.Network;

public class GroupMemberMappingExtensionsTests
{
    [Fact]
    public void ToNetworkGroupMemberStatUpdate_MapsInterruptArmorAndGroupIndex()
    {
        var member = new GroupMember
        {
            Identity   = new Identity { RealmId = 1, Id = 42ul },
            GroupIndex = 3,
            Character  = new GroupCharacter
            {
                Name               = "Test",
                RealmName          = "TestRealm",
                Faction            = Faction.Exile,
                Race               = Race.Human,
                Class              = Class.Esper,
                Sex                = Sex.Male,
                Level              = 50,
                EffectiveLevel     = 50,
                Path               = PlayerPath.Soldier,
                Health             = 800f,
                HealthMax          = 1000f,
                Shield             = 100f,
                ShieldMax          = 200f,
                InterruptArmour    = 4f,
                InterruptArmourMax = 8f,
                Absorption         = 0f,
                AbsorptionMax      = 0f,
                Focus              = 250f,
                FocusMax           = 300f,
                HealingAbsorb      = 0f,
                HealingAbsorbMax   = 0f,
                PhaseFlags1        = 1,
                PhaseFlags2        = 2,
            }
        };

        ServerGroupMemberStatUpdate update = member.ToNetworkGroupMemberStatUpdate(99ul);

        Assert.Equal(99ul, update.GroupId);
        Assert.Equal(3, update.GroupMemberId);
        Assert.Equal(4f, update.InterruptArmor);
        Assert.Equal(8f, update.InterruptArmorMax);
        Assert.Equal(800f, update.Health);

        ServerGroupRosterUpdate roster = member.ToNetworkGroupRosterUpdate(99ul);
        Assert.Equal(3, roster.GroupMemberId);
        Assert.Equal(4f, roster.InterruptArmor);
        Assert.Equal(8f, roster.InterruptArmorMax);
    }
}
