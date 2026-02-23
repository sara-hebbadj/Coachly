using Coachly.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

Microsoft.AspNetCore.Builder.WebApplicationBuilder builder =
    Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);


// =======================
// SERVICES
// =======================

// Controllers + JSON (prevents EF loop crashes in Swagger)
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<CoachlyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// =======================
// APP
// =======================

var app = builder.Build();

// Developer exception page (important while building)
app.UseDeveloperExceptionPage();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Coachly API v1");
    c.RoutePrefix = "swagger"; // https://localhost:7131/swagger
});

app.UseHttpsRedirection();
app.UseAuthorization();

// IMPORTANT: this must be ON once you have controllers
 app.MapControllers();

app.Run();
