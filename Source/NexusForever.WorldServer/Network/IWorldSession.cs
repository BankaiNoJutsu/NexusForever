using System.Collections.Generic;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Session;

namespace NexusForever.WorldServer.Network
{
    public interface IWorldSession : IGameSession, ILootEvidenceCaptureSession, IAccountRuntimeEvidenceCaptureSession, IRewardRotationEvidenceCaptureSession
    {
        IAccount Account { get; }
        IPlayer Player { get; set; }

        bool HasSentCharacterListPackets { get; set; }

        /// <summary>
        /// True after pregame account packets (currency, unlocks, entitlements, tier) are sent during character list setup.
        /// Retail can open the storefront before the async <see cref="ServerCharacterList"/> finishes loading.
        /// </summary>
        bool HasSentPregameAccountPackets { get; set; }

        List<CharacterModel> Characters { get; }

        /// <summary>
        /// Determines if the <see cref="WorldSession"/> is queued to enter the realm.
        /// </summary>
        /// <remarks>
        /// This occurs when the world has reached the maximum number of allowed players.
        /// </remarks>
        bool? IsQueued { get; set; }

        /// <summary>
        /// Initialise <see cref="WorldSession"/> from an existing <see cref="AccountModel"/> database model.
        /// </summary>
        void Initialise(AccountModel account);

        void SetEncryptionKey(byte[] sessionKey);

        void ArmNextClientSpellEvidenceCapture(bool emitDiagnosticSpellBroadcasts = false);
        bool TryConsumeNextClientSpellEvidenceCapture(out bool emitDiagnosticSpellBroadcasts);
    }
}
