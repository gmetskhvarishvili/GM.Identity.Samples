using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Users.Commands.CreateUserRole;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Identity.Sample.Domain.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Users.Commands.CreateUser;

public class CreateUserCommand : IRequest<string>
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Password { get; set; }

    public IEnumerable<CreateUserRoleCommand>? UserRoles { get; set; }
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotNull().NotEmpty();
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}

public class CreateUserCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<CreateUserCommand, string>
{
    public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
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

        return entity.Id.ToString();
    }
}