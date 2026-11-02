using NexusForever.Database.Character;
using NLog;

namespace NexusForever.Game.Marketplace
{
    // Preserve confirmed SQL success without replaying the enrollment callback.
    internal static class MarketplaceCommittedSave
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        internal static void Save(CharacterDatabase database, Action<CharacterContext> enroll)
            => Save(database.SaveBlocking, enroll);

        internal static void Save(Action<Action<CharacterContext>> persist, Action<CharacterContext> enroll)
        {
            bool committed = false;
            try
            {
                persist(context =>
                {
                    enroll(context);
                    // Do not acknowledge an early flush from incomplete enrollment.
                    context.OnSaveSettled(success => committed |= success);
                });
                if (!committed)
                    throw new InvalidOperationException("Marketplace save returned without a confirmed terminal commit.");
            }
            catch (Exception error) when (committed)
            {
                try { log.Warn(error, "Marketplace save reported an error after confirmed commit; retaining SQL success."); }
                catch { /* Diagnostics cannot reclassify an already committed outcome. */ }
            }
        }
    }
}
