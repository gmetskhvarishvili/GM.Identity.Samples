using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Users.Commands.DeleteUser;

public class DeleteUserCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class DeleteUserCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id
                                      && x.IsActive
                                      && !x.IsDeleted
                                      && !x.IsHidden,
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.User,
                StringResource.Id,
                request.Id);
        }

        entity.SoftRemove();

        // Persist the aggregate
        unitOfWork.UserRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // A deleted user must not keep active sessions — revoke them so access stops immediately.
        await unitOfWork.RevokeAllUserSessionsAsync(sessionCache, entity.Id, cancellationToken);
    }
}
