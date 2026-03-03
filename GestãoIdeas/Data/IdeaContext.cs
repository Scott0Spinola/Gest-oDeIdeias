using GestãoIdeas.Models;
using Microsoft.EntityFrameworkCore;
namespace GestãoIdeas.Data;

public class IdeaContext(DbContextOptions<IdeaContext> options) : DbContext(options)
{
    public DbSet<Idea> Ideas => Set<Idea>();
}


