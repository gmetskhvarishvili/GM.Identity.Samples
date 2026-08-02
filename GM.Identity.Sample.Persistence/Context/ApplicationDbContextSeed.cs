using Microsoft.Extensions.Logging;

namespace GM.Identity.Sample.Persistence.Context;

public class ApplicationDbContextSeed
{
    public async Task SeedAsync(ApplicationDbContext context,
        ILogger<ApplicationDbContextSeed> logger, int? retry = 0)
    {
        int retryForAvaiability = retry.Value;

        try
        {
            
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