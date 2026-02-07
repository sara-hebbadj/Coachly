using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Coachly.Helpers;

namespace Coachly.Models;

public class Booking
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public Guid ClassSessionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = BookingStatus.Pending;

    public Guid? PaymentId { get; set; }

    // Navigation
    public User? User { get; set; }
    public ClassSession? ClassSession { get; set; }
    public Payment? Payment { get; set; }

    // Domain rules
    public bool CanBeCancelled()
        => Status == BookingStatus.Confirmed;
}

