using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coachly.Helpers;

namespace Coachly.Models;

public class MembershipPlan
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = MembershipType.Unlimited;

    public decimal Price { get; set; }

    public int DurationInDays { get; set; }

    // Only for credit packs
    public int? TotalCredits { get; set; }
}

