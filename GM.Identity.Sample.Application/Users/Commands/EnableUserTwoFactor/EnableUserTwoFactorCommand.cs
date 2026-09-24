using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Application.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.EnableUserTwoFactor;

/// <summary>
/// Begins enrolling an existing user in a second-factor method. The enrolment starts <em>pending</em>: it does
/// not gate login until the user confirms it (see the confirm command), so a user who cannot receive codes is
/// never locked out. A one-time setup code is issued (via the outbox, like the login challenge) so the user can
/// confirm. Re-enabling a pending method re-issues the code; a method already confirmed is a no-op.
/// </summary>
public class EnableUserTwoFactorCommand : IRequest
{
    public Guid UserId { get; set; }

    /// <summary>The 2FA method to enrol the user in (see the TwoFactorAuthType reference data).</summary>
    public int TwoFactorAuthTypeId { get; set; }
}

public class EnableUserTwoFactorCommandValidator : AbstractValidator<EnableUserTwoFactorCommand>
{
    public EnableUserTwoFactorCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.TwoFactorAuthTypeId).GreaterThan(0);
    }
}

public class EnableUserTwoFactorCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<EnableUserTwoFactorCommand>
{
    public async Task Handle(EnableUserTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, true, null, cancellationToken);
        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        var typeExists = await unitOfWork.TwoFactorAuthTypeRepository.ExistsAsync(
            x => x.Id == request.TwoFactorAuthTypeId && x.IsActive && !x.IsDeleted && !x.IsHidden,
            cancellationToken);
        if (!typeExists)
            throw new NotFoundException(StringResource.TwoFactorAuthType, StringResource.Id, request.TwoFactorAuthTypeId);

        var existing = await unitOfWork.UserTwoFactorAuthTypeRepository
            .FirstOrDefaultAsync(
                x => x.UserId == request.UserId
                     && x.TwoFactorAuthTypeId == request.TwoFactorAuthTypeId
                     && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);

        // Already fully enrolled → nothing to do.
        if (existing is { IsConfirmed: true })
            return;

        if (existing == null)
        {
            var entity = UserTwoFactorAuthType.Create(request.UserId, request.TwoFactorAuthTypeId);
            await unitOfWork.UserTwoFactorAuthTypeRepository.AddAsync(entity, cancellationToken);
        }

        // Issue a setup code to the user's contact so they can confirm the enrolment. Reuses the same OTP
        // purpose/subject as the login challenge, so the confirm step validates against it.
        await IssueSetupCodeAsync(user, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task IssueSetupCodeAsync(User user, CancellationToken cancellationToken)
    {
        var subject = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : user.PhoneNumber;
        if (string.IsNullOrWhiteSpace(subject))
            return; // No contact to send a code to; the enrolment stays pending until confirmed by other means.

        await unitOfWork.OutboxMessageRepository.AddAsync(
            OutboxMessage.From(user.Id, new TwoFactorChallengeIssuedIntegrationEvent(subject) { UserId = user.Id }),
            cancellationToken);
    }
}
