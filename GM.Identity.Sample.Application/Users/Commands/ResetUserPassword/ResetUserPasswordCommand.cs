using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Events.Users;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Users.Commands.ResetUserPassword;

public class ResetUserPasswordCommand : IRequest
{
    public string Email { get; set; } = null!;
    public int NotificationType { get; set; }
}

public class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty();
        RuleFor(x => x.NotificationType).NotNull();
    }
}

public class ResetUserPasswordCommandHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<ResetUserPasswordCommand>
{
    public async Task Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Email == request.Email
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
                StringResource.Email,
                request.Email);
        }

        // Hand off to the downstream notifier (via the outbox): it generates and sends a reset code/link to the
        // user's contact. The reset is completed later by RecoverUserPassword with the code.
        var evt = new PasswordResetRequestedIntegrationEvent(entity.Email!, request.NotificationType)
        {
            UserId = entity.Id
        };

        await unitOfWork.OutboxMessageRepository.AddAsync(OutboxMessage.From(entity.Id, evt), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
