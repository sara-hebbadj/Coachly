using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coachly.Shared.DTOs;

public class PaymentDto
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AED";
    public string Description { get; set; } = string.Empty;
}
