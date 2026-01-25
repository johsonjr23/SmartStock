namespace SmartStock.Models
{
    public enum InventoryTransactionType
    {
        Purchase = 1,
        Sale = 2,
        Adjustment = 3,

        // Step 5: reversal of a completed sale (kept separate for audit/reporting)
        Void = 4,

            // Step 5.3: customer return (full refund)
        Refund = 5
    }
}
