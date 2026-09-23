using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common;
using GM.Identity;
using GM.Identity.Sample.Application.Users.Commands.CreateUserRole;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
namespace GM.Identity.Sample.Application.Users.Commands.CreateUser;

public class CreateUserCommand : IRequest<Guid>
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Password { get; set; }

    public IEnumerable<CreateUserRoleCommand>? UserRoles { get; set; }

    /// <summary>Ids of the 2FA methods to enrol the user in (see the TwoFactorAuthType reference data).</summary>
    public IEnumerable<int>? TwoFactorAuthTypeIds { get; set; }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IOptions<PasswordPolicyOptions> passwordPolicy)
    {
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotNull().NotEmpty();
        RuleFor(x => x.Password).StrongPassword(passwordPolicy.Value);
    }
}

public class CreateUserCommandHandler(
    IUnitOfWork unitOfWork,
    IBreachedPasswordChecker breachedPasswordChecker) : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await PasswordPolicy.EnsureNotBreachedAsync(request.Password, breachedPasswordChecker, cancellationToken);

        if (await unitOfWork.UserRepository.ExistsAsync(
                x => x.Email == request.Email
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

        // Create the root aggregate
        var entity = User
            .Create(request.Username!, request.Email, request.PhoneNumber);

        var (hash, salt) = PasswordHasher
            .Hash(request.Password!);

        entity.UpdatePassword(hash, salt);

        // Add child items if any
        if (request.UserRoles?.Any() == true)
        {
            var items = request.UserRoles
                .Select(item =>
                    UserRole.Create(entity.Id, item.RoleId))
                .ToArray();

            entity.AddRoles(items);
        }

        // Enrol the user in any requested 2FA methods (same child-collection pattern as roles). Login then
        // references these: a user with an enrolled method must clear a second-factor challenge.
        if (request.TwoFactorAuthTypeIds?.Any() == true)
        {
            var twoFactorItems = request.TwoFactorAuthTypeIds
                .Select(typeId => UserTwoFactorAuthType.Create(entity.Id, typeId))
                .ToArray();

            entity.AddTwoFactorAuthType(twoFactorItems);
        }

        // Persist the aggregate
        await unitOfWork.UserRepository.AddAsync(entity, cancellationToken);
        
        var evt = new UserRegisteredIntegrationEvent(
            request.Email,
            request.Username,
            request.PhoneNumber)
        {
            UserId = entity.Id
        };
        
        await unitOfWork.OutboxMessageRepository.AddAsync(OutboxMessage.From(entity.Id, evt), cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
