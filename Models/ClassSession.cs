using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coachly.Models;

public class ClassSession
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public TimeSpan Duration { get; set; }

    public int Capacity { get; set; }

    public decimal Price { get; set; }

    public Guid CoachId { get; set; }

    // Navigation
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    // Derived (NOT stored)
    public int RemainingSpots => Capacity - Bookings.Count(b => b.Status == Helpers.BookingStatus.Confirmed);
}
