using System;
using System.Linq;
using GestãoIdeas.Models;

namespace GestãoIdeas.Data;

public static class IdeaMaintenanceService
{
    public static void UpdateOutdatedIdeas(IdeaContext db, DateTime utcNow)
    {
        var cutoffDate = utcNow.AddMonths(-1);

        var outdatedIdeas = db.Ideas
            .Where(i => i.CreatedAt <= cutoffDate && i.State != IdeaState.COMPLETED && i.State != IdeaState.ABANDONED)
            .ToList();

        if (outdatedIdeas.Count == 0)
        {
            return;
        }

        foreach (var idea in outdatedIdeas)
        {
            idea.State = IdeaState.ABANDONED;
        }

        db.SaveChanges();
    }
}
