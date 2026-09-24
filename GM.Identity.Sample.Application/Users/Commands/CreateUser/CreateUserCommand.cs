using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common;
using GM.Identity;
using GM.Identity.Sample.Application.Users.Commands.CreateUserRole;
using GM.Identity.Sample.Application.Users.Commands.RecordUserConsent;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserConsentAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserTwoFactorAuthTypeAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Application.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using ValidationException = GM.Exceptions.ValidationException;

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

    /// <summary>Consent documents the user accepts at registration; each is recorded as an audit row.</summary>
    public IEnumerable<RecordUserConsentCommand>? Consents { get; set; }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(
        IPasswordPolicyOptions passwordPolicy, IBreachedPasswordChecker breachedPasswordChecker)
    {
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotNull().NotEmpty();
        RuleFor(x => x.Password).StrongPassword(passwordPolicy);
        RuleFor(x => x.Password).NotBreached(breachedPasswordChecker);

        RuleForEach(x => x.Consents).ChildRules(consent =>
        {
            consent.RuleFor(c => c.ConsentType).NotNull().NotEmpty();
            consent.RuleFor(c => c.DocumentVersion).NotNull().NotEmpty();
        });
    }
}

public class CreateUserCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {

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

        // Record any consents the user accepted at registration. Each is validated against the current document
        // version (same rule as RecordUserConsent) so we never store acceptance of an unknown or stale version.
        if (request.Consents?.Any() == true)
        {
            foreach (var consent in request.Consents)
            {
                var current = await unitOfWork.ConsentDocumentRepository.FirstOrDefaultAsync(
                    x => x.ConsentType == consent.ConsentType
                         && x.IsCurrent && x.IsActive && !x.IsDeleted && !x.IsHidden,
                    false, null, cancellationToken);

                if (current == null)
                    throw new NotFoundException(
                        StringResource.ConsentDocument, StringResource.ConsentType, consent.ConsentType);

                if (current.Version != consent.DocumentVersion)
                    throw new ValidationException(
                        $"Consent '{consent.ConsentType}' must be accepted at the current version '{current.Version}'.");

                await unitOfWork.UserConsentRepository.AddAsync(
                    UserConsent.Create(entity.Id, consent.ConsentType, consent.DocumentVersion), cancellationToken);
            }
        }

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
