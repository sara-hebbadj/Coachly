using System.ComponentModel.DataAnnotations;

namespace Coachly.Api.Entities
{
    public class MembershipPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        // Duration in days (30, 90, etc.)
        public int DurationDays { get; set; }

        // Number of classes included (null = unlimited)
        public int? ClassLimit { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
