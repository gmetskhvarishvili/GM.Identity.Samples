using FluentValidation;
using GM.Identity.Authorization;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Queries.GetUserInfo;

/// <summary>
/// OpenID Connect-style userinfo: resolves the caller's own opaque access token to the user it represents and
/// returns that user's standard claims. Returns <c>null</c> when the token is missing, unknown, revoked, or
/// expired, or when it is a client-credentials token (no user), so the endpoint can answer 401.
/// </summary>
public class GetUserInfoQuery : IRequest<UserInfoDto?>
{
    public string? AccessToken { get; set; }
}

public class GetUserInfoQueryValidator : AbstractValidator<GetUserInfoQuery>
{
    public GetUserInfoQueryValidator() => RuleFor(x => x.AccessToken).NotNull().NotEmpty();
}

public class GetUserInfoQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetUserInfoQuery, UserInfoDto?>
{
    public async Task<UserInfoDto?> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            return null;

        var hash = TokenGenerator.Hash(request.AccessToken);
        var now = DateTime.UtcNow;

        var session = await unitOfWork.UserSessionRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TokenHash == hash && !x.IsRevoked && x.ExpiresAt > now, cancellationToken);

        if (session?.UserId is not { } userId)
            return null;

        var user = await unitOfWork.UserRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);

        if (user == null)
            return null;

        return new UserInfoDto
        {
            Subject = user.Id.ToString(),
            PreferredUsername = user.UserName,
            Email = user.Email,
            EmailVerified = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberVerified = user.PhoneNumberConfirmed,
        };
    }
}

public class UserInfoDto
{
    [JsonPropertyName("sub")]
    public string Subject { get; set; } = null!;

    [JsonPropertyName("preferred_username")]
    public string PreferredUsername { get; set; } = null!;

    [JsonPropertyName("email")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }

    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("phone_number")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("phone_number_verified")]
    public bool PhoneNumberVerified { get; set; }
}
