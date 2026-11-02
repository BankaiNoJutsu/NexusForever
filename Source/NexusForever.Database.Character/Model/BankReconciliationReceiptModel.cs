using System;

namespace NexusForever.Database.Character.Model
{
    // Retain outcome evidence after later custody changes or Character deletion.
    public sealed class BankReconciliationReceiptModel
    {
        public string Id { get; set; }
        public string Payload { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
