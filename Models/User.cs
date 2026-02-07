using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coachly.Models;

public class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    // Navigation (later EF)
   
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
   public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}


