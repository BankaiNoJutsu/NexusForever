using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;

namespace NexusForever.Game.Challenges.Reward
{
    public sealed partial class CharacterChallengeRewardRepository
    {
        internal ContractTrackState ChooseContractTrack(uint trackId, string operation, ContractRewardCatalog contracts,
            IReadOnlyList<ContractTrackState> before, ContractRewardSelection selection)
            => Stage(() =>
            {
                var current = ContractRewardSnapshot.Validate(ContractRewardSnapshot.Read(context, characterId, () => contracts), characterId, contracts);
                Require(SameContracts(current, before), "Contract choose baseline changed before execution.");
                var definition = contracts.GetTrack(trackId); var selected = current.SingleOrDefault(row => row.TrackId == trackId);
                Require(definition != null && selected != null && meets(definition.PrerequisiteId), "Contract choose owner/prerequisite is unavailable.");
                Require(selected.SelectionInitialized != (selection != null), "Contract choose cannot resample or omit its selected cycle.");
                if (selection != null)
                {
                    var restored = ContractRewardSelection.Restore(contracts, selection.Json);
                    Require(restored.OperationId == operation && restored.AcceptanceId == null && restored.TrackId == trackId && restored.CycleId == selected.CycleId,
                        "Contract choose selection changed its operation owner.");
                }
                var expected = new List<ContractTrackState>();
                foreach (var state in current)
                {
                    bool active = contracts.GetTrack(state.TrackId).Type == definition.Type ? state.TrackId == trackId : state.Active;
                    bool initialize = state.TrackId == trackId && selection != null;
                    var after = state;
                    if (state.Active != active || initialize)
                    {
                        var row = context.CharacterContractRewardTrack.AsNoTracking().Single(row => row.Id == characterId && row.TrackId == state.TrackId);
                        context.Attach(row); row.Active = active; row.Revision = checked(row.Revision + 1);
                        var slots = state.Slots;
                        if (initialize)
                        {
                            Require(!context.CharacterContractRewardSlot.AsNoTracking().Any(slot => slot.Id == characterId && slot.TrackId == trackId && slot.CycleId == state.CycleId),
                                "Unselected Contract cycle contains slots.");
                            var added = new List<ContractRewardSlotState>();
                            foreach (var slot in definition.Slots)
                            {
                                var model = new CharacterContractRewardSlotModel { Id = characterId, TrackId = trackId, CycleId = state.CycleId, Ordinal = slot.Ordinal,
                                    RewardRowId = selection.Rewards[slot.Ordinal], RequiredPoints = slot.RequiredPoints, SelectionJson = selection.Json };
                                context.CharacterContractRewardSlot.Add(model);
                                Own(model, () => context.CharacterContractRewardSlot.AsNoTracking().SingleOrDefault(stored => stored.Id == characterId && stored.TrackId == trackId
                                    && stored.CycleId == model.CycleId && stored.Ordinal == model.Ordinal));
                                added.Add(new(model.Ordinal, model.RewardRowId, model.RequiredPoints, model.SelectionJson));
                            }
                            row.SelectionInitialized = true; slots = Array.AsReadOnly(added.ToArray());
                        }
                        Own(row, () => context.CharacterContractRewardTrack.AsNoTracking().SingleOrDefault(stored => stored.Id == characterId && stored.TrackId == row.TrackId));
                        after = state with { Active = active, Revision = row.Revision, SelectionInitialized = row.SelectionInitialized, Slots = slots };
                    }
                    expected.Add(after);
                }
                var target = ContractRewardSnapshot.Validate(expected, characterId, contracts);
                context.OnSaveValidation(phase =>
                {
                    if (phase == CharacterSaveValidationPhase.BeforeExplicitCommit)
                        Require(SameContracts(ContractRewardSnapshot.Validate(ContractRewardSnapshot.Read(context, characterId, () => contracts), characterId, contracts), target),
                            "Contract choose SQL differs from its frozen aggregate target.");
                });
                return target.Single(row => row.TrackId == trackId);
            });

        internal static bool SameContracts(IReadOnlyList<ContractTrackState> first, IReadOnlyList<ContractTrackState> second)
            => first != null && second != null && first.Count == second.Count
                && first.Zip(second).All(pair => ContractRewardSnapshot.Same(pair.First, pair.Second));
    }
}
