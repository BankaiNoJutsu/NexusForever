using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.Reputation;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Network;

public class MatchingPacketShapeTests
{
    [Fact]
    public void ServerMatchingMatchJoined_WritesSharedFourteenBitMapId()
    {
        var packet = new ServerMatchingMatchJoined
        {
            MatchingGameMapId = 0x2AAAu
        };

        byte[] packetData = WritePacket(packet);

        Assert.Equal(2, packetData.Length);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x2AAAu, reader.ReadUInt(14u));
    }

    [Fact]
    public void ServerMatchingMatchParticipantCountUpdate_WritesSharedOneFlagShape()
    {
        var managerFlag = new ServerMatchingManagerFlag
        {
            Flag = true
        };
        var participantCount = new ServerMatchingMatchParticipantCountUpdate
        {
            Ally = true
        };
        var roleCheck = new ServerMatchingRoleCheckStarted
        {
            RolesRequired = true
        };

        byte[] managerFlagData = WritePacket(managerFlag);
        byte[] participantCountData = WritePacket(participantCount);
        byte[] roleCheckData = WritePacket(roleCheck);

        Assert.Single(managerFlagData);
        Assert.Single(participantCountData);
        Assert.Equal(managerFlagData, participantCountData);
        Assert.Equal(participantCountData, roleCheckData);

        using var reader = new GamePacketReader(new MemoryStream(managerFlagData));
        Assert.True(reader.ReadBit());
    }

    [Fact]
    public void ServerMatching0x05CF_WritesSharedRawUInt32Shape()
    {
        byte[] packetData = WritePacket(new ServerMatching0x05CF(0x11223344u));

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x11223344u, reader.ReadUInt());
    }

    [Fact]
    public void ServerMatchingAverageWaitTimeUpdate_WritesSharedUInt5UInt32Shape()
    {
        byte[] packetData = WritePacket(new ServerMatchingAverageWaitTimeUpdate
        {
            Type = MatchType.Dungeon,
            AverageWaitTime = 0x11223344u
        });

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(MatchType.Dungeon, reader.ReadEnum<MatchType>(5u));
        Assert.Equal(0x11223344u, reader.ReadUInt());
    }

    [Fact]
    public void ServerMatchingPenaltyUpdated_WritesFixedSixteenUInt32Slots()
    {
        var packet = new ServerMatchingPenaltyUpdated
        {
            MatchingPenaltyTimesMS = [0x11223344u, 0x55667788u]
        };

        byte[] packetData = WritePacket(packet);

        Assert.Equal(16 * sizeof(uint), packetData.Length);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(0x55667788u, reader.ReadUInt());

        for (int index = 2; index < 16; index++)
        {
            Assert.Equal(0u, reader.ReadUInt());
        }
    }

    [Fact]
    public void ServerMatchingMatchLeft_WritesSharedUInt5MatchType()
    {
        var packet = new ServerMatchingMatchLeft
        {
            Type = MatchType.PrimeLevelExpedition
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(MatchType.PrimeLevelExpedition, reader.ReadEnum<MatchType>(5u));
    }

    [Fact]
    public void ServerMatchingQueueResultAnnounce_WritesMappedResultAndStatusBits()
    {
        var packet = new ServerMatchingQueueResultAnnounce
        {
            Result = MatchingQueueResult.Requeueing,
            Status = MatchingQueueStatus.MatchReady
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(MatchingQueueResult.Requeueing, reader.ReadEnum<MatchingQueueResult>(6u));
        Assert.Equal(MatchingQueueStatus.MatchReady, reader.ReadEnum<MatchingQueueStatus>(4u));
    }

    [Fact]
    public void ServerMatchingMatchPvpPoolUpdated_WritesTwoUInt32LivesFields()
    {
        var packet = new ServerMatchingMatchPvpPoolUpdated
        {
            LivesRemainingTeam1 = 0x11223344u,
            LivesRemainingTeam2 = 0x55667788u
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(0x55667788u, reader.ReadUInt());
    }

    [Fact]
    public void ServerMatchingPvpKillNotification_WritesMappedOpponentVariants()
    {
        var packet = new ServerMatchingPvpKillNotification
        {
            KillerType = OpponentType.Player,
            KillerPlayer = new ServerMatchingPvpKillNotification.OpponentPlayer
            {
                Identity = new Identity { RealmId = 7, Id = 0x1122334455667788ul },
                Class = Class.Spellslinger
            },
            VictimType = OpponentType.Creature,
            VictimCreature = new ServerMatchingPvpKillNotification.OpponentCreature
            {
                UnitId = 0xAABBCCDDu,
                Creature2Id = 0x2AAAAu
            },
            VictimTeam = MatchTeam.Blue,
            DeathReason = PvpDeathReason.KilledByCreature
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(OpponentType.Player, reader.ReadEnum<OpponentType>(2u));
        Assert.Equal(7, reader.ReadUShort(14u));
        Assert.Equal(0x1122334455667788ul, reader.ReadULong());
        Assert.Equal(Class.Spellslinger, reader.ReadEnum<Class>(5u));
        Assert.Equal(OpponentType.Creature, reader.ReadEnum<OpponentType>(2u));
        Assert.Equal(0xAABBCCDDu, reader.ReadUInt());
        Assert.Equal(0x2AAAAu, reader.ReadUInt(18u));
        Assert.Equal(MatchTeam.Blue, reader.ReadEnum<MatchTeam>(2u));
        Assert.Equal(PvpDeathReason.KilledByCreature, reader.ReadEnum<PvpDeathReason>(3u));
    }

    [Fact]
    public void ServerMatchingQueueJoin_WritesMappedMapQueueAndRoleFields()
    {
        var packet = new ServerMatchingQueueJoin
        {
            MapData = new ServerMatchingQueueJoin.Map
            {
                MatchType = MatchType.Shiphand,
                MatchingGameMapIds = [0x01020304u, 0xA0B0C0D0u],
                MatchingGameType = 0x1234,
                QueueFlags = MatchingQueueFlags.GroupIsQueued | MatchingQueueFlags.Veteran
            },
            QueueData = new ServerMatchingQueueJoin.Queue
            {
                MatchType = MatchType.Shiphand,
                IsParty = true,
                QueueTime = 0x11223344u,
                AverageWaitTime = 0x55667788u
            },
            QueuedRoles = Role.Tank | Role.Healer
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(MatchType.Shiphand, reader.ReadEnum<MatchType>(5u));
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(0xA0B0C0D0u, reader.ReadUInt());
        Assert.Equal((ushort)0x1234, reader.ReadUShort(14u));
        Assert.Equal((uint)(MatchingQueueFlags.GroupIsQueued | MatchingQueueFlags.Veteran), reader.ReadUInt());
        Assert.Equal(MatchType.Shiphand, reader.ReadEnum<MatchType>(5u));
        Assert.True(reader.ReadBit());
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(0x55667788u, reader.ReadUInt());
        Assert.Equal((uint)(Role.Tank | Role.Healer), reader.ReadUInt());
    }

    [Fact]
    public void ServerMatchingQueueStatus_WritesMappedHeaderAndQueueBits()
    {
        var queuesJoined = new NetworkBitArray(16u, NetworkBitArray.BitOrder.LeastSignificantBit);
        queuesJoined.SetBit((uint)MatchType.Dungeon, true);
        queuesJoined.SetBit((uint)MatchType.Shiphand, true);

        var packet = new ServerMatchingQueueStatus
        {
            Status = MatchingQueueStatus.InQueue,
            JoinedMatchType = MatchType.Adventure,
            ReadyMatchType = MatchType.PrimeLevelExpedition,
            QueuesJoined = queuesJoined
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(MatchingQueueStatus.InQueue, reader.ReadEnum<MatchingQueueStatus>(4u));
        Assert.Equal(MatchType.Adventure, reader.ReadEnum<MatchType>(5u));
        Assert.Equal(MatchType.PrimeLevelExpedition, reader.ReadEnum<MatchType>(5u));

        for (uint index = 0; index < 16; index++)
        {
            bool expected = index == (uint)MatchType.Dungeon || index == (uint)MatchType.Shiphand;
            Assert.Equal(expected, reader.ReadBit());
        }
    }

    [Fact]
    public void MatchingReadyPackets_WriteSharedMatchTypeAndCountShape()
    {
        var matchReady = new ServerMatchingMatchReady
        {
            MatchType = MatchType.PrimeLevelDungeon,
            PendingAllies = 3u,
            PendingEnemies = 7u
        };
        var inProgressReady = new ServerMatchingMatchInProgressReady
        {
            MatchType = MatchType.PrimeLevelDungeon,
            PendingAllies = 3u,
            CurrentAllies = 7u
        };

        byte[] matchReadyData = WritePacket(matchReady);
        byte[] inProgressReadyData = WritePacket(inProgressReady);

        Assert.Equal(matchReadyData, inProgressReadyData);

        using var reader = new GamePacketReader(new MemoryStream(matchReadyData));
        Assert.Equal(MatchType.PrimeLevelDungeon, reader.ReadEnum<MatchType>(5u));
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(7u, reader.ReadUInt());
    }

    [Fact]
    public void ServerPveRatingUpdate_WritesPvpAlignedRatingCounters()
    {
        var packet = new ServerPveRatingUpdate
        {
            PveRatings =
            [
                new ServerPveRatingUpdate.PveRating
                {
                    Category = 0x5Au,
                    Type = MatchingGameRatingType.Warplot,
                    Rating = 0x11223344u,
                    Wins = 0x55667788u,
                    Losses = 0x99AABBCCu,
                    Draws = 0xDDEEFF00u
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((byte)0x5A, reader.ReadByte(8u));
        Assert.Equal(MatchingGameRatingType.Warplot, reader.ReadEnum<MatchingGameRatingType>(3u));
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(0x55667788u, reader.ReadUInt());
        Assert.Equal(0x99AABBCCu, reader.ReadUInt());
        Assert.Equal(0xDDEEFF00u, reader.ReadUInt());
    }

    [Fact]
    public void ServerMatchingListPlayersQueuedForMap_WritesMappedQueuedPlayerFields()
    {
        var packet = new ServerMatchingListPlayersQueuedForMap
        {
            MapId = 0x01020304u,
            QueuedPlayers =
            [
                new ServerMatchingListPlayersQueuedForMap.QueuedPlayerInfo
                {
                    Identity = new Identity { RealmId = 13, Id = 0x1122334455667788ul },
                    Name = "Queue Tester",
                    Faction = Faction.Exile,
                    Race = 0x123u,
                    Class = 0x456u,
                    Gender = 2u,
                    Level = 50u,
                    Path = 5u,
                    PrimeLevel = 0x0A0B0C0Du,
                    IsParty = true,
                    Roles = Role.Tank | Role.Healer,
                    TrailingUInt32_0x3C = 0u,
                    StatSlots =
                    [
                        new GroupMemberStatSlot { Value = 1, WireMarker0x30 = 48 },
                        new GroupMemberStatSlot { Value = 2, WireMarker0x30 = 48 },
                        new GroupMemberStatSlot { Value = 3, WireMarker0x30 = 48 },
                        new GroupMemberStatSlot { Value = 4, WireMarker0x30 = 48 },
                        new GroupMemberStatSlot { Value = 5, WireMarker0x30 = 48 }
                    ],
                    PrimeLevels =
                    [
                        new PrimeLevelInfo
                        {
                            WorldId = 0x1234,
                            PrimeLevelAchieved = 0x5678
                        }
                    ]
                }
            ]
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(13, reader.ReadUShort(14u));
        Assert.Equal(0x1122334455667788ul, reader.ReadULong());
        Assert.Equal("Queue Tester", reader.ReadWideString());
        Assert.Equal(Faction.Exile, reader.ReadEnum<Faction>(14u));
        Assert.Equal(0x123u, reader.ReadUInt(14u));
        Assert.Equal(0x456u, reader.ReadUInt(14u));
        Assert.Equal(2u, reader.ReadUInt(2u));
        Assert.Equal(50u, reader.ReadUInt());
        Assert.Equal(5u, reader.ReadUInt(3u));
        Assert.Equal(0x0A0B0C0Du, reader.ReadUInt());
        Assert.True(reader.ReadBit());
        Assert.Equal(Role.Tank | Role.Healer, reader.ReadEnum<Role>(32u));
        Assert.Equal(0u, reader.ReadUInt());

        for (ushort index = 1; index <= 5; index++)
        {
            Assert.Equal(index, reader.ReadUShort());
            Assert.Equal((byte)48, reader.ReadByte());
        }

        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal((ushort)0x1234, reader.ReadUShort(15u));
        Assert.Equal((ushort)0x5678, reader.ReadUShort());
    }

    [Fact]
    public void ServerMatchingMatchVoteKickBegin_WritesTwoIdentityPayloads()
    {
        var packet = new ServerMatchingMatchVoteKickBegin
        {
            Initiator = new Identity { RealmId = 3, Id = 0x0123456789ABCDEFul },
            MemberToKick = new Identity { RealmId = 11, Id = 0x0FEDCBA987654321ul }
        };

        byte[] packetData = WritePacket(packet);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(3, reader.ReadUShort(14u));
        Assert.Equal(0x0123456789ABCDEFul, reader.ReadULong());
        Assert.Equal(11, reader.ReadUShort(14u));
        Assert.Equal(0x0FEDCBA987654321ul, reader.ReadULong());
    }

    [Fact]
    public void MatchingVoteCooldownPackets_WriteSharedResultAndWaitTimeShape()
    {
        var kickCooldown = new ServerMatchingMatchKickCooldownUpdate
        {
            Result = MatchingQueueResult.PersonalKickCooldown,
            WaitTimeBeforeVoteMS = 0x11223344u
        };
        var operationResult = new ServerMatchingMatchOperationResult
        {
            Result = MatchingQueueResult.PersonalSurrenderCooldown,
            WaitTimeBeforeVoteMS = 0x55667788u
        };

        byte[] kickCooldownData = WritePacket(kickCooldown);
        byte[] operationResultData = WritePacket(operationResult);

        using (var reader = new GamePacketReader(new MemoryStream(kickCooldownData)))
        {
            Assert.Equal(MatchingQueueResult.PersonalKickCooldown, reader.ReadEnum<MatchingQueueResult>(6u));
            Assert.Equal(0x11223344u, reader.ReadUInt());
        }

        using var operationReader = new GamePacketReader(new MemoryStream(operationResultData));
        Assert.Equal(MatchingQueueResult.PersonalSurrenderCooldown, operationReader.ReadEnum<MatchingQueueResult>(6u));
        Assert.Equal(0x55667788u, operationReader.ReadUInt());
    }

    [Fact]
    public void ClientMatchingQueuePackets_ReadSharedMapRoleAndPrimeLevelFields()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(MatchType.Shiphand, 5u);
            writer.Write(2u);
            writer.Write(0x01020304u);
            writer.Write(0xA0B0C0D0u);
            writer.Write((ushort)0x1234, 14u);
            writer.Write((uint)(MatchingQueueFlags.GroupIsQueued | MatchingQueueFlags.Veteran));
            writer.Write((uint)(Role.Tank | Role.Healer));
            writer.Write(0x55667788u);
        });

        ClientMatchingQueue queue = ReadPacket<ClientMatchingQueue>(packetData);
        ClientMatchingQueueParty party = ReadPacket<ClientMatchingQueueParty>(packetData);

        Assert.Equal(MatchType.Shiphand, queue.MapData.MatchType);
        Assert.Equal(new uint[] { 0x01020304u, 0xA0B0C0D0u }, queue.MapData.Maps);
        Assert.Equal((ushort)0x1234, queue.MapData.MatchingGameTypeId);
        Assert.Equal(MatchingQueueFlags.GroupIsQueued | MatchingQueueFlags.Veteran, queue.MapData.QueueFlags);
        Assert.Equal(Role.Tank | Role.Healer, queue.Roles);
        Assert.Equal(0x55667788u, queue.PrimeLevel);

        Assert.Equal(MatchType.Shiphand, party.MapData.MatchType);
        Assert.Equal(new uint[] { 0x01020304u, 0xA0B0C0D0u }, party.MapData.Maps);
        Assert.Equal((ushort)0x1234, party.MapData.MatchingGameTypeId);
        Assert.Equal(MatchingQueueFlags.GroupIsQueued | MatchingQueueFlags.Veteran, party.MapData.QueueFlags);
        Assert.Equal(Role.Tank | Role.Healer, party.Roles);
        Assert.Equal(0x55667788u, party.PrimeLevel);
    }

    [Fact]
    public void ClientMatchingQueueRandomPackets_ReadSharedMatchTypeFlagsAndRoles()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(MatchType.PrimeLevelDungeon, 5u);
            writer.Write((uint)(MatchingQueueFlags.GroupIsQueued | MatchingQueueFlags.Veteran));
            writer.Write((uint)(Role.Tank | Role.DPS));
        });

        ClientMatchingQueueRandom random = ReadPacket<ClientMatchingQueueRandom>(packetData);
        ClientMatchingQueueRandomParty randomParty = ReadPacket<ClientMatchingQueueRandomParty>(packetData);

        Assert.Equal(MatchType.PrimeLevelDungeon, random.MatchType);
        Assert.Equal(MatchingQueueFlags.GroupIsQueued | MatchingQueueFlags.Veteran, random.Flags);
        Assert.Equal(Role.Tank | Role.DPS, random.Roles);

        Assert.Equal(MatchType.PrimeLevelDungeon, randomParty.MatchType);
        Assert.Equal(MatchingQueueFlags.GroupIsQueued | MatchingQueueFlags.Veteran, randomParty.Flags);
        Assert.Equal(Role.Tank | Role.DPS, randomParty.Roles);
    }

    [Fact]
    public void MatchingVoteTerminalPackets_WriteSharedEmptyPayloads()
    {
        Assert.Empty(WritePacket(new ServerMatchingMatchVoteKickCancelled()));
        Assert.Empty(WritePacket(new ServerMatchingMatchVoteKickFailed()));
        Assert.Empty(WritePacket(new ServerMatchingMatchVoteKickSucceeded()));
        Assert.Empty(WritePacket(new ServerMatchingMatchVoteSurrenderFailed()));
        Assert.Empty(WritePacket(new ServerMatchingMatchVoteSurrenderBegin()));
    }

    [Fact]
    public void ClientMatchingTransferIntoMatch_ReadsEmptyPayload()
    {
        ClientMatchingTransferIntoMatch packet = ReadPacket<ClientMatchingTransferIntoMatch>([]);

        Assert.NotNull(packet);
    }

    [Fact]
    public void ClientMatchingMatchCastVoteKick_ReadsIdentityAndVoteBit()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write((ushort)17, 14u);
            writer.Write(0x0123456789ABCDEFul);
            writer.Write(true);
        });

        ClientMatchingMatchCastVoteKick packet = ReadPacket<ClientMatchingMatchCastVoteKick>(packetData);

        Assert.Equal((ushort)17, packet.MemberToKick.RealmId);
        Assert.Equal(0x0123456789ABCDEFul, packet.MemberToKick.Id);
        Assert.True(packet.Vote);
    }

    [Fact]
    public void ClientMatchingMatchCastVoteSurrender_ReadsVoteBit()
    {
        byte[] packetData = WritePacket(writer => writer.Write(true));

        ClientMatchingMatchCastVoteSurrender packet = ReadPacket<ClientMatchingMatchCastVoteSurrender>(packetData);

        Assert.True(packet.Vote);
    }

    [Fact]
    public void ClientMatchingReadyAndVoteInitiationPackets_ReadMappedShapes()
    {
        ClientMatchingGameReadyResponse readyResponse = ReadPacket<ClientMatchingGameReadyResponse>(WritePacket(writer => writer.Write(true)));

        byte[] voteKickData = WritePacket(writer =>
        {
            writer.Write((ushort)23, 14u);
            writer.Write(0x0FEDCBA987654321ul);
        });
        ClientMatchingMatchInitiateVoteToKick voteKick = ReadPacket<ClientMatchingMatchInitiateVoteToKick>(voteKickData);
        ClientMatchingMatchInitiateVoteToSurrender voteSurrender = ReadPacket<ClientMatchingMatchInitiateVoteToSurrender>([]);
        ClientMatchingMatchInitiateLookingForReplacements replacements = ReadPacket<ClientMatchingMatchInitiateLookingForReplacements>(WritePacket(writer => writer.Write((uint)(Role.Tank | Role.DPS))));

        Assert.True(readyResponse.Response);
        Assert.Equal((ushort)23, voteKick.MemberToKick.RealmId);
        Assert.Equal(0x0FEDCBA987654321ul, voteKick.MemberToKick.Id);
        Assert.NotNull(voteSurrender);
        Assert.Equal(Role.Tank | Role.DPS, replacements.Roles);
    }

    [Fact]
    public void ClientMatchingRoleCheckResponse_ReadsMatchTypeRolesAndResponse()
    {
        byte[] packetData = WritePacket(writer =>
        {
            writer.Write(MatchType.PrimeLevelDungeon, 5u);
            writer.Write((uint)(Role.Tank | Role.Healer));
            writer.Write(true);
        });

        ClientMatchingRoleCheckResponse packet = ReadPacket<ClientMatchingRoleCheckResponse>(packetData);

        Assert.Equal(MatchType.PrimeLevelDungeon, packet.Type);
        Assert.Equal(Role.Tank | Role.Healer, packet.Roles);
        Assert.True(packet.Response);
    }

    [Fact]
    public void ClientMatchingQueueLeavePackets_ReadSharedMatchTypePayload()
    {
        byte[] packetData = WritePacket(writer => writer.Write(MatchType.Shiphand, 5u));

        ClientMatchingQueueLeave leave = ReadPacket<ClientMatchingQueueLeave>(packetData);
        ClientMatchingQueueLeaveAsGroup leaveAsGroup = ReadPacket<ClientMatchingQueueLeaveAsGroup>(packetData);

        Assert.Equal(MatchType.Shiphand, leave.MatchType);
        Assert.Equal(MatchType.Shiphand, leaveAsGroup.MatchType);
    }

    [Fact]
    public void MatchingServerStatusPackets_WriteSharedUInt32AndEmptyShapes()
    {
        const uint sharedValue = 0x11223344u;

        byte[] eligibilityData = WritePacket(new ServerMatchingEligibilityChanged
        {
            MatchingEligibilityFlags = sharedValue
        });
        byte[] enteredData = WritePacket(new ServerMatchingMatchEntered
        {
            MatchingGameMapId = sharedValue
        });

        Assert.Equal(eligibilityData, enteredData);

        using var reader = new GamePacketReader(new MemoryStream(eligibilityData));
        Assert.Equal(sharedValue, reader.ReadUInt());

        Assert.Empty(WritePacket(new ServerMatchingMatchReadyCancel()));
        Assert.Empty(WritePacket(new ServerMatchingMatchFinished()));
        Assert.Empty(WritePacket(new ServerMatchingMatchExited()));
        Assert.Empty(WritePacket(new ServerMatchingGroupIsQueued()));
        Assert.Empty(WritePacket(new ServerMatchingLeftQueue()));
    }

    [Fact]
    public void ServerMatchingGroupMemberRoleSelection_WritesHousingReservationShape()
    {
        var matchingPacket = new ServerMatchingGroupMemberRoleSelection
        {
            Identity = new Identity { RealmId = 0x1234, Id = 0x0102030405060708ul },
            TrailingValue = (uint)Role.DPS
        };
        var housingPacket = new ServerHousingCommunityPlotReservation
        {
            PlotIndex = (uint)Role.DPS
        };
        housingPacket.TargetResidence.RealmId = 0x1234;
        housingPacket.TargetResidence.ResidenceId = 0x0102030405060708ul;

        byte[] matchingData = WritePacket(matchingPacket);
        byte[] housingData = WritePacket(housingPacket.Write);

        Assert.Equal(housingData, matchingData);

        using var reader = new GamePacketReader(new MemoryStream(matchingData));
        Assert.Equal((ushort)0x1234, reader.ReadUShort(14u));
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal((uint)Role.DPS, reader.ReadUInt());
    }

    [Fact]
    public void ClientMatchingEmptyLeavePackets_ReadEmptyPayloads()
    {
        ClientMatchingQueueLeaveAll leaveAll = ReadPacket<ClientMatchingQueueLeaveAll>([]);
        ClientMatchingMatchLeave matchLeave = ReadPacket<ClientMatchingMatchLeave>([]);
        ClientMatchingStopLookingForReplacements stopLooking = ReadPacket<ClientMatchingStopLookingForReplacements>([]);

        Assert.NotNull(leaveAll);
        Assert.NotNull(matchLeave);
        Assert.NotNull(stopLooking);
    }

    private static byte[] WritePacket(IWritable packet)
    {
        return WritePacket(packet.Write);
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }

    private static T ReadPacket<T>(byte[] packetData)
        where T : IReadable, new()
    {
        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var packet = new T();
        packet.Read(reader);

        return packet;
    }
}
