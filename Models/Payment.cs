using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coachly.Helpers;

namespace Coachly.Models;

public class Payment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "AED";

    public string Status { get; set; } = PaymentStatus.Pending;

    public string Provider { get; set; } = string.Empty; // Stripe, Telr, etc.

    public string ProviderReference { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}

