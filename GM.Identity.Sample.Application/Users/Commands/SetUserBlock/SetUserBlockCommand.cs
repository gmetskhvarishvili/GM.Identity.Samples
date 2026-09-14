using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.SetUserBlock;

/// <summary>Blocks or unblocks a user. Blocking revokes the user's active sessions immediately.</summary>
public class SetUserBlockCommand : IRequest
{
    public Guid UserId { get; set; }
    public bool Block { get; set; }
}

public class SetUserBlockCommandValidator : AbstractValidator<SetUserBlockCommand>
{
    public SetUserBlockCommandValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class SetUserBlockCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<SetUserBlockCommand>
{
    public async Task Handle(SetUserBlockCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, true, null, cancellationToken);

        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        if (request.Block)
            user.Block();
        else
            user.UnBlock();

        unitOfWork.UserRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // A blocked user's live tokens must stop working now, not at their TTL.
        if (request.Block)
            await unitOfWork.RevokeAllUserSessionsAsync(sessionCache, user.Id, cancellationToken);
    }
}
