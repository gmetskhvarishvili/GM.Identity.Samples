using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.DeleteUserSession;

public class DeleteUserSessionCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid Id { get; set; }
}

public class DeleteUserSessionCommandValidator : AbstractValidator<DeleteUserSessionCommand>
{
    public DeleteUserSessionCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class DeleteUserSessionCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteUserSessionCommand>
{
    public async Task Handle(DeleteUserSessionCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.UserSessionRepository
            .FirstOrDefaultAsync(x => x.UserId == request.UserId
                                      && x.Id == request.Id,
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.UserSession,
                StringResource.Id,
                request.Id);
        }
        
        // Revoke raises GMUserSessionRevokedDomainEvent (in the GMUserSession base), which the dispatcher hands
        // to its handler after SaveChangesAsync — that handler triggers the background job that invalidates the
        // cached session (deferred, not an inline write), so it can't be used before its TTL expires.
        entity.Revoke();

        // Persist the aggregate (and dispatch its domain events).
        unitOfWork.UserSessionRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
