using System.Security.Cryptography;
using System.Text;
using Coachly.Api.Data;
using Coachly.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

Microsoft.AspNetCore.Builder.WebApplicationBuilder builder =
    Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddDataProtection();

builder.Services.AddDbContext<CoachlyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Coachly API v1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

await SeedDemoUsersAsync(app.Services);

app.Run();

static async Task SeedDemoUsersAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CoachlyDbContext>();

    var demoUsers = new[]
    {
        new { FullName = "Demo Client", Email = "client@coachly.demo", Password = "password123", Role = "Client" },
        new { FullName = "Demo Coach", Email = "coach@coachly.demo", Password = "password123", Role = "Coach" }
    };

    foreach (var demo in demoUsers)
    {
        var normalizedEmail = demo.Email.ToLowerInvariant();
        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (exists)
        {
            continue;
        }

        db.Users.Add(new User
        {
            FullName = demo.FullName,
            Email = normalizedEmail,
            PasswordHash = Hash(demo.Password),
            Role = demo.Role,
            CreatedAt = DateTime.UtcNow
        });
    }

    await db.SaveChangesAsync();
}

static string Hash(string value)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
    return Convert.ToHexString(bytes);
}
