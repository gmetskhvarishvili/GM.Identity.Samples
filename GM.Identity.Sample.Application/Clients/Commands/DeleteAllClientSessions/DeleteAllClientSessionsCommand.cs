using FluentValidation;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace GM.Identity.Sample.Application.Clients.Commands.DeleteAllClientSessions;

public class DeleteAllClientSessionsCommand : IRequest
{
    public Guid ClientId { get; set; }
}

public class DeleteAllClientSessionsCommandValidator : AbstractValidator<DeleteAllClientSessionsCommand>
{
    public DeleteAllClientSessionsCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
    }
}

public class DeleteAllClientSessionsCommandHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAllClientSessionsCommand>
{
    public async Task Handle(DeleteAllClientSessionsCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entities = await unitOfWork.ClientSessionRepository
            .Query(true,
                null)
            .Where(x => x.ClientId == request.ClientId)
            .ToListAsync(cancellationToken);

        // Each Revoke() raises GMClientSessionRevokedDomainEvent; after SaveChangesAsync the dispatcher hands each
        // to its handler, which fires the background job that evicts that session from the cache. No inline write.
        foreach (var entity in entities)
        {
            entity.Revoke();
        }

        // Persist the aggregate (and dispatch its domain events).
        unitOfWork.ClientSessionRepository.UpdateRange(entities);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
