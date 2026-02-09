namespace Coachly.Api.Entities;

public class Booking
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ClassSessionId { get; set; }

    public DateTime BookedAt { get; set; } = DateTime.UtcNow;
}
