using Coachly.Api.Data;
using Coachly.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coachly.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembershipsController : ControllerBase
    {
        private readonly CoachlyDbContext _context;

        public MembershipsController(CoachlyDbContext context)
        {
            _context = context;
        }

        // GET: api/memberships/plans
        [HttpGet("plans")]
        public async Task<IActionResult> GetMembershipPlans()
        {
            var plans = await _context.MembershipPlans
                .Where(p => p.IsActive)
                .ToListAsync();

            return Ok(plans);
        }

        // POST: api/memberships
        [HttpPost]
        public async Task<IActionResult> CreateMembership(Membership membership)
        {
            membership.StartDate = DateTime.UtcNow;
            membership.IsActive = true;

            _context.Memberships.Add(membership);
            await _context.SaveChangesAsync();

            return Ok(membership);
        }

        // GET: api/memberships/user/1
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserMemberships(int userId)
        {
            var memberships = await _context.Memberships
                .Include(m => m.MembershipPlan)
                .Where(m => m.UserId == userId)
                .ToListAsync();

            return Ok(memberships);
        }
    }
}
