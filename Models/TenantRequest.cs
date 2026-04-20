namespace SmartStock.Models
{
    public class TenantRequest
    {
        public int Id { get; set; }
        public string ShopName { get; set; } = null!;
        public string OwnerName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? BusinessType { get; set; }
        public string? Message { get; set; }

        // "Pending" | "Approved" | "Rejected"
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? AdminNotes { get; set; }
    }
}
