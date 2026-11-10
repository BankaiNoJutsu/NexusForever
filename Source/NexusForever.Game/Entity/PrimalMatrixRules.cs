using System.Runtime.CompilerServices;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// The Primal Matrix allocation rules: which node may take an allocation and what it costs.
    /// </summary>
    /// <remarks>
    /// See Decomp/Analysis/PRIMAL_MATRIX_RULES_20261110.md. Layout from the table (axial hex coordinates, checked by the table's own cluster structure); availability from the
    /// client (<c>1406e4fc0</c>: a node is open when it is the start node or any of its six neighbours holds an allocation); cost per allocation from the client
    /// (<c>1406e4e00</c>). These are the client's rules: that the retired server enforced the same ones is assumed. No retail parity claim.
    /// </remarks>
    public static class PrimalMatrixRules
    {
        public enum Result
        {
            Ok,
            EmptyRequest,
            UnknownNode,
            OverMaximum,
            Locked
        }

        /// <summary>
        /// Essence cost by colour, in the order of the PrimalMatrixNode cost columns.
        /// </summary>
        public readonly record struct Cost(ulong Red, ulong Blue, ulong Green, ulong Purple);

        /// <summary>
        /// PrimalMatrixRewardType, the client's named enum (read from its registration table).
        /// </summary>
        public enum RewardType : uint
        {
            None           = 0,
            RewardProperty = 1,
            AbilityPoint   = 2,
            AmpPoint       = 3,
            AbilityUnlock  = 4,
            UnitProperty   = 5
        }

        public readonly record struct Reward(RewardType Type, uint ObjectId, uint SubObjectId, float Value);

        private const uint StartNodeFlag = 0x1u;

        private static readonly (int X, int Y)[] neighbourOffsets = [(1, 0), (-1, 0), (0, 1), (0, -1), (1, -1), (-1, 1)];

        private static readonly ConditionalWeakTable<GameTable<PrimalMatrixNodeEntry>, Dictionary<(int, int), PrimalMatrixNodeEntry>> positionCache = new();

        /// <summary>
        /// Return the nodes next to the supplied one on the hexagonal lattice.
        /// </summary>
        public static IEnumerable<PrimalMatrixNodeEntry> GetNeighbours(PrimalMatrixNodeEntry node, GameTable<PrimalMatrixNodeEntry> nodes)
        {
            if (node == null || nodes == null)
                yield break;

            Dictionary<(int, int), PrimalMatrixNodeEntry> byPosition = positionCache.GetValue(nodes,
                table => table.Entries.Where(e => e != null).ToDictionary(e => ((int)e.PositionX, (int)e.PositionY)));

            foreach ((int x, int y) in neighbourOffsets)
                if (byPosition.TryGetValue(((int)node.PositionX + x, (int)node.PositionY + y), out PrimalMatrixNodeEntry neighbour))
                    yield return neighbour;
        }

        /// <summary>
        /// Validate a save request against the saved allocations and return the allocations and the cost it would lead to.
        /// </summary>
        /// <remarks>
        /// A request is a list of nodes with the number of allocations added to each since the last save. The order within one request is not significant: a node may be opened
        /// by another node of the same request.
        /// </remarks>
        public static Result TryApply(
            IReadOnlyDictionary<uint, byte> saved,
            IEnumerable<(uint NodeId, byte Increment)> request,
            GameTable<PrimalMatrixNodeEntry> nodes,
            out Dictionary<uint, byte> allocations,
            out Cost cost)
        {
            allocations = new Dictionary<uint, byte>(saved ?? new Dictionary<uint, byte>());
            cost = default;

            var increments = new Dictionary<uint, uint>();
            foreach ((uint nodeId, byte increment) in request ?? [])
                if (increment != 0)
                    increments[nodeId] = increments.GetValueOrDefault(nodeId) + increment;

            if (increments.Count == 0)
                return Result.EmptyRequest;

            ulong red = 0ul, blue = 0ul, green = 0ul, purple = 0ul;
            var pending = new List<PrimalMatrixNodeEntry>();
            foreach ((uint nodeId, uint increment) in increments)
            {
                PrimalMatrixNodeEntry node = nodes?.GetEntry(nodeId);
                if (node == null)
                    return Result.UnknownNode;

                uint total = allocations.GetValueOrDefault(nodeId) + increment;
                if (total > node.MaxAllocations || total > byte.MaxValue)
                    return Result.OverMaximum;

                red    += (ulong)node.CostRedEssence * increment;
                blue   += (ulong)node.CostBlueEssence * increment;
                green  += (ulong)node.CostGreenEssence * increment;
                purple += (ulong)node.CostPurpleEssence * increment;

                // a node that already holds an allocation can always take more; a new one has to be open
                if (allocations.GetValueOrDefault(nodeId) == 0)
                    pending.Add(node);
            }

            var allocated = new HashSet<uint>(allocations.Where(a => a.Value != 0).Select(a => a.Key));
            bool progress = true;
            while (pending.Count != 0 && progress)
            {
                progress = false;
                foreach (PrimalMatrixNodeEntry node in pending.ToArray())
                {
                    if ((node.Flags & StartNodeFlag) == 0u && !GetNeighbours(node, nodes).Any(n => allocated.Contains(n.Id)))
                        continue;

                    allocated.Add(node.Id);
                    pending.Remove(node);
                    progress = true;
                }
            }

            if (pending.Count != 0)
                return Result.Locked;

            foreach ((uint nodeId, uint increment) in increments)
                allocations[nodeId] = (byte)(allocations.GetValueOrDefault(nodeId) + increment);

            cost = new Cost(red, blue, green, purple);
            return Result.Ok;
        }

        /// <summary>
        /// Return the rewards of a node for the supplied class.
        /// </summary>
        public static IEnumerable<Reward> GetRewards(PrimalMatrixNodeEntry node, Class @class, GameTable<PrimalMatrixRewardEntry> rewards)
        {
            uint rewardId = node == null ? 0u : @class switch
            {
                Class.Warrior      => node.PrimalMatrixRewardIdWarrior,
                Class.Engineer     => node.PrimalMatrixRewardIdEngineer,
                Class.Esper        => node.PrimalMatrixRewardIdEsper,
                Class.Medic        => node.PrimalMatrixRewardIdMedic,
                Class.Stalker      => node.PrimalMatrixRewardIdStalker,
                Class.Spellslinger => node.PrimalMatrixRewardIdSpellslinger,
                _                  => 0u
            };

            PrimalMatrixRewardEntry entry = rewardId == 0u ? null : rewards?.GetEntry(rewardId);
            if (entry == null)
                yield break;

            (uint Type, uint ObjectId, uint SubObjectId, float Value)[] slots =
            [
                (entry.PrimalMatrixRewardTypeEnum0, entry.ObjectId0, entry.SubObjectId0, entry.Value0),
                (entry.PrimalMatrixRewardTypeEnum1, entry.ObjectId1, entry.SubObjectId1, entry.Value1),
                (entry.PrimalMatrixRewardTypeEnum2, entry.ObjectId2, entry.SubObjectId2, entry.Value2),
                (entry.PrimalMatrixRewardTypeEnum3, entry.ObjectId3, entry.SubObjectId3, entry.Value3)
            ];

            foreach ((uint type, uint objectId, uint subObjectId, float value) in slots)
                if (type != (uint)RewardType.None)
                    yield return new Reward((RewardType)type, objectId, subObjectId, value);
        }
    }
}
