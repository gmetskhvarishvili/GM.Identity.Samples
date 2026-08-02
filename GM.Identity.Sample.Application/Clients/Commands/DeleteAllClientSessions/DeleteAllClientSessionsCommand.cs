using FluentValidation;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

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

public class DeleteAllClientSessionsCommandHandler(IUnitOfWork unitOfWork)
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
    }
}