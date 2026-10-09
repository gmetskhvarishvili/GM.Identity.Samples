using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.UpdateUser;

public class UpdateUserCommand : IRequest
{
    public Guid? Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>Optional personal details, stored 1:1 with the user.</summary>
    public UpdateUserPersonalInfoCommand? PersonalInfo { get; set; }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotNull().NotEmpty();
    }
}

public class UpdateUserCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // Load tracked (asNoTracking: false) and include the 1:1 personal info, so a change to it — or a first
        // one — is persisted by change tracking without a manual Update() reattach.
        var entity = await unitOfWork.UserRepository
            .Query(false, null)
            .Include(x => x.PersonalInfo)
            .FirstOrDefaultAsync(x => x.Id == request.Id
                                      && x.IsActive
                                      && !x.IsDeleted
                                      && !x.IsHidden,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.User,
                StringResource.Id,
                request.Id!);
        }

        if (await unitOfWork.UserRepository.ExistsAsync(
                x => x.Id != entity.Id
                     && x.Email == request.Email
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.User,
                StringResource.Email,
                request.Email!);
        }

        // A changed contact is no longer verified — reset its confirmation so it must be re-confirmed.
        var emailChanged = !string.Equals(entity.Email, request.Email, StringComparison.OrdinalIgnoreCase);
        var phoneChanged = !string.Equals(entity.PhoneNumber, request.PhoneNumber, StringComparison.Ordinal);

        entity.Update(request.Username!, request.Email!, request.PhoneNumber);

        if (emailChanged)
            entity.ResetEmailConfirmation();
        if (phoneChanged)
            entity.ResetPhoneNumberConfirmation();

        if (request.PersonalInfo is not null)
            entity.SetPersonalInfo(
                request.PersonalInfo.FirstName,
                request.PersonalInfo.LastName,
                request.PersonalInfo.PersonalNumber,
                request.PersonalInfo.BirthDate);

        // Entity is tracked; change tracking persists the user and its owned personal-info row.
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
