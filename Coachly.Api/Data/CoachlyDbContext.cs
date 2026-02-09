using Microsoft.EntityFrameworkCore;
using Coachly.Api.Entities;

namespace Coachly.Api.Data
{
    public class CoachlyDbContext : DbContext
    {
        public CoachlyDbContext(DbContextOptions<CoachlyDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ClassSession> ClassSessions { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
        public DbSet<Payment> Payments => Set<Payment>();


    }
}