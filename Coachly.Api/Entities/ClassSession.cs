namespace Coachly.Api.Entities;

public class ClassSession
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public int DurationMinutes { get; set; }

    public decimal Price { get; set; }

    public int CoachId { get; set; }
}
