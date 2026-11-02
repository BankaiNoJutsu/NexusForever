using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Mail;

internal sealed record MailHeaderState(ulong Id, ulong RecipientId, byte SenderType, byte ContentType, ulong SenderId,
        string Subject, string Message, uint TextEntrySubject, uint TextEntryMessage, uint CreatureId,
        byte CurrencyType, ulong CurrencyAmount, byte IsCashOnDelivery, byte HasPaidOrCollectedCurrency,
        byte Flags, byte DeliveryTime, DateTime CreateTime)
{
    internal static MailHeaderState From(CharacterMailModel m) => m == null ? null : new(m.Id, m.RecipientId, m.SenderType,
        m.ContentType, m.SenderId, m.Subject, m.Message, m.TextEntrySubject, m.TextEntryMessage, m.CreatureId,
        m.CurrencyType, m.CurrencyAmount, m.IsCashOnDelivery, m.HasPaidOrCollectedCurrency, m.Flags, m.DeliveryTime, m.CreateTime);
    internal CharacterMailModel Model() => new() { Id = Id, RecipientId = RecipientId, SenderType = SenderType,
        ContentType = ContentType, SenderId = SenderId, Subject = Subject, Message = Message,
        TextEntrySubject = TextEntrySubject, TextEntryMessage = TextEntryMessage, CreatureId = CreatureId,
        CurrencyType = CurrencyType, CurrencyAmount = CurrencyAmount, IsCashOnDelivery = IsCashOnDelivery,
        HasPaidOrCollectedCurrency = HasPaidOrCollectedCurrency, Flags = Flags, DeliveryTime = DeliveryTime, CreateTime = CreateTime };
}

internal sealed record MailAttachmentState(ulong Id, uint Index, ulong ItemGuid)
{
    internal static MailAttachmentState From(CharacterMailAttachmentModel m) => m == null ? null : new(m.Id, m.Index, m.ItemGuid);
    internal CharacterMailAttachmentModel Model() => new() { Id = Id, Index = Index, ItemGuid = ItemGuid };
}
