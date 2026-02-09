using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coachly.Api.Entities
{
    public class Membership
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int MembershipPlanId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        public bool IsActive { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public MembershipPlan MembershipPlan { get; set; } = null!;
    }
}
