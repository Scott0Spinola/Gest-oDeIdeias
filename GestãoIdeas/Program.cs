using GestãoIdeas.Controllers;
using GestãoIdeas.Data;
using GestãoIdeas.Models;
using GestãoIdeas.DTOs;
using GestãoIdeas.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IAdviceService, AdviceService>();


builder.Services.AddDbContext<IdeaContext>(options =>
{
	var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
	options.UseSqlite(connectionString);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
// Ensure database is created/migrated and seed initial data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdeaContext>();
    db.Database.Migrate();

	IdeaMaintenanceService.UpdateOutdatedIdeas(db, DateTime.UtcNow);

	if (!db.Ideas.Any())
	{
		var now = DateTime.UtcNow;
		db.Ideas.AddRange(
			new Idea
			{
				Name = "First idea",
				Description = "Example seeded idea",
				CreatedAt = now,
				State = IdeaState.IN_DEVELOPMENT,
				Priority = 1
			},
			new Idea
			{
				Name = "Second idea",
				Description = "Another example idea",
				CreatedAt = now,
				State = IdeaState.COMPLETED,
				Priority = 2
			}
		);
		db.SaveChanges();
	}
}

// Simple request logging to a local file (log.txt in app root)
app.Use(async (context, next) =>
{
	var logLine = $"{DateTime.UtcNow:o} {context.Request.Method} {context.Request.Path}{context.Request.QueryString}{Environment.NewLine}";
	await File.AppendAllTextAsync("log.txt", logLine);
	await next();
});

// Map idea endpoints that operate on the data
app.MapIdeasEndPoints();

app.Run();


