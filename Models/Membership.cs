using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coachly.Helpers;

namespace Coachly.Models;

public class Membership
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public Guid MembershipPlanId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int? RemainingCredits { get; set; }

    // Navigation
    public User? User { get; set; }
    public MembershipPlan? MembershipPlan { get; set; }

    public bool IsActive()
        => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;

    public bool CanBookClass()
    {
        if (!IsActive()) return false;

        if (MembershipPlan?.Type == MembershipType.Unlimited)
            return true;

        return RemainingCredits.HasValue && RemainingCredits > 0;
    }
}

