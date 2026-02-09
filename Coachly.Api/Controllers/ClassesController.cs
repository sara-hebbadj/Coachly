using Coachly.Api.Data;
using Coachly.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coachly.Api.Controllers;

[ApiController]
[Route("api/classes")]
public class ClassesController : ControllerBase
{
    private readonly CoachlyDbContext _db;

    public ClassesController(CoachlyDbContext db)
    {
        _db = db;
    }

    // GET: api/classes
    [HttpGet]
    public async Task<ActionResult<List<ClassSession>>> GetClasses()
    {
        var classes = await _db.ClassSessions
            .OrderBy(c => c.StartTime)
            .ToListAsync();

        return Ok(classes);
    }

    // POST: api/classes
    [HttpPost]
    public async Task<ActionResult> CreateClass(ClassSession session)
    {
        _db.ClassSessions.Add(session);
        await _db.SaveChangesAsync();

        return Ok(session);
    }
}
