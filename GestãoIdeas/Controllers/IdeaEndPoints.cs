using GestãoIdeas.Data;
using GestãoIdeas.DTOs;
using GestãoIdeas.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using GestãoIdeas.Services;

namespace GestãoIdeas.Controllers;

public static class IdeaEndPoints
{

    public static void MapIdeasEndPoints(this WebApplication app)
    {

        var ideaURL = app.MapGroup("/ideas");
        ideaURL.MapGet("/", GetAllIdeas)
            .WithName("GetAllIdeas")
            .WithSummary("Get all ideas")
            .WithDescription("Returns all ideas currently stored in the database.")
            .Produces<List<Idea>>(StatusCodes.Status200OK);

        ideaURL.MapGet("/{id}", GetIdIdeas)
            .WithName("GetIdeaById")
            .WithSummary("Get a single idea")
            .WithDescription("Looks up an idea by its numeric identifier.")
            .Produces<IdeaDTO>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        ideaURL.MapGet("/priority", GetPriority)
            .WithName("GetIdeaByPriority")
            .WithSummary("Get ideas by priority")
            .WithDescription("Returns all ideas that match the given priority value.")
            .Produces<List<IdeaDTO>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        ideaURL.MapPost("/", CreateIdea)
            .WithName("CreateIdea")
            .WithSummary("Create a new idea")
            .WithDescription("Creates a new idea using the data provided in the request body.")
            .Accepts<CreateIdea>("application/json")
            .Produces<IdeaDTO>(StatusCodes.Status201Created);

        ideaURL.MapPut("/{id}", UpdateIdea)
            .WithName("UpdateIdea")
            .WithSummary("Update an existing idea")
            .WithDescription("Updates the title, description, state, and priority of an existing idea.")
            .Accepts<UpdateIdea>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        ideaURL.MapDelete("/{id}", DeleteIdea)
            .WithName("DeleteIdea")
            .WithSummary("Delete an idea")
            .WithDescription("Deletes the idea with the specified id.")
            .Produces(StatusCodes.Status204NoContent);

    }


    /// <summary>
    /// Gets all ideas from the database.
    /// </summary>
    private static async Task<IResult> GetAllIdeas(IdeaContext context, ILogger<IdeaContext> logger, IAdviceService adviceService)
    {
        try
        {
           
            var ideas = await context.Ideas.ToListAsync();
            var ideaDtos = ideas.Select(i => new IdeaDTO(i)).ToList();
            var advice = await adviceService.GetRandomAdviceAsync();
            var response = new IdeasWithAdviceResponse(ideaDtos, advice);
           
            LogToFile("INFO", $"Returned {ideas.Count} ideas");
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting ideas");
            LogToFile("ERROR", "Error getting ideas", ex);
            return TypedResults.Problem(
                detail: "An error occurred while retrieving ideas.",
                statusCode: StatusCodes.Status500InternalServerError
            );
            throw;
        }
    }



    /// <summary>
    /// Gets a single idea by its identifier.
    /// </summary>
    private static async Task<IResult> GetIdIdeas(int id, IdeaContext context, ILogger<IdeaContext> logger)
    {
        try
        {
            var idea = await context.Ideas.FirstOrDefaultAsync(x => x.Id == id);
            if (idea is null)
            {
                logger.LogWarning("Idea with id {Id} not found", id);
                LogToFile("WARN", $"Idea with id {id} not found");
                return TypedResults.NotFound();
            }
            logger.LogInformation("Returning idea with id {Id}", id);
            LogToFile("INFO", $"Returned idea with id {id}");
            return TypedResults.Ok(new IdeaDTO(idea));

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting idea with id {Id}", id);
            LogToFile("ERROR", $"Error getting idea with id {id}", ex);
            return TypedResults.Problem(
                detail: "An error occurred while retrieving the idea.",
                statusCode: StatusCodes.Status500InternalServerError
            );
            throw;
        }
    }
    /// <summary>
    /// Gets all ideas that match the specified priority.
    /// </summary>
    private static async Task<IResult> GetPriority(int priority, IdeaContext context, ILogger<IdeaContext> logger)
    {
        try
        {
            var ideas = await context.Ideas
                .Where(x => x.Priority == priority)
                .ToListAsync();

            if (ideas.Count == 0)
            {
                logger.LogWarning("No ideas found with priority {Priority}", priority);
                LogToFile("WARN", $"No ideas found with priority {priority}");
                return TypedResults.NotFound();
            }

            var ideaDtos = ideas.Select(i => new IdeaDTO(i)).ToList();

            logger.LogInformation("Returning {Count} ideas with priority {Priority}", ideas.Count, priority);
            LogToFile("INFO", $"Returned {ideas.Count} ideas with priority {priority}");
            return TypedResults.Ok(ideaDtos);
        }
        catch (Exception ex)
        {

            logger.LogError(ex, "Error getting idea with priority {Priority}", priority);
            LogToFile("ERROR", $"Error getting idea with priority {priority}", ex);

            return TypedResults.Problem(
                detail: "An error occurred while retrieving the idea.",
                statusCode: StatusCodes.Status500InternalServerError
            );
            throw;
        }
    }

