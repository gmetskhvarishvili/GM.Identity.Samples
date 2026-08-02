using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Users.Commands.CreateUserRole;

public class CreateUserRoleCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class CreateUserRoleCommandValidator : AbstractValidator<CreateUserRoleCommand>
{
    public CreateUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.RoleId).NotNull().NotEmpty();
    }
}

public class CreateUserRoleCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<CreateUserRoleCommand>
{
    public async Task Handle(CreateUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.UserRoleRepository.ExistsAsync(
                x => x.UserId == request.RoleId
                     && x.RoleId == request.RoleId
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.UserRole,
                StringResource.RoleId,
                request.RoleId);
        }
        
        
        // Create the root aggregate
        var entity = UserRole
            .Create(request.UserId, request.RoleId);


        // Persist the aggregate
        await unitOfWork.UserRoleRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}