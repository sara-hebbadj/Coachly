using Coachly.Api.Entities;

namespace Coachly.Api.Data;

public static class DataSeeder
{
    public static void Seed(CoachlyDbContext db)
    {
        if (!db.ClassSessions.Any())
        {
            db.ClassSessions.AddRange(
                new ClassSession
                {
                    Title = "High Heels Beginner",
                    StartTime = DateTime.UtcNow.AddDays(1).AddHours(18),
                    DurationMinutes = 60,
                    Price = 120,
                    CoachId = 1
                },
                new ClassSession
                {
                    Title = "High Heels Intermediate",
                    StartTime = DateTime.UtcNow.AddDays(2).AddHours(19),
                    DurationMinutes = 75,
                    Price = 150,
                    CoachId = 1
                }
            );

            db.SaveChanges();
        }
    }
}
