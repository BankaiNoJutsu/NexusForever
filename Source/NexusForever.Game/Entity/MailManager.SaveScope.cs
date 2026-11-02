using NexusForever.Database.Character;

namespace NexusForever.Game.Entity
{
    public partial class MailManager
    {
        private readonly object mailSaveGate = new();
        private MailSaveState mailSave;
        private sealed class MailSaveState(CharacterContext context)
        {
            internal readonly CharacterContext Context = context;
            internal int Calls;
        }
        private sealed class MailSaveCall(MailSaveState state)
        {
            internal readonly MailSaveState State = state;
            internal bool Registered, Ended;
        }
        private bool IsMailSaveInProgress => Volatile.Read(ref mailSave) != null
            || player is Player { IsNormalSaveInProgress: true };

        private MailSaveCall BeginMailSave(CharacterContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            MailSaveCall call;
            lock (mailSaveGate)
            {
                if (mailSave != null && !ReferenceEquals(mailSave.Context, context))
                {
                    var error = new InvalidOperationException("Mailbox already belongs to an unfinished save unit.");
                    context.RejectSave(error); throw error;
                }
                mailSave ??= new(context); ++mailSave.Calls; call = new(mailSave);
            }
            context.OnSaveCompleted(_ => { if (!call.Registered) EndMailSave(call); });
            return call;
        }
        private void EndMailSave(MailSaveCall call)
        {
            lock (mailSaveGate)
            {
                if (call.Ended) return;
                call.Ended = true;
                if (--call.State.Calls == 0 && ReferenceEquals(mailSave, call.State)) mailSave = null;
            }
        }
    }
}
