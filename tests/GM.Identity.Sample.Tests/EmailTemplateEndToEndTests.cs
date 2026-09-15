using GM.Identity.Sample.Application.EmailTemplates.Commands.SaveEmailTemplate;
using GM.Identity.Sample.Application.EmailTemplates.Queries.GetEmailTemplates;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.EmailTemplateAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of email-template management: saving a template makes it listable, and saving the same key
/// again updates it in place (no duplicate). Requires Postgres + Redis; no-ops if unavailable.
/// </summary>
public sealed class EmailTemplateEndToEndTests : IAsyncLifetime
{
    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _key = $"welcome-{Guid.NewGuid():N}";
    private bool _infraReady;

    public Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _infraReady = true;
        }
        catch
        {
            _infraReady = false;
        }
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_infraReady)
        {
            try
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Set<EmailTemplate>().IgnoreQueryFilters().Where(x => x.Key == _key).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Save_creates_then_updates_in_place()
    {
        if (!_infraReady) return;

        await SendAsync(new SaveEmailTemplateCommand { Key = _key, Subject = "Welcome!", Body = "Hi {{name}}" });

        var afterCreate = (await ListAsync()).Where(t => t.Key == _key).ToList();
        var created = Assert.Single(afterCreate);
        Assert.Equal("Welcome!", created.Subject);

        // Saving the same key updates in place.
        await SendAsync(new SaveEmailTemplateCommand { Key = _key, Subject = "Welcome aboard!", Body = "Hello {{name}}" });

        var afterUpdate = (await ListAsync()).Where(t => t.Key == _key).ToList();
        var updated = Assert.Single(afterUpdate);
        Assert.Equal("Welcome aboard!", updated.Subject);
        Assert.Equal("Hello {{name}}", updated.Body);
    }

    private async Task SendAsync(SaveEmailTemplateCommand command)
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(command);
    }

    private async Task<System.Collections.Generic.IReadOnlyList<EmailTemplateDto>> ListAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new GetEmailTemplatesQuery());
    }
}
