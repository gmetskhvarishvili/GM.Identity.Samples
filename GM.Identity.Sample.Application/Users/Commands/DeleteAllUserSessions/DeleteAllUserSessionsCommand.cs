using FluentValidation;
using GM.Identity.Authorization;
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
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache)
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

        foreach (var entity in entities)
        {
            entity.Revoke();
        }
        
        // Persist the aggregate
        unitOfWork.UserSessionRepository.UpdateRange(entities);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate every cached session immediately (don't wait for their TTLs).
        foreach (var entity in entities)
            await sessionCache.RemoveAsync(entity.TokenHash, cancellationToken);
    }
}
