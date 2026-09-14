using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.Extensions.Options;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.ImpersonateUser;

/// <summary>
/// Issues a session that authenticates <em>as</em> another user, for an authorized administrator (support
/// login-as). The minted session is tagged with the impersonating admin (provider = <c>impersonation:{adminId}</c>)
/// and, via the actor-audit interceptor, records the admin as its creator — so every impersonation is traceable.
/// </summary>
public class ImpersonateUserCommand : IRequest<ImpersonationTokenDto>
{
    /// <summary>The user to impersonate.</summary>
    public Guid TargetUserId { get; set; }

    /// <summary>The OAuth client the impersonation session runs under (the admin app).</summary>
    public Guid ClientId { get; set; }

    /// <summary>The administrator performing the impersonation (recorded for audit).</summary>
    public Guid ImpersonatorUserId { get; set; }
}

public class ImpersonateUserCommandValidator : AbstractValidator<ImpersonateUserCommand>
{
    public ImpersonateUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotNull().NotEmpty();
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ImpersonatorUserId).NotNull().NotEmpty();
        RuleFor(x => x).Must(x => x.TargetUserId != x.ImpersonatorUserId)
            .WithMessage("A user cannot impersonate themselves.");
    }
}

public class ImpersonateUserCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache,
    IOptions<AuthOptions> options) : IRequestHandler<ImpersonateUserCommand, ImpersonationTokenDto>
{
    public async Task<ImpersonationTokenDto> Handle(ImpersonateUserCommand request, CancellationToken cancellationToken)
    {
        var target = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(
                x => x.Id == request.TargetUserId && x.IsActive && !x.IsDeleted && !x.IsHidden && !x.IsBlocked,
                false, null, cancellationToken);
        if (target == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.TargetUserId);

        var settings = options.Value;
        var now = DateTime.UtcNow;

        var accessToken = TokenGenerator.Generate();
        var refreshToken = TokenGenerator.Generate();
        var accessExpiry = now.AddMinutes(settings.AccessTokenMinutes);
        var accessHash = TokenGenerator.Hash(accessToken);
        var refreshExpiry = now.AddDays(settings.RefreshTokenDays);

        var session = UserSession.Create(
            target.Id, request.ClientId, $"impersonation:{request.ImpersonatorUserId}",
            accessHash, refreshExpiry, TokenGenerator.Hash(refreshToken));

        await unitOfWork.UserSessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await sessionCache.SetAsync(
            accessHash, new SessionInfo(target.Id, session.Id, request.ClientId, accessExpiry), cancellationToken);

        return new ImpersonationTokenDto(accessToken, accessExpiry, "bearer", refreshToken, refreshExpiry);
    }
}

public record ImpersonationTokenDto(
    string AccessToken, DateTime ExpiresAt, string TokenType, string RefreshToken, DateTime RefreshTokenExpiresAt);
