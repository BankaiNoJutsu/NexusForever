using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusForever.Aspire.Database.Migrations.Configuration.Model;
using NexusForever.Cryptography;
using NexusForever.Database.Auth;
using NexusForever.Database.Auth.Model;

namespace NexusForever.Aspire.Database.Migrations.Service
{
    public class AccountCreationHostedService : IHostedService
    {
        #region Dependency Injection

        private readonly ILogger<AccountCreationHostedService> _log;
        private readonly AccountCreationOptions _options;
        private readonly AuthContext _context;

        public AccountCreationHostedService(
            ILogger<AccountCreationHostedService> log,
            IOptions<AccountCreationOptions> options,
            AuthContext context)
        {
            _log     = log;
            _options = options.Value;
            _context = context;
        }

        #endregion

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            List<AccountCreationAccountOptions> configuredAccounts = GetConfiguredAccounts();
            if (configuredAccounts.Count == 0)
            {
                _log.LogWarning("Account creation options are not configured, skipping account creation.");
                return;
            }

            foreach (AccountCreationAccountOptions configuredAccount in configuredAccounts)
            {
                if (string.IsNullOrWhiteSpace(configuredAccount.UserName) || string.IsNullOrWhiteSpace(configuredAccount.Password))
                {
                    _log.LogWarning("Account creation entry is missing a username or password, skipping it.");
                    continue;
                }

                uint roleId = configuredAccount.RoleId ?? 1u;

                AccountModel accountModel = await _context.Account
                    .Include(a => a.AccountRole)
                    .Include(a => a.AccountEntitlement)
                    .SingleOrDefaultAsync(a => a.Email == configuredAccount.UserName, cancellationToken);

                if (accountModel != null)
                {
                    bool changed = AccountDefaultEntitlements.EnsureBaseline(accountModel);

                    if (!accountModel.AccountRole.Any(r => r.RoleId == roleId))
                    {
                        accountModel.AccountRole.Add(new AccountRoleModel
                        {
                            RoleId = roleId
                        });
                        changed = true;
                    }

                    if (changed)
                    {
                        try
                        {
                            await _context.SaveChangesAsync(cancellationToken);
                            _log.LogInformation("Updated existing account '{UserName}' with role {RoleId} and baseline entitlements.", configuredAccount.UserName, roleId);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "Failed to update existing account '{UserName}'.", configuredAccount.UserName);
                        }
                    }
                    else
                    {
                        _log.LogInformation("Account with username '{UserName}' already exists with role {RoleId} and baseline entitlements, skipping account creation.", configuredAccount.UserName, roleId);
                    }

                    continue;
                }

                (string salt, string vertifier) = PasswordProvider.GenerateSaltAndVerifier(configuredAccount.UserName, configuredAccount.Password);
                var newAccount = new AccountModel
                {
                    Email = configuredAccount.UserName,
                    S     = salt,
                    V     = vertifier
                };
                newAccount.AccountRole.Add(new AccountRoleModel
                {
                    RoleId = roleId
                });
                AccountDefaultEntitlements.EnsureBaseline(newAccount);

                _context.Account.Add(newAccount);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    _log.LogInformation("Account with username '{UserName}' created successfully with role {RoleId}.", configuredAccount.UserName, roleId);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to create account with username '{UserName}'.", configuredAccount.UserName);
                }
            }
        }

        private List<AccountCreationAccountOptions> GetConfiguredAccounts()
        {
            if (_options.Accounts != null && _options.Accounts.Count > 0)
            {
                return _options.Accounts
                    .Where(a => a != null)
                    .ToList();
            }

            if (string.IsNullOrWhiteSpace(_options.UserName) && string.IsNullOrWhiteSpace(_options.Password))
            {
                return [];
            }

            return
            [
                new AccountCreationAccountOptions
                {
                    UserName = _options.UserName,
                    Password = _options.Password,
                    RoleId   = _options.RoleId
                }
            ];
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
