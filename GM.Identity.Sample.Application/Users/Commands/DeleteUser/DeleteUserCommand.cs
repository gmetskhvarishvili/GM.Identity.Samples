using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Events.Users;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
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

public class DeleteUserCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteUserCommand>
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

        unitOfWork.UserRepository.Update(entity);
        await unitOfWork.UserSessionRepository.RevokeAllForUserAsync(entity.Id, cancellationToken);

        var evt = new UserDeletedIntegrationEvent { UserId = entity.Id };
        await unitOfWork.OutboxMessageRepository.AddAsync(OutboxMessage.From(entity.Id, evt), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
