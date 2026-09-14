using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common.Security;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.AuthorizationCodeAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.AuthorizeCode;

/// <summary>
/// The authorization endpoint of the PKCE authorization-code flow. Validates the client, the registered
/// redirect URI, and the PKCE challenge, authenticates the resource owner, and mints a short-lived single-use
/// authorization code bound to all of them. In a browser deployment this sits behind a login + consent UI;
/// here it takes the user's credentials directly so the flow is exercisable without a front-end.
/// </summary>
public class AuthorizeCodeCommand : IRequest<AuthorizeCodeResponseDto>
{
    public Guid ClientId { get; set; }
    public string RedirectUri { get; set; } = null!;
    public string? Scope { get; set; }
    public string? State { get; set; }

    /// <summary>PKCE code challenge — BASE64URL(SHA256(code_verifier)).</summary>
    public string CodeChallenge { get; set; } = null!;

    /// <summary>PKCE method; only <c>S256</c> is supported.</summary>
    public string CodeChallengeMethod { get; set; } = "S256";

    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class AuthorizeCodeCommandValidator : AbstractValidator<AuthorizeCodeCommand>
{
    public AuthorizeCodeCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.RedirectUri).NotNull().NotEmpty();
        RuleFor(x => x.CodeChallenge).NotNull().NotEmpty();
        RuleFor(x => x.CodeChallengeMethod).Equal("S256").WithMessage("Only the S256 PKCE method is supported.");
        RuleFor(x => x.UserName).NotNull().NotEmpty();
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}

public class AuthorizeCodeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<AuthorizeCodeCommand, AuthorizeCodeResponseDto>
{
    // Authorization codes are short-lived by design (RFC 6749 §4.1.2 recommends <= 10 minutes; we use 1).
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(1);

    public async Task<AuthorizeCodeResponseDto> Handle(AuthorizeCodeCommand request, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.ClientId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (client == null)
            throw new NotFoundException(StringResource.Client, StringResource.Id, request.ClientId);

        // The redirect URI must be one registered for the client (exact match) — never a caller-supplied one.
        var redirectRegistered = await unitOfWork.ClientRedirectUriRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .AnyAsync(x => x.ClientId == request.ClientId && x.Uri == request.RedirectUri
                           && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (!redirectRegistered)
            throw new ValidationException("The redirect URI is not registered for this client.");

        // Authenticate the resource owner (cross-tenant, like the token endpoint).
        var user = await unitOfWork.UserRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserName == request.UserName && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);

        if (user == null || user.IsBlocked
            || (user.LockoutEnd is { } lockoutEnd && lockoutEnd > DateTime.UtcNow)
            || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }

        var code = TokenGenerator.Generate();
        var authCode = AuthorizationCode.Create(
            request.ClientId, user.Id, TokenGenerator.Hash(code), request.RedirectUri,
            request.Scope, request.CodeChallenge, request.CodeChallengeMethod, DateTime.UtcNow.Add(CodeLifetime));

        await unitOfWork.AuthorizationCodeRepository.AddAsync(authCode, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // The caller redirects the user agent here (code + state on the registered redirect URI).
        var separator = request.RedirectUri.Contains('?') ? '&' : '?';
        var redirectTo = $"{request.RedirectUri}{separator}code={Uri.EscapeDataString(code)}";
        if (!string.IsNullOrEmpty(request.State))
            redirectTo += $"&state={Uri.EscapeDataString(request.State)}";

        return new AuthorizeCodeResponseDto(code, request.State, redirectTo);
    }
}

public record AuthorizeCodeResponseDto(string Code, string? State, string RedirectTo);
