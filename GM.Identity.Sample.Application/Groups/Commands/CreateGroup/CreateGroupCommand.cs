using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.GroupAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Groups.Commands.CreateGroup;

/// <summary>Creates a group (team) that roles can be attached to and users placed in.</summary>
public class CreateGroupCommand : IRequest<Guid>
{
    public string Name { get; set; } = null!;
}

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator() => RuleFor(x => x.Name).NotNull().NotEmpty();
}

public class CreateGroupCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateGroupCommand, Guid>
{
    public async Task<Guid> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.GroupRepository.ExistsAsync(
                x => x.Name == request.Name && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken))
        {
            throw new AlreadyExistsException(StringResource.Name, StringResource.Name, request.Name);
        }

        var group = Group.Create(request.Name);
        await unitOfWork.GroupRepository.AddAsync(group, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return group.Id;
    }
}
