using Coachly.Api.Data;
using Coachly.Api.Entities;
using Coachly.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coachly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly CoachlyDbContext _context;

    public BookingsController(CoachlyDbContext context)
    {
        _context = context;
    }

    // GET: api/bookings/user/1
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserBookings(int userId)
    {
        var bookings = await _context.Bookings
            .Where(b => b.UserId == userId)
            .Select(b => new BookingResponseDto
            {
                Id = b.Id,
                UserId = b.UserId,
                ClassSessionId = b.ClassSessionId,
                BookedAt = b.BookedAt
            })
            .ToListAsync();

        return Ok(bookings);
    }

    // POST: api/bookings
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] BookingDto dto)
    {
        var alreadyBooked = await _context.Bookings.AnyAsync(b =>
            b.UserId == dto.UserId &&
            b.ClassSessionId == dto.ClassSessionId
        );

        if (alreadyBooked)
            return BadRequest("User already booked this class");

        var booking = new Booking
        {
            UserId = dto.UserId,
            ClassSessionId = dto.ClassSessionId,
            BookedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var response = new BookingResponseDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            ClassSessionId = booking.ClassSessionId,
            BookedAt = booking.BookedAt
        };

        return Ok(response);
    }
}
