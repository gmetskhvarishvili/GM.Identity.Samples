using GM.Identity.Sample.Application.Events.Users;
using GM.Identity.Sample.Application.Users.Commands.SetUserTwoFactor;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Enums;
using GM.Identity.Sample.Persistence.Context;
using GM.Mediator.Contracts;
using GM.Testing;
using GM.Testing.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// End-to-end proof that the user chooses where a 2FA code goes. The password grant lists the enrolled methods
/// and sends nothing when there is a choice; POST /connect/two-factor/send then queues the code over the chosen
/// method (to the phone for SMS/WhatsApp, to the email for Email). With a single delivery method the password
/// grant sends right away. Requires Postgres + Redis; no-ops if they aren't reachable.
/// </summary>
public sealed class TwoFactorDeliveryEndToEndTests : IAsyncLifetime
{
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string ClientSecret = "secret";
    private const string Password = "Correct123!";
    private const string Phone = "+995555000111";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly GmWebApplicationFactory<Program> _factory = new();
    private readonly string _choiceUser = $"tfa-choice-{Guid.NewGuid():N}";   // Email + WhatsApp
    private readonly string _singleUser = $"tfa-single-{Guid.NewGuid():N}";   // Email only
    private readonly string _noPhoneUser = $"tfa-nophone-{Guid.NewGuid():N}"; // no methods, no phone
    private readonly List<Guid> _userIds = new();
    private Guid _choiceUserId;
    private Guid _singleUserId;
    private Guid _noPhoneUserId;
    private bool _infraReady;

    public async Task InitializeAsync()
    {
        try
        {
            _choiceUserId = await CreateUserAsync(_choiceUser, Phone, TwoFactorMethod.Email, TwoFactorMethod.WhatsApp);
            _singleUserId = await CreateUserAsync(_singleUser, Phone, TwoFactorMethod.Email);
            _noPhoneUserId = await CreateUserAsync(_noPhoneUser, phone: null);
            _infraReady = true;
        }
        catch
        {
            _infraReady = false; // Postgres/Redis unavailable — tests will no-op.
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
                await context.Set<OutboxMessage>().Where(x => x.UserId != null && _userIds.Contains(x.UserId.Value)).ExecuteDeleteAsync();
                await context.Set<UserTwoFactorAuthType>().Where(x => _userIds.Contains(x.UserId)).ExecuteDeleteAsync();
                await context.Set<User>().IgnoreQueryFilters().Where(u => _userIds.Contains(u.Id)).ExecuteDeleteAsync();
            }
            catch { /* best-effort cleanup */ }
        }
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task With_a_choice_the_password_grant_lists_methods_and_the_chosen_one_gets_the_code()
    {
        if (!_infraReady) return;

        var challenge = await PasswordGrantAsync(_choiceUser);
        Assert.True(challenge.TwoFactorRequired);
        Assert.Contains((int)TwoFactorMethod.Email, challenge.TwoFactorAuthTypeIds!);
        Assert.Contains((int)TwoFactorMethod.WhatsApp, challenge.TwoFactorAuthTypeIds!);
        Assert.Null(challenge.TwoFactorCodeSentVia);
        Assert.Empty(await ChallengesQueuedForAsync(_choiceUserId)); // nothing sent until the user picks

        var response = await SendCodeAsync(_choiceUser, Password, TwoFactorMethod.WhatsApp);
        await response.ShouldBeOkAsync();

        var queued = Assert.Single(await ChallengesQueuedForAsync(_choiceUserId));
        Assert.Equal(Phone, queued.Destination);
        Assert.Equal((int)ConfirmationType.WhatsApp, queued.Channel);
        Assert.Equal($"{_choiceUser}@test.local", queued.Subject); // verified under the same key whatever the channel
    }

    [Fact]
    public async Task With_one_delivery_method_the_password_grant_sends_the_code_right_away()
    {
        if (!_infraReady) return;

        var challenge = await PasswordGrantAsync(_singleUser);
        Assert.True(challenge.TwoFactorRequired);
        Assert.Equal((int)TwoFactorMethod.Email, challenge.TwoFactorCodeSentVia);

        var queued = Assert.Single(await ChallengesQueuedForAsync(_singleUserId));
        Assert.Equal($"{_singleUser}@test.local", queued.Destination);
        Assert.Equal((int)ConfirmationType.Email, queued.Channel);
    }

    [Fact]
    public async Task Sending_over_a_method_the_user_is_not_enrolled_in_is_rejected()
    {
        if (!_infraReady) return;

        var response = await SendCodeAsync(_choiceUser, Password, TwoFactorMethod.Sms);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ChallengesQueuedForAsync(_choiceUserId));
    }

    [Fact]
    public async Task Sending_with_a_wrong_password_is_rejected()
    {
        if (!_infraReady) return;

        var response = await SendCodeAsync(_choiceUser, "wrong-password", TwoFactorMethod.Email);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ChallengesQueuedForAsync(_choiceUserId));
    }

    [Fact]
    public async Task Enabling_a_phone_method_needs_a_phone_number()
    {
        if (!_infraReady) return;

        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<GM.Exceptions.ValidationException>(() => mediator.Send(new SetUserTwoFactorCommand
        {
            UserId = _noPhoneUserId,
            TwoFactorAuthTypeId = (int)TwoFactorMethod.Sms,
            Enabled = true,
        }));
    }

    private async Task<Guid> CreateUserAsync(string userName, string? phone, params TwoFactorMethod[] methods)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = User.Create(userName, $"{userName}@test.local", phone);
        var (hash, salt) = PasswordHasher.Hash(Password);
        user.UpdatePassword(hash, salt);
        await context.Set<User>().AddAsync(user);
        _userIds.Add(user.Id);

        foreach (var method in methods)
            await context.Set<UserTwoFactorAuthType>().AddAsync(UserTwoFactorAuthType.Create(user.Id, (int)method));

        await context.SaveChangesAsync();
        return user.Id;
    }

    private async Task<TokenResponse> PasswordGrantAsync(string userName)
    {
        var response = await _factory.CreateClient().PostAsync("/connect/token", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = userName,
                ["password"] = Password,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
            }));
        await response.ShouldBeOkAsync();
        return JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync(), JsonOptions)!;
    }

    private Task<HttpResponseMessage> SendCodeAsync(string userName, string password, TwoFactorMethod method) =>
        _factory.CreateClient().PostAsync("/connect/two-factor/send", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["username"] = userName,
                ["password"] = password,
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["two_factor_auth_type_id"] = ((int)method).ToString(CultureInfo.InvariantCulture),
            }));

    private async Task<List<TwoFactorChallengeIssuedIntegrationEvent>> ChallengesQueuedForAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var payloads = await context.Set<OutboxMessage>()
            .Where(x => x.UserId == userId && x.EventType.Contains(nameof(TwoFactorChallengeIssuedIntegrationEvent)))
            .Select(x => x.Payload)
            .ToListAsync();
        return payloads.Select(p => JsonSerializer.Deserialize<TwoFactorChallengeIssuedIntegrationEvent>(p)!).ToList();
    }

    private sealed record TokenResponse(
        string? AccessToken,
        bool TwoFactorRequired,
        List<int>? TwoFactorAuthTypeIds,
        bool TwoFactorAuthenticatorApp,
        int? TwoFactorCodeSentVia);
}
