using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.UnlockUser;

/// <summary>Clears a user's failed-login lockout (resets the attempt counter and the lockout window).</summary>
public class UnlockUserCommand : IRequest
{
    public Guid UserId { get; set; }
}

public class UnlockUserCommandValidator : AbstractValidator<UnlockUserCommand>
{
    public UnlockUserCommandValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class UnlockUserCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UnlockUserCommand>
{
    public async Task Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, true, null, cancellationToken);

        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        user.Unlock();

        unitOfWork.UserRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
