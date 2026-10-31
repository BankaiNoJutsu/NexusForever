using Microsoft.Extensions.DependencyInjection;
using NexusForever.API.Character.Client;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using NexusForever.Database.Query;
using NexusForever.Database.Query.Model;
using NexusForever.Database.Query.Repository;

namespace NexusForever.Server.Character.Game.Character
{
    public class CharacterManager
    {
        #region Dependency Injection

        private readonly IServiceProvider _serviceProvider;
        private readonly CharacterRepository _repository;
        private readonly CharacterAPIClient _apiClient;
        private readonly QueryContext _context;

        public CharacterManager(
            IServiceProvider serviceProvider,
            CharacterRepository repository,
            CharacterAPIClient apiClient,
            QueryContext context)
        {
            _serviceProvider = serviceProvider;
            _repository      = repository;
            _apiClient       = apiClient;
            _context         = context;
        }

        #endregion

        /// <summary>
        /// Get an <see cref="Character"/> with the specified character identity.
        /// </summary>
        /// <remarks>
        /// If the character does not exist in the database it will be fetched from the API and saved to the local database.
        /// </remarks>
        /// <param name="identity">Identity of the <see cref="Character"/> to return.</param>
        public async Task<Character> GetCharacterRemoteAsync(Identity identity)
        {
            CharacterModel databaseModel = await _repository.GetCharacterAsync(identity.Id, identity.RealmId);
            if (databaseModel == null)
            {
                API.Model.Character.Character apiModel = await _apiClient.GetCharacterAsync(identity.ToAPIIdentity());
                if (apiModel == null)
                    return null;

                databaseModel = await _repository.EnsureCharacterAsync(apiModel.ToDatabaseCharacter());
            }

            var character = _serviceProvider.GetRequiredService<Character>();
            character.Initialise(databaseModel);
            return character;
        }

        /// <summary>
        /// Commit an atomic bootstrap and one message-owned field together.
        /// An API snapshot never replaces another message's existing fields.
        /// </summary>
        public Task UpdateCharacterRemoteAsync<TProperty>(Identity identity,
            Expression<Func<CharacterModel, TProperty>> property, TProperty value)
        {
            if (_context.Database.CurrentTransaction != null)
                return UpdateCharacterCoreAsync(identity, property, value);

            // The provider's retry strategy must own the complete transaction.
            // A caller that already owns a transaction also owns its retry unit.
            return _context.Database.CreateExecutionStrategy().ExecuteAsync(
                () => UpdateCharacterCoreAsync(identity, property, value));
        }

        private async Task UpdateCharacterCoreAsync<TProperty>(Identity identity,
            Expression<Func<CharacterModel, TProperty>> property, TProperty value)
        {
            CharacterModel model = await _repository.GetCharacterAsync(identity.Id, identity.RealmId);
            CharacterModel seed = null;
            if (model == null)
            {
                API.Model.Character.Character apiModel = await _apiClient.GetCharacterAsync(identity.ToAPIIdentity());
                if (apiModel == null)
                    return;
                seed = apiModel.ToDatabaseCharacter();
            }

            // Fetch remotely before starting our transaction so independent bootstrap
            // requests contend on the atomic INSERT rather than on earlier read locks.
            await using var transaction = _context.Database.CurrentTransaction == null
                ? await _context.Database.BeginTransactionAsync() : null;
            model ??= await _repository.EnsureCharacterAsync(seed);
            var field = _context.Entry(model).Property(property);
            field.CurrentValue = value;
            // A repeated value is authoritative too: another context may have
            // changed this column after our original tracked read.
            field.IsModified = true;
            await _context.SaveChangesAsync();
            if (transaction != null)
                await transaction.CommitAsync();
        }
    }
}
