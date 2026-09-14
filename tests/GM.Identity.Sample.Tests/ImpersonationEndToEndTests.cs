using GM.Identity.Sample.Application.Users.Commands.ImpersonateUser;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof of admin impersonation: the minted token authenticates as the target user (userinfo
/// returns the target's claims) and the session records the acting admin for audit. Requires Postgres + Redis;
/// no-ops if they aren't reachable.
/// </summary>
public sealed class ImpersonationEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string Password = "Correct123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _adminName = $"imp-admin-{Guid.NewGuid():N}";
    private readonly string _targetName = $"imp-target-{Guid.NewGuid():N}";
    private Guid _adminId;
    private Guid _targetId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _adminId = await CreateUserAsync(context, _adminName);
            _targetId = await CreateUserAsync(context, _targetName);
            _infraReady = true;
        }
        catch
        {
            _infraReady = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_infraReady)
        {
            try
            {
                using var scope = _factory.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Set<UserSession>().Where(s => s.UserId == _targetId || s.UserId == _adminId).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => u.Id == _targetId || u.Id == _adminId).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Impersonation_token_authenticates_as_the_target_and_records_the_admin()
    {
        if (!_infraReady) return;

        string accessToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new ImpersonateUserCommand
            {
                TargetUserId = _targetId,
                ClientId = Guid.Parse(ClientId),
                ImpersonatorUserId = _adminId,
            });
            accessToken = result.AccessToken;
        }

        // The token resolves to the TARGET user.
        var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _factory.CreateClient().SendAsync(request);
        await response.ShouldBeOkAsync();
        var info = JsonSerializer.Deserialize<UserInfo>(await response.Content.ReadAsStringAsync(), JsonOptions);
        Assert.Equal(_targetId.ToString(), info!.Subject);
        Assert.Equal(_targetName, info.PreferredUsername);

        // The session is tagged with the acting admin for audit.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var session = await context.Set<UserSession>()
                .IgnoreQueryFilters()
                .FirstAsync(s => s.UserId == _targetId);
            Assert.Equal($"impersonation:{_adminId}", session.Provider);
        }
    }

    private static async Task<Guid> CreateUserAsync(ApplicationDbContext context, string userName)
    {
        var user = User.Create(userName, $"{userName}@test.local", null);
        var (hash, salt) = PasswordHasher.Hash(Password);
        user.UpdatePassword(hash, salt);
        await context.Set<User>().AddAsync(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private sealed record UserInfo(
        [property: JsonPropertyName("sub")] string Subject,
        [property: JsonPropertyName("preferred_username")] string PreferredUsername);
}
