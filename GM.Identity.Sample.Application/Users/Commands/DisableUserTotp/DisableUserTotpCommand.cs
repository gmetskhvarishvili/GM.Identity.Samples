using FluentValidation;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.DisableUserTotp;

/// <summary>Removes a user's authenticator-app (TOTP) device. Idempotent — a no-op if none is enrolled.</summary>
public class DisableUserTotpCommand : IRequest
{
    public Guid UserId { get; set; }
}

public class DisableUserTotpCommandValidator : AbstractValidator<DisableUserTotpCommand>
{
    public DisableUserTotpCommandValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class DisableUserTotpCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DisableUserTotpCommand>
{
    public async Task Handle(DisableUserTotpCommand request, CancellationToken cancellationToken)
    {
        var devices = await unitOfWork.UserTotpDeviceRepository
            .Query(true, null)
            .Where(x => x.UserId == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .ToListAsync(cancellationToken);

        if (devices.Count == 0)
            return;

        unitOfWork.UserTotpDeviceRepository.RemoveRange(devices);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
