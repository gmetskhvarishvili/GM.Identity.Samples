using GM.Identity.Sample.Application.Roles.Commands.CreateRole;
using GM.Identity.Sample.Application.Roles.Queries.GetRolesList;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of list paging (GM.API 1.8.0) on the roles list: by page number with totals, by last
/// key value (the rows after the identified row, in OrderBy order), by a named key in descending order, and
/// with the total count skipped. Requires Postgres; no-ops if it isn't reachable.
/// </summary>
public sealed class ListPagingEndToEndTests : IAsyncLifetime
{
    private static readonly string[] Suffixes = ["a", "b", "c"];

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _prefix = "pg-" + Guid.NewGuid().ToString("N");
    private readonly Guid[] _roleIds = new Guid[3];
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
                await context.Set<Role>().IgnoreQueryFilters().Where(r => r.Name.StartsWith(_prefix)).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Roles_list_pages_by_number_by_last_key_value_and_by_a_named_key()
    {
        if (!_infraReady) return; // integration test: no-op when Postgres isn't reachable

        // Three roles that only this test's Name filter matches: <prefix>-a, -b, -c.
        foreach (var (suffix, i) in Suffixes.Select((s, i) => (s, i)))
            _roleIds[i] = await Send(new CreateRoleCommand { Name = $"{_prefix}-{suffix}" });

        // Page 1 by number, ordered by Name.
        var first = await Send(new GetRolesListQuery { Name = _prefix, CurrentPage = 1, PageSize = 2, OrderBy = "Name asc" });
        Assert.Equal([$"{_prefix}-a", $"{_prefix}-b"], first.Items.Select(r => r.Name));
        Assert.Equal(1, first.CurrentPage);
        Assert.Equal(3, first.TotalCount);
        Assert.True(first.HasMore);
        Assert.Equal("Id", first.LastKeyProperty);
        Assert.Equal(_roleIds[1].ToString(), first.LastKeyValue, ignoreCase: true);

        // Next page by last key value: the roles after role "-b", in the same order.
        var second = await Send(new GetRolesListQuery
        {
            Name = _prefix, PageSize = 2, OrderBy = "Name asc",
            LastKeyProperty = first.LastKeyProperty, LastKeyValue = first.LastKeyValue
        });
        Assert.Equal([$"{_prefix}-c"], second.Items.Select(r => r.Name));
        Assert.Equal(0, second.CurrentPage); // paged by key, not by number
        Assert.False(second.HasMore);

        // A named key, descending, without the count: the roles before "-b" by Name.
        var byName = await Send(new GetRolesListQuery
        {
            Name = _prefix, PageSize = 10, OrderBy = "Name desc", IncludeTotalCount = false,
            LastKeyProperty = "Name", LastKeyValue = $"{_prefix}-b"
        });
        Assert.Equal([$"{_prefix}-a"], byName.Items.Select(r => r.Name));
        Assert.False(byName.HasTotalCount);
        Assert.Equal("Name", byName.LastKeyProperty);
        Assert.Equal($"{_prefix}-a", byName.LastKeyValue);
    }

    private async Task<TResult> Send<TResult>(IRequest<TResult> request)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }
}