    /// <summary>
    /// Creates a new idea.
    /// </summary>
    private static async Task<IResult> CreateIdea(CreateIdea createIdea, IdeaContext context, ILogger<IdeaContext> logger)
    {
        try
        {
            var idea = new Idea
            {
                Name = createIdea.Name,
                Description = createIdea.Description,
                State = createIdea.State,
                CreatedAt = DateTime.UtcNow,
                Priority = createIdea.Priority
            };

            context.Ideas.Add(idea);
            await context.SaveChangesAsync();
            var ideaDto = new IdeaDTO(idea);
            logger.LogInformation("Created idea with id {Id}", idea.Id);
            LogToFile("INFO", $"Created idea with id {idea.Id}");
            return TypedResults.Created($"/ideas/{idea.Id}", ideaDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating idea with name {Name}", createIdea.Name);
            LogToFile("ERROR", $"Error creating idea with name {createIdea.Name}", ex);
            return TypedResults.Problem(
                detail: "An error occurred while retrieving the idea.",
                statusCode: StatusCodes.Status500InternalServerError
            );
            throw;
        }


    }

    /// <summary>
    /// Updates an existing idea.
    /// </summary>
    private static async Task<IResult> UpdateIdea(int id, UpdateIdea updateIdea, IdeaContext context, ILogger<IdeaContext> logger)
    {
        try
        {
            var thought = await context.Ideas.FindAsync(id);

            if (thought is null)
            {
                logger.LogWarning("Idea with id {Id} not found for update", id);
                LogToFile("WARN", $"Idea with id {id} not found for update");
                return TypedResults.NotFound();
            }

            thought.Name = updateIdea.Name;
            thought.Description = updateIdea.Description;
            thought.State = updateIdea.State;
            thought.Priority = updateIdea.Priority;

            await context.SaveChangesAsync();
            logger.LogInformation("Updated idea with id {Id}", id);
            LogToFile("INFO", $"Updated idea with id {id}");
            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating idea with id {Id}", id);
            LogToFile("ERROR", $"Error updating idea with id {id}", ex);
            return TypedResults.Problem(
                detail: "An error occurred while updating the idea.",
                statusCode: StatusCodes.Status500InternalServerError
            );
            throw;
        }

    }

    /// <summary>
    /// Deletes an idea by its identifier.
    /// </summary>
    private static async Task<IResult> DeleteIdea(int id, IdeaContext context, ILogger<IdeaContext> logger)
    {
        try
        {
            if (await context.Ideas.FindAsync(id) is not Idea idea)
            {
                logger.LogWarning("Idea with id {Id} not found for delete", id);
                LogToFile("WARN", $"Idea with id {id} not found for delete");
                return TypedResults.NotFound();
            }

            context.Ideas.Remove(idea);
            await context.SaveChangesAsync();
            logger.LogInformation("Deleted idea with id {Id}", id);
            LogToFile("INFO", $"Deleted idea with id {id}");
            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting idea with id {Id}", id);
            LogToFile("ERROR", $"Error deleting idea with id {id}", ex);
            return TypedResults.Problem(
                detail: "An error occurred while deleting the idea.",
                statusCode: StatusCodes.Status500InternalServerError
            );
            throw;
        }

    }

    private static void LogToFile(string level, string message, Exception? ex = null)
    {
        var line = $"{DateTime.UtcNow:o} {level} {message}";
        if (ex is not null)
        {
            line += $" {ex}";
        }
        line += Environment.NewLine;
        File.AppendAllText("log.txt", line);
    }

}
