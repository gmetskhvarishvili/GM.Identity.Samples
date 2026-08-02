using FluentValidation;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

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

public class DeleteAllUserSessionsCommandHandler(IUnitOfWork unitOfWork)
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
    }
}