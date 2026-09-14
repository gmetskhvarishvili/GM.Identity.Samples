using FluentValidation;
using GM.Identity.Sample.Application.Common.Authorization;
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
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache)
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

        foreach (var entity in entities)
        {
            entity.Revoke();
        }
        
        // Persist the aggregate
        unitOfWork.ClientSessionRepository.UpdateRange(entities);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate every cached session immediately (don't wait for their TTLs).
        foreach (var entity in entities)
            await sessionCache.RemoveAsync(entity.TokenHash, cancellationToken);
    }
}
