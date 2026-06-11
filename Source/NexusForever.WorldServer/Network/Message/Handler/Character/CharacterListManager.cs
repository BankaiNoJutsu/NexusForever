using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Storefront;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Shared.Game.Events;

namespace NexusForever.WorldServer.Network.Message.Handler.Character
{
    public class CharacterListManager : ICharacterListManager
    {
        private readonly ILogger<CharacterListHandler> log;

        private readonly IDatabaseManager databaseManager;
        private readonly IRealmContext realmContext;
        private readonly IGlobalStorefrontManager globalStorefrontManager;
        private readonly IItemManager itemManager;

        public CharacterListManager(
            ILogger<CharacterListHandler> log,
            IDatabaseManager databaseManager,
            IRealmContext realmContext,
            IGlobalStorefrontManager globalStorefrontManager,
            IItemManager itemManager = null)
        {
            this.log = log;
            this.databaseManager = databaseManager;
            this.realmContext = realmContext;
            this.globalStorefrontManager = globalStorefrontManager;
            this.itemManager = itemManager;
        }

        public void SendCharacterListPackets(IWorldSession session)
        {
            if (session.IsQueued == true)
                return;

            QueueCharacterListPackets(session,
                databaseManager.GetDatabase<CharacterDatabase>().GetCharacters(session.Account.Id));
        }

        internal void QueueCharacterListPackets(IWorldSession session, Task<List<CharacterModel>> loadCharactersTask)
        {
            // Retail can request the storefront before the character query completes, so
            // send the prerequisite account-state packets before the async character list.
            session.HasSentCharacterListPackets = false;
            session.HasSentPregameAccountPackets = false;
            globalStorefrontManager.ClearCatalogDeliveryState(session.Id, session.Account?.Id ?? 0u);
            SendPregameAccountPackets(session);
            session.HasSentPregameAccountPackets = true;

            session.Events.EnqueueEvent(new TaskGenericEvent<List<CharacterModel>>(
                loadCharactersTask,
                characters =>
                {
                    session.Characters.Clear();
                    session.Characters.AddRange(characters.Where(c => c.DeleteTime == null));

                    foreach (IWritable packet in GetCharacterListPackets(session))
                        session.EnqueueMessageEncrypted(packet);

                    session.HasSentCharacterListPackets = true;
                }));
        }

        internal static void SendPregameAccountPackets(IWorldSession session)
        {
            session.Account.CurrencyManager.SendCharacterListPacket();
            session.Account.GenericUnlockManager.SendUnlockList();

            session.EnqueueMessageEncrypted(new ServerAccountEntitlements
            {
                Entitlements = session.Account.EntitlementManager
                    .Select(e => new ServerAccountEntitlements.AccountEntitlementInfo
                    {
                        Entitlement = e.Type,
                        Count       = e.Amount
                    })
                    .ToList()
            });

            session.EnqueueMessageEncrypted(new ServerAccountTier
            {
                Tier = session.Account.AccountTier
            });
        }

        private IEnumerable<IWritable> GetCharacterListPackets(IWorldSession session)
        {

            ServerCharacterList serverCharacterList = CreateServerCharacterList(
                session.Account.RewardPropertyManager,
                MapServerCharacters(session.Characters)
            );

            uint maxCharacterLevelAchieved = serverCharacterList.Characters
                .Select(i => i.Level)
                .Append((byte)1)
                .Max();

            yield return new ServerMaxCharacterLevelAchieved
            {
                Level = maxCharacterLevelAchieved
            };
            yield return serverCharacterList;
        }

        private ServerCharacterList CreateServerCharacterList(IRewardPropertyManager rewardPropertyManager,
            IEnumerable<ServerCharacterList.Character> characters)
        {
            var characterList = (characters as IList<ServerCharacterList.Character> ?? characters.ToList());
            // 2 is just a fail safe for the minimum amount of character slots
            // this is set in the tbl files so a value should always exist
            uint characterSlots =
                (uint)(rewardPropertyManager.GetRewardProperty(RewardPropertyType.CharacterSlots).GetValue(0u) ?? 2u);

            var serverCharacterList = new ServerCharacterList
            {
                ServerTime                     = realmContext.GetServerTime(),
                RealmId                        = realmContext.RealmId,
                // no longer used as replaced by entitlements but retail server still used to send this
                AdditionalCount                = (uint)characterList.Count,
                MaxNumberCharacters = (uint)Math.Max(0, (int)characterSlots - characterList.Count),
                // Free Level 50 needs(?) support. It appears to have just been a custom flag on the account that was consume when used up.
                // FreeLevel50 = true
            };
            serverCharacterList.Characters.AddRange(characterList);

            return serverCharacterList;
        }

        private IEnumerable<ServerCharacterList.Character> MapServerCharacters(IEnumerable<CharacterModel> characters)
        {
            foreach (CharacterModel character in characters)
            {
                var listCharacter = new ServerCharacterList.Character
                {
                    Id                = character.Id,
                    Name              = character.Name,
                    Sex               = (Sex)character.Sex,
                    Race              = (Race)character.Race,
                    Class             = (Class)character.Class,
                    Faction           = character.FactionId,
                    Level             = character.Level,
                    WorldId           = character.WorldId,
                    WorldZoneId       = character.WorldZoneId,
                    RealmId           = realmContext.RealmId,
                    Path              = (byte)character.ActivePath,
                    LastLoggedOutDays =
                        (float)DateTime.UtcNow.Subtract(character.LastOnline ?? DateTime.UtcNow).TotalDays * -1f
                };

                try
                {
                    // create a temporary Inventory and CostumeManager to show equipped gear
                    var inventory      = new Inventory(null, character, itemManager: itemManager);
                    var costumeManager = new CostumeManager(null, character, itemManager);

                    ICostume costume = null;
                    if (costumeManager.CostumeIndex.HasValue)
                        costume = costumeManager.GetCostume((byte)character.ActiveCostumeIndex);

                    listCharacter.GearMask = costume?.VisibilityMask ?? 0xFFFFFFFF;

                    Dictionary<ItemSlot, IItemVisual> costumeVisuals =
                        costume?.GetItemVisuals().ToDictionary(c => c.Slot);
                    foreach (IItemVisual itemVisual in inventory.GetItemVisuals())
                    {
                        if (costumeVisuals != null
                            && costumeVisuals.TryGetValue(itemVisual.Slot, out IItemVisual costumeVisual)
                            && costumeVisual.DisplayId.HasValue)
                            listCharacter.Gear.Add(costumeVisual.Build());
                        else
                            listCharacter.Gear.Add(itemVisual.Build());
                    }

                    foreach (CharacterAppearanceModel appearance in character.Appearance)
                    {
                        listCharacter.Appearance.Add(new NexusForever.Network.World.Message.Model.Shared.ItemVisual
                        {
                            Slot      = (ItemSlot)appearance.Slot,
                            DisplayId = appearance.DisplayId
                        });
                    }

                    foreach (CharacterBoneModel bone in character.Bone.OrderBy(bone => bone.BoneIndex))
                    {
                        listCharacter.Bones.Add(bone.Bone);
                    }

                    foreach (CharacterStatModel stat in character.Stat)
                    {
                        if ((Stat)stat.Stat == Stat.Level)
                        {
                            listCharacter.Level = (uint)stat.Value;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogCritical(ex, $"An error has occured while loading character '{character.Name}'");
                    continue;
                }

                yield return listCharacter;
            }
        }
    }
}
