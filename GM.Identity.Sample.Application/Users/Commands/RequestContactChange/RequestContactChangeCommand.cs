using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserPendingContactChangeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Enums;
using GM.Identity.Sample.Domain.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.RequestContactChange;

/// <summary>
/// Starts a verify-before-apply change of the user's email or phone: records the new contact as pending and
/// sends a one-time code to it. The change is only written onto the user when that code is confirmed, so a
/// user can't set a contact they don't control. Supersedes any prior pending change of the same kind.
/// </summary>
public class RequestContactChangeCommand : IRequest
{
    public Guid UserId { get; set; }
    public ConfirmationType ConfirmationType { get; set; }
    public string NewContact { get; set; } = null!;
}

public class RequestContactChangeCommandValidator : AbstractValidator<RequestContactChangeCommand>
{
    public RequestContactChangeCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.ConfirmationType).IsInEnum();
        RuleFor(x => x.NewContact).NotNull().NotEmpty();
        RuleFor(x => x.NewContact).EmailAddress()
            .When(x => x.ConfirmationType == ConfirmationType.Email)
            .WithMessage("A valid email address is required for an email change.");
    }
}

public class RequestContactChangeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<RequestContactChangeCommand>
{
    public async Task Handle(RequestContactChangeCommand request, CancellationToken cancellationToken)
    {
        var userExists = await unitOfWork.UserRepository.ExistsAsync(
            x => x.Id == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (!userExists)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        // A contact must be unique across active users.
        var taken = request.ConfirmationType.ConfirmsPhoneNumber()
            ? await unitOfWork.UserRepository.ExistsAsync(
                x => x.PhoneNumber == request.NewContact && x.Id != request.UserId && x.IsActive && !x.IsDeleted, cancellationToken)
            : await unitOfWork.UserRepository.ExistsAsync(
                x => x.Email == request.NewContact && x.Id != request.UserId && x.IsActive && !x.IsDeleted, cancellationToken);
        if (taken)
            throw new AlreadyExistsException(StringResource.User,
                request.ConfirmationType.ConfirmsPhoneNumber() ? StringResource.User : StringResource.Email, request.NewContact);

        // Supersede any prior pending change of the same kind.
        var priors = await unitOfWork.UserPendingContactChangeRepository
            .Query(true, null)
            .Where(x => x.UserId == request.UserId && x.ConfirmationType == request.ConfirmationType
                        && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .ToListAsync(cancellationToken);
        if (priors.Count > 0)
            unitOfWork.UserPendingContactChangeRepository.RemoveRange(priors);

        await unitOfWork.UserPendingContactChangeRepository.AddAsync(
            UserPendingContactChange.Create(request.UserId, request.ConfirmationType, request.NewContact), cancellationToken);

        // Send the code to the NEW contact (same event/purpose as initial contact confirmation).
        await unitOfWork.OutboxMessageRepository.AddAsync(
            OutboxMessage.From(request.UserId,
                new UserConfirmationInitiatedIntegrationEvent(request.NewContact, (int)request.ConfirmationType) { UserId = request.UserId }),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
