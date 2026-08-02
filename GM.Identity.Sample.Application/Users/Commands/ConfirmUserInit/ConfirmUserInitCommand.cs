using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Enums;
using GM.Identity.Sample.Domain.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using ValidationException = FluentValidation.ValidationException;

namespace GM.Identity.Sample.Application.Users.Commands.ConfirmUserInit;

public class ConfirmUserInitCommand : IRequest
{
    public Guid UserId { get; set; }
    public ConfirmationType ConfirmationType { get; set; }
}

public class ConfirmUserInitCommandValidator : AbstractValidator<ConfirmUserInitCommand>
{
    public ConfirmUserInitCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.ConfirmationType).IsInEnum();
    }
}

public class ConfirmUserInitCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmUserInitCommand>
{
    public async Task Handle(ConfirmUserInitCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.UserId
                                      && x.IsActive
                                      && !x.IsDeleted
                                      && !x.IsHidden,
                false,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.User,
                StringResource.Id,
                request.UserId);
        }

        // A contact that is already confirmed cannot be confirmed again.
        var alreadyConfirmed = request.ConfirmationType.ConfirmsPhoneNumber()
            ? entity.PhoneNumberConfirmed
            : entity.EmailConfirmed;

        if (alreadyConfirmed)
        {
            throw new ValidationException(
                $"The user's contact is already confirmed for {request.ConfirmationType} confirmation.");
        }

        // The contact being confirmed: the email address for Email, the phone number for SMS/WhatsApp.
        // GM.OTP generates and later validates the code against this same payload.
        var contact = request.ConfirmationType.ConfirmsPhoneNumber()
            ? entity.PhoneNumber
            : entity.Email;

        if (string.IsNullOrWhiteSpace(contact))
        {
            throw new ValidationException(
                $"The user has no contact set for {request.ConfirmationType} confirmation.");
        }

        var evt = new UserConfirmationInitiatedIntegrationEvent(
            contact,
            (int)request.ConfirmationType)
        {
            UserId = entity.Id
        };

        await unitOfWork.OutboxMessageRepository.AddAsync(OutboxMessage.From(entity.Id, evt), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
