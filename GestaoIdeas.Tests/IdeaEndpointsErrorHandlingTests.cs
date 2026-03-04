using System.Net;
using System.Net.Http.Json;
using GestãoIdeas.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestaoIdeas.Tests;

public class IdeaApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration so we can plug in a test database
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IdeaContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            // Use a single in-memory SQLite connection for the lifetime of the test host
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            services.AddDbContext<IdeaContext>(options =>
            {
                options.UseSqlite(connection);
            });
        });
    }
}

public class IdeaEndpointsErrorHandlingTests : IClassFixture<IdeaApplicationFactory>
{
    private readonly HttpClient _client;

    public IdeaEndpointsErrorHandlingTests(IdeaApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetIdeaById_NonExisting_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/ideas/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetIdeasByPriority_NoMatches_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/ideas/priority?priority=9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateIdea_NonExisting_ReturnsNotFound()
    {
        var updateBody = new
        {
            Name = "Does not matter",
            Description = "Test description",
            // Use numeric enum value to avoid any model-binding edge cases
            State = 0,
            Priority = 1
        };

        var response = await _client.PutAsJsonAsync("/ideas/9999", updateBody);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteIdea_NonExisting_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/ideas/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateIdea_InvalidModel_ReturnsBadRequest()
    {
        // Missing required Name field, should trigger validation failure
        var invalidBody = new
        {
            Description = "No name provided",
            State = "IN_DEVELOPMENT",
            Priority = 1
        };

        var response = await _client.PostAsJsonAsync("/ideas", invalidBody);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
