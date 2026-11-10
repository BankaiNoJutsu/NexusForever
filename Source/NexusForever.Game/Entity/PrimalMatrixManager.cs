using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Account.Currency;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.PrimalMatrix;
using NLog;

namespace NexusForever.Game.Entity
{
    /// <summary>
    /// The character's Primal Matrix allocations.
    /// </summary>
    /// <remarks>
    /// Rules and evidence: Decomp/Analysis/PRIMAL_MATRIX_RULES_20261110.md and <see cref="PrimalMatrixRules"/>. Server-side choices that come from no source:
    /// <list type="bullet">
    /// <item>The four cost columns are spent from the account currencies Crimson, Cobalt, Viridian and Violet Essence (red, blue, green, purple by name).</item>
    /// <item>A UnitProperty reward is added once per allocation of its node (launch coverage says extra essence in a multi-tier node boosts its benefit; nothing native was read).</item>
    /// <item>AMPPoint rewards are a term of their own: the snapshot's last word is the AMP power the matrix contributes (the client stores it apart from the bonus power),
    /// and the same total is added to every action set on top of the base and bonus power.</item>
    /// <item>AbilityPoint rewards are a term of their own as well: the total is added to every action set on top of the base and bonus points and counted in the published
    /// ability point total and in the action set change limit. That the retired server published them through the same total is assumed.</item>
    /// </list>
    /// No retail parity claim.
    /// </remarks>
    public class PrimalMatrixManager : IPrimalMatrixManager
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly object gate = new();
        private readonly IPlayer player;
        private readonly IGameTableManager gameTableManager;
        private readonly ulong characterId;
        private readonly Dictionary<uint, byte> allocations = [];
        private readonly Dictionary<uint, byte> persisted = [];

        /// <summary>
        /// Create a new <see cref="IPrimalMatrixManager"/> from an existing database model.
        /// </summary>
        public PrimalMatrixManager(IPlayer player, CharacterModel characterModel, IGameTableManager gameTableManager)
        {
            this.player           = player;
            this.gameTableManager = gameTableManager;
            characterId           = characterModel.Id;

            foreach (CharacterPrimalMatrixNodeModel model in characterModel.PrimalMatrixNode)
            {
                if (model.Allocations == 0)
                    continue;

                allocations[model.NodeId] = model.Allocations;
                persisted[model.NodeId]   = model.Allocations;
            }

            ApplyPropertyRewards();
            ApplyAmpPower();
            ApplyTierPoints();
        }

        public byte GetAllocations(uint nodeId)
        {
            lock (gate)
                return allocations.GetValueOrDefault(nodeId);
        }

        public bool TryAllocate(IReadOnlyList<(uint NodeId, byte Increment)> request)
        {
            Dictionary<uint, byte> before;
            Dictionary<uint, byte> after;
            lock (gate)
            {
                before = new Dictionary<uint, byte>(allocations);
                PrimalMatrixRules.Result result = PrimalMatrixRules.TryApply(before, request, gameTableManager?.PrimalMatrixNode, out after, out PrimalMatrixRules.Cost cost);
                if (result != PrimalMatrixRules.Result.Ok)
                {
                    log.Debug($"Primal Matrix save for character {characterId} rejected: {result}.");
                    return false;
                }

                IAccountCurrencyManager wallet = player.Account?.CurrencyManager;
                if (wallet == null || !CanAfford(wallet, cost))
                {
                    log.Debug($"Primal Matrix save for character {characterId} rejected: cannot afford {cost}.");
                    return false;
                }

                Spend(wallet, AccountCurrencyType.CrimsonEssence, cost.Red);
                Spend(wallet, AccountCurrencyType.CobaltEssence, cost.Blue);
                Spend(wallet, AccountCurrencyType.ViridianEssence, cost.Green);
                Spend(wallet, AccountCurrencyType.VioletEssence, cost.Purple);

                allocations.Clear();
                foreach ((uint nodeId, byte count) in after)
                    allocations[nodeId] = count;
            }

            GrantOneTimeRewards(before, after);
            ApplyPropertyRewards();
            ApplyAmpPower();
            if (ApplyTierPoints())
                player.SpellManager?.SendServerAbilityPoints();
            player.RequestSave();
            return true;
        }

        private static bool CanAfford(IAccountCurrencyManager wallet, PrimalMatrixRules.Cost cost)
        {
            return (cost.Red == 0ul || wallet.CanAfford(AccountCurrencyType.CrimsonEssence, cost.Red))
                && (cost.Blue == 0ul || wallet.CanAfford(AccountCurrencyType.CobaltEssence, cost.Blue))
                && (cost.Green == 0ul || wallet.CanAfford(AccountCurrencyType.ViridianEssence, cost.Green))
                && (cost.Purple == 0ul || wallet.CanAfford(AccountCurrencyType.VioletEssence, cost.Purple));
        }

        private static void Spend(IAccountCurrencyManager wallet, AccountCurrencyType type, ulong amount)
        {
            if (amount != 0ul)
                wallet.CurrencySubtractAmount(type, amount);
        }

