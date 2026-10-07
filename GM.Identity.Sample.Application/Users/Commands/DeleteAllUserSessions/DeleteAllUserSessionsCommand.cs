using FluentValidation;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace GM.Identity.Sample.Application.Users.Commands.DeleteAllUserSessions;

public class DeleteAllUserSessionsCommand : IRequest
{
    public Guid UserId { get; set; }
}

public class DeleteAllUserSessionsCommandValidator : AbstractValidator<DeleteAllUserSessionsCommand>
{
    public DeleteAllUserSessionsCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
    }
}

public class DeleteAllUserSessionsCommandHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAllUserSessionsCommand>
{
    public async Task Handle(DeleteAllUserSessionsCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entities = await unitOfWork.UserSessionRepository
            .Query(true,
                null)
            .Where(x => x.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        // Each Revoke() raises GMUserSessionRevokedDomainEvent; after SaveChangesAsync the dispatcher hands each
        // to its handler, which fires the background job that evicts that session from the cache. No inline write.
        foreach (var entity in entities)
        {
            entity.Revoke();
        }

        // Persist the aggregate (and dispatch its domain events).
        unitOfWork.UserSessionRepository.UpdateRange(entities);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
