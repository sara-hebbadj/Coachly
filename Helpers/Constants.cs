using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coachly.Helpers;

public static class Roles
{
    public const string Client = "Client";
    public const string Coach = "Coach";
    public const string Admin = "Admin";
}

public static class BookingStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}

public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";
}

public static class MembershipType
{
    public const string Unlimited = "Unlimited";
    public const string CreditPack = "CreditPack";
}

