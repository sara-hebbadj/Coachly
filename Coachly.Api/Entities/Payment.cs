namespace Coachly.Api.Entities;

public class Payment
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int? BookingId { get; set; }
    public int? MembershipId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AED";

    public string Status { get; set; } = "Pending";
    // Pending | Paid | Failed | Refunded

    public string Provider { get; set; } = "Manual";
    // Manual | Stripe | ApplePay | GooglePay

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation (optional for now)
    public User User { get; set; } = null!;
}