        /// <summary>
        /// Grant the rewards that are given once, for the allocations added by a save: ability unlocks.
        /// </summary>
        private void GrantOneTimeRewards(IReadOnlyDictionary<uint, byte> before, IReadOnlyDictionary<uint, byte> after)
        {
            foreach ((uint nodeId, byte count) in after)
            {
                int added = count - before.GetValueOrDefault(nodeId);
                if (added <= 0)
                    continue;

                PrimalMatrixNodeEntry node = gameTableManager?.PrimalMatrixNode?.GetEntry(nodeId);
                foreach (PrimalMatrixRules.Reward reward in PrimalMatrixRules.GetRewards(node, player.Class, gameTableManager?.PrimalMatrixReward))
                {
                    switch (reward.Type)
                    {
                        case PrimalMatrixRules.RewardType.AbilityUnlock:
                        {
                            Spell4Entry spell = gameTableManager.Spell4?.GetEntry(reward.ObjectId);
                            if (spell != null && player.SpellManager?.GetSpell(spell.Spell4BaseIdBaseSpell) == null)
                                player.SpellManager?.AddSpell(spell.Spell4BaseIdBaseSpell);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recompute the property totals of every allocated node and hand them to the player. Derived from the allocations, so it is applied on load as well.
        /// </summary>
        private void ApplyPropertyRewards()
        {
            var totals = new Dictionary<Property, float>();
            lock (gate)
            {
                foreach ((uint nodeId, byte count) in allocations)
                {
                    PrimalMatrixNodeEntry node = gameTableManager?.PrimalMatrixNode?.GetEntry(nodeId);
                    foreach (PrimalMatrixRules.Reward reward in PrimalMatrixRules.GetRewards(node, player.Class, gameTableManager?.PrimalMatrixReward))
                    {
                        if (reward.Type != PrimalMatrixRules.RewardType.UnitProperty || !float.IsFinite(reward.Value))
                            continue;

                        var property = (Property)reward.ObjectId;
                        totals[property] = totals.GetValueOrDefault(property) + reward.Value * count;
                    }
                }
            }

            player.SetPrimalMatrixProperties(totals);
        }

        /// <summary>
        /// The AMP power the allocated nodes grant in total.
        /// </summary>
        private ushort GetAmpPower()
        {
            return (ushort)Math.Clamp(GetRewardTotal(PrimalMatrixRules.RewardType.AmpPoint), 0f, ushort.MaxValue);
        }

        /// <summary>
        /// The summed value of one counted reward type over every allocation.
        /// </summary>
        private float GetRewardTotal(PrimalMatrixRules.RewardType type)
        {
            float total = 0f;
            lock (gate)
            {
                foreach ((uint nodeId, byte count) in allocations)
                {
                    PrimalMatrixNodeEntry node = gameTableManager?.PrimalMatrixNode?.GetEntry(nodeId);
                    foreach (PrimalMatrixRules.Reward reward in PrimalMatrixRules.GetRewards(node, player.Class, gameTableManager?.PrimalMatrixReward))
                        if (reward.Type == type && float.IsFinite(reward.Value))
                            total += reward.Value * count;
                }
            }

            return total;
        }

        private bool ApplyTierPoints()
        {
            byte points = (byte)Math.Clamp(GetRewardTotal(PrimalMatrixRules.RewardType.AbilityPoint), 0f, 100f);
            return player.SpellManager?.SetPrimalMatrixTierPoints(points) ?? false;
        }

        private void ApplyAmpPower()
        {
            player.SpellManager?.SetPrimalMatrixAmpPower(GetAmpPower());
        }

        public void SendSnapshot(bool force = false)
        {
            var snapshot = new ServerPrimalMatrixSnapshot();
            var counts = new List<byte>();
            lock (gate)
            {
                foreach ((uint nodeId, byte count) in allocations.OrderBy(a => a.Key))
                {
                    snapshot.NodeIds.Add(nodeId);
                    counts.Add(count);
                }
            }

            // The client clears every node a snapshot does not list, so a character without allocations has nothing to be told.
            if (snapshot.NodeIds.Count == 0 && !force)
                return;

            snapshot.SavedAllocationCounts = counts.ToArray();
            snapshot.MatrixAmpPower        = GetAmpPower();
            player.Session?.EnqueueMessageEncrypted(snapshot);
        }

        public void Save(CharacterContext context)
        {
            (uint NodeId, byte Count, bool Exists)[] pending;
            lock (gate)
            {
                pending = allocations
                    .Where(a => persisted.GetValueOrDefault(a.Key) != a.Value)
                    .Select(a => (a.Key, a.Value, persisted.ContainsKey(a.Key)))
                    .ToArray();
            }

            if (pending.Length == 0)
                return;

            foreach ((uint nodeId, byte count, bool exists) in pending)
            {
                var model = new CharacterPrimalMatrixNodeModel
                {
                    Id          = characterId,
                    NodeId      = (ushort)nodeId,
                    Allocations = count
                };

                if (exists)
                {
                    context.CharacterPrimalMatrixNode.Attach(model);
                    context.Entry(model).Property(m => m.Allocations).IsModified = true;
                }
                else
                    context.CharacterPrimalMatrixNode.Add(model);
            }

            // A row counts as persisted only once its save actually committed; a failed save leaves it pending for the next one.
            context.OnSaveSettled(success =>
            {
                if (!success)
                    return;

                lock (gate)
                    foreach ((uint nodeId, byte count, _) in pending)
                        persisted[nodeId] = count;
            });
        }
    }
}
