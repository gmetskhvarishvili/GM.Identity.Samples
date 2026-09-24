using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.EnableUserTwoFactor;

/// <summary>
/// Enrols an existing user in a second-factor method and activates it immediately — no setup code or confirmation
/// step. From then on the method gates login (the actual one-time code is issued at login time). Enabling a method
/// the user already has is a no-op.
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
        var userExists = await unitOfWork.UserRepository.ExistsAsync(
            x => x.Id == request.UserId && !x.IsDeleted, cancellationToken);
        if (!userExists)
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

        // Already enrolled and active → nothing to do.
        if (existing is { IsConfirmed: true })
            return;

        if (existing == null)
        {
            var entity = UserTwoFactorAuthType.Create(request.UserId, request.TwoFactorAuthTypeId);
            entity.Confirm();
            await unitOfWork.UserTwoFactorAuthTypeRepository.AddAsync(entity, cancellationToken);
        }
        else
        {
            existing.Confirm();
            unitOfWork.UserTwoFactorAuthTypeRepository.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
