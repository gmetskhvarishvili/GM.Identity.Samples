using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Accounts.Commands.Authorize;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Infrastructure.Services.OAuth;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using GM.Identity.Sample.Application.Users.Commands.CreateUserRole;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ValidationException = FluentValidation.ValidationException;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Accounts.Commands.ExternalAuthorize;

public class ExternalAuthorizeCommand : IRequest<AuthorizeResponseDto>
{
    public string Code { get; set; } = null!;
    public string State { get; set; } = null!;
    public string RedirectUri { get; set; } = null!;
    public string Provider { get; set; } = null!;
    
    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; } = null!;
}

public class ExternalAuthorizeCommandValidator : AbstractValidator<ExternalAuthorizeCommand>
{
    public ExternalAuthorizeCommandValidator()
    {
        RuleFor(x => x.Code).NotNull().NotEmpty();
        RuleFor(x => x.State).NotNull().NotEmpty();
        RuleFor(x => x.RedirectUri).NotNull().NotEmpty();
        RuleFor(x => x.Provider).NotNull().NotEmpty();
    }
}

public class ExternalAuthorizeCommandHandler(
    IOAuthService oAuthService,
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache,
    IAuthOptions options)
    : IRequestHandler<ExternalAuthorizeCommand, AuthorizeResponseDto>
{
    public async Task<AuthorizeResponseDto> Handle(ExternalAuthorizeCommand request, CancellationToken cancellationToken)
    {
        // Authentication is cross-tenant (see AuthorizeCommand): resolve the client/user across tenants.
        var client = await unitOfWork.ClientRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.Id == request.ClientId &&
                     x.IsActive &&
                     !x.IsDeleted &&
                     !x.IsHidden,
                cancellationToken);

        if (client == null)
        {
            throw new NotFoundException(
                StringResource.Client,
                StringResource.Id,
                request.ClientId);
        }
        
        if (!PasswordHasher.Verify(
                request.ClientSecret, 
                client.SecretHash, 
                client.SecretSalt))
        {
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }
        
        var email = await oAuthService.GetEmail(
            request.Adapt<GetEmailDto>(),
            cancellationToken);
        
        var user = await unitOfWork.UserRepository
            .Query(true, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.Email == email &&
                     x.IsActive &&
                     !x.IsDeleted &&
                     !x.IsHidden,
                cancellationToken);

        if (user == null)
        {
            if (await unitOfWork.UserRepository.ExistsAsync(
                    x => x.UserName == email
                         && x.IsActive
                         && !x.IsDeleted
                         && !x.IsHidden,
                    cancellationToken))
            {
                throw new AlreadyExistsException(
                    StringResource.User,
                    StringResource.UserName,
                    email);
            }
            
            user = User
                .Create(email, email, null);

            await unitOfWork.UserRepository.AddAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Two-factor step-up: an existing user enrolled in a 2FA method must complete a second factor
        // before a session is issued — return a challenge naming the enrolled methods. (A just-created
        // external user has none, so this only gates users who previously configured 2FA.) The second
        // factor's verification mechanism is intentionally left as a follow-up step.
        var twoFactorTypeIds = await unitOfWork.UserTwoFactorAuthTypeRepository
            .Query(false, null)
            .Where(x => x.UserId == user.Id && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.TwoFactorAuthTypeId)
            .ToListAsync(cancellationToken);
        if (twoFactorTypeIds.Count > 0)
            return AuthorizeResponseDto.TwoFactorChallenge(twoFactorTypeIds);

        // Issue a short-lived access token + a rotating refresh token, exactly like the password grant:
        // the session's absolute expiry is the refresh lifetime, while the Redis access entry carries the
        // shorter access TTL. The refresh grant (grant_type=refresh_token) then rotates these too.
        var settings = options;
        var now = DateTime.UtcNow;
        var accessToken = TokenGenerator.Generate();
        var refreshToken = TokenGenerator.Generate();
        var accessExpiry = now.AddMinutes(settings.AccessTokenMinutes);
        var refreshExpiry = now.AddDays(settings.RefreshTokenDays);
        var accessHash = TokenGenerator.Hash(accessToken);

        var session = UserSession.Create(
            user.Id,
            client.Id,
            request.Provider,
            accessHash,
            refreshExpiry,
            TokenGenerator.Hash(refreshToken));

        await unitOfWork.UserSessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish the access session to Redis (keyed by the access token hash) for gateway-side validation.
        await sessionCache.SetAsync(
            accessHash,
            new SessionInfo(user.Id, session.Id, client.Id, accessExpiry),
            cancellationToken);

        return new AuthorizeResponseDto(accessToken, accessExpiry, "bearer", refreshToken, refreshExpiry);
    }
}
