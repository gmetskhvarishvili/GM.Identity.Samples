using Microsoft.Extensions.Logging;

namespace GM.Identity.Sample.Persistence.Context;

public class ApplicationDbContextSeed
{
    public async Task SeedAsync(ApplicationDbContext context,
        ILogger<ApplicationDbContextSeed> logger, int? retry = 0)
    {
        int retryForAvaiability = retry ?? 0;

        try
        {
            // No seed data yet — this is a placeholder for future reference-data seeding,
            // kept so the retry/backoff scaffolding around it is ready to use.
        }
        catch (Exception ex)
        {
            if (retryForAvaiability < 10)
            {
                retryForAvaiability++;

                logger.LogError(ex, "EXCEPTION ERROR while migrating {DbContextName}", nameof(ApplicationDbContext));

                await SeedAsync(context, logger, retryForAvaiability);
            }
        }
    }
}