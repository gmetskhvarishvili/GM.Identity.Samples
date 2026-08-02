using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Infrastructure;
using GM.Identity.Sample.Persistence;
using GM.Identity.Sample.Persistence.Context;
using GM.Messaging.Workers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddProducerWorkerInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddHostedService<OutboxRelayWorker<OutboxMessage>>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (context != null)
        {
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
                await context.Database.MigrateAsync();

            var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContextSeed>>();
            if (logger != null)
                new ApplicationDbContextSeed().SeedAsync(context, logger).Wait();
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
    }
}

app.Run();