using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GestãoIdeas.Data;
using GestãoIdeas.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestaoIdeas.Tests;

public sealed class TestDatabase : IDisposable
{
    public IdeaContext Context { get; }
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, $"testDatabase_{Guid.NewGuid():N}.db");

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

        var options = new DbContextOptionsBuilder<IdeaContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new IdeaContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

public class IdeaCrudTests
{

    [Fact]
    public async Task CreateIdea_SavesToDatabase()
    {
        // Arrange
        using var db = new TestDatabase();
        var context = db.Context;
        var idea = new Idea
        {
            Name = "Test idea",
            Description = "My first idea",
            State = IdeaState.IN_DEVELOPMENT,
            Priority = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await context.Ideas.AddAsync(idea);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Ideas.FindAsync(idea.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test idea", saved!.Name);
    }

    [Fact]
    public async Task ReadIdea_ReturnsExistingIdea()
    {
        // Arrange
        using var db = new TestDatabase();
        var context = db.Context;
        var idea = new Idea
        {
            Name = "Read idea",
            Description = "To be read",
            State = IdeaState.IN_DEVELOPMENT,
            Priority = 2,
            CreatedAt = DateTime.UtcNow
        };
        await context.Ideas.AddAsync(idea);
        await context.SaveChangesAsync();

        // Act
        var result = await context.Ideas
            .Where(i => i.Priority == 2)
            .FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Read idea", result!.Name);
    }

    [Fact]
    public async Task UpdateIdea_ChangesArePersisted()
    {
        // Arrange
        using var db = new TestDatabase();
        var context = db.Context;
        var idea = new Idea
        {
            Name = "Old name",
            Description = "Old description",
            State = IdeaState.IN_DEVELOPMENT,
            Priority = 1,
            CreatedAt = DateTime.UtcNow
        };
        await context.Ideas.AddAsync(idea);
        await context.SaveChangesAsync();

        // Act
        idea.Name = "New name";
        idea.Description = "New description";
        idea.State = IdeaState.COMPLETED;
        idea.Priority = 3;
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.Ideas.FindAsync(idea.Id);
        Assert.NotNull(updated);
        Assert.Equal("New name", updated!.Name);
        Assert.Equal(IdeaState.COMPLETED, updated.State);
        Assert.Equal(3, updated.Priority);
    }

    [Fact]
    public async Task DeleteIdea_RemovesFromDatabase()
    {
        // Arrange
        using var db = new TestDatabase();
        var context = db.Context;
        var idea = new Idea
        {
            Name = "To delete",
            Description = "Will be deleted",
            State = IdeaState.IN_DEVELOPMENT,
            Priority = 1,
            CreatedAt = DateTime.UtcNow
        };
        await context.Ideas.AddAsync(idea);
        await context.SaveChangesAsync();

        // Act
        context.Ideas.Remove(idea);
        await context.SaveChangesAsync();

        // Assert
        var deleted = await context.Ideas.FindAsync(idea.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task UpdateOutdatedIdeas_SetsOldNonCompletedToAbandoned()
    {
        using var db = new TestDatabase();
        var context = db.Context;

        var fixedNow = new DateTime(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc);
        var olderThanMonth = fixedNow.AddMonths(-2);
        var newerThanMonth = fixedNow.AddDays(-10);

        var oldInDev = new Idea
        {
            Name = "Old in dev",
            CreatedAt = olderThanMonth,
            State = IdeaState.IN_DEVELOPMENT,
            Priority = 1,
            Description = "Should be abandoned"
        };

        var recentInDev = new Idea
        {
            Name = "Recent in dev",
            CreatedAt = newerThanMonth,
            State = IdeaState.IN_DEVELOPMENT,
            Priority = 1,
            Description = "Should stay in dev"
        };

        var oldCompleted = new Idea
        {
            Name = "Old completed",
            CreatedAt = olderThanMonth,
            State = IdeaState.COMPLETED,
            Priority = 1,
            Description = "Should remain completed"
        };

        context.Ideas.AddRange(oldInDev, recentInDev, oldCompleted);
        await context.SaveChangesAsync();

        IdeaMaintenanceService.UpdateOutdatedIdeas(context, fixedNow);

        var reloadedOldInDev = await context.Ideas.FindAsync(oldInDev.Id);
        var reloadedRecentInDev = await context.Ideas.FindAsync(recentInDev.Id);
        var reloadedOldCompleted = await context.Ideas.FindAsync(oldCompleted.Id);

        Assert.NotNull(reloadedOldInDev);
        Assert.Equal(IdeaState.ABANDONED, reloadedOldInDev!.State);

        Assert.NotNull(reloadedRecentInDev);
        Assert.Equal(IdeaState.IN_DEVELOPMENT, reloadedRecentInDev!.State);

        Assert.NotNull(reloadedOldCompleted);
        Assert.Equal(IdeaState.COMPLETED, reloadedOldCompleted!.State);
    }
}
