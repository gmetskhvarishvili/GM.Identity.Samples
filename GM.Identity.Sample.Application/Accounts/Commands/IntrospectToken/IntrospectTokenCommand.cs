using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.IntrospectToken;

/// <summary>
/// OAuth 2.0 token introspection (RFC 7662). Authenticates the calling client, then reports whether the given
/// opaque access or refresh token is currently active and, if so, its metadata. An inactive/unknown token
/// yields <c>{ active: false }</c> without leaking why.
/// </summary>
public class IntrospectTokenCommand : IRequest<IntrospectionResultDto>
{
    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; } = null!;
    public string Token { get; set; } = null!;
}

public class IntrospectTokenCommandValidator : AbstractValidator<IntrospectTokenCommand>
{
    public IntrospectTokenCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ClientSecret).NotNull().NotEmpty();
        RuleFor(x => x.Token).NotNull().NotEmpty();
    }
}

public class IntrospectTokenCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<IntrospectTokenCommand, IntrospectionResultDto>
{
    public async Task<IntrospectionResultDto> Handle(IntrospectTokenCommand request, CancellationToken cancellationToken)
    {
        // The caller must authenticate as a known client (cross-tenant, like the token endpoint).
        var client = await unitOfWork.ClientRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.ClientId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);

        if (client == null || !PasswordHasher.Verify(request.ClientSecret, client.SecretHash, client.SecretSalt))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var now = DateTime.UtcNow;
        var hash = TokenGenerator.Hash(request.Token);

        // A user session matches on its access-token hash or its refresh-token hash.
        var userSession = await unitOfWork.UserSessionRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TokenHash == hash || x.RefreshTokenHash == hash, cancellationToken);
        if (userSession != null)
        {
            if (userSession.IsRevoked || userSession.ExpiresAt <= now)
                return IntrospectionResultDto.Inactive;

            var isRefresh = userSession.RefreshTokenHash == hash && userSession.TokenHash != hash;
            return await BuildActiveAsync(
                userSession.ClientId, userSession.UserId, userSession.ExpiresAt,
                isRefresh ? "refresh_token" : "access_token", cancellationToken);
        }

        var clientSession = await unitOfWork.ClientSessionRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (clientSession != null)
        {
            if (clientSession.IsRevoked || clientSession.ExpiresAt <= now)
                return IntrospectionResultDto.Inactive;

            return await BuildActiveAsync(
                clientSession.ClientId, userId: null, clientSession.ExpiresAt, "access_token", cancellationToken);
        }

        return IntrospectionResultDto.Inactive;
    }

    private async Task<IntrospectionResultDto> BuildActiveAsync(
        Guid? clientId, Guid? userId, DateTime expiresAt, string tokenType, CancellationToken cancellationToken)
    {
        // The token is bound to its client's granted scopes.
        string? scope = null;
        if (clientId is { } cid)
        {
            var scopeIds = await unitOfWork.ClientScopeRepository
                .Query(false, null)
                .IgnoreQueryFilters()
                .Where(x => x.ClientId == cid && x.IsActive && !x.IsDeleted && !x.IsHidden)
                .Select(x => x.ScopeId)
                .ToListAsync(cancellationToken);

            if (scopeIds.Count > 0)
            {
                var names = await unitOfWork.ScopeRepository
                    .Query(false, null)
                    .IgnoreQueryFilters()
                    .Where(x => scopeIds.Contains(x.Id))
                    .Select(x => x.Name)
                    .ToListAsync(cancellationToken);
                if (names.Count > 0)
                    scope = string.Join(' ', names);
            }
        }

        return new IntrospectionResultDto
        {
            Active = true,
            Subject = userId?.ToString(),
            ClientId = clientId?.ToString(),
            TokenType = tokenType,
            Expiry = new DateTimeOffset(expiresAt, TimeSpan.Zero).ToUnixTimeSeconds(),
            Scope = scope,
        };
    }
}

/// <summary>The RFC 7662 introspection response. Fields other than <c>active</c> are omitted when null.</summary>
public class IntrospectionResultDto
{
    public static readonly IntrospectionResultDto Inactive = new() { Active = false };

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("sub")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Subject { get; set; }

    [JsonPropertyName("client_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClientId { get; set; }

    [JsonPropertyName("token_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TokenType { get; set; }

    [JsonPropertyName("exp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Expiry { get; set; }

    [JsonPropertyName("scope")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Scope { get; set; }
}
