using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Scopes.Commands.UpdateScope;

public class UpdateScopeCommand : IRequest
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
}

public class UpdateScopeCommandValidator : AbstractValidator<UpdateScopeCommand>
{
    public UpdateScopeCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}

public class UpdateScopeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateScopeCommand>
{
    public async Task Handle(UpdateScopeCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.ScopeRepository
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
                StringResource.Scope,
                StringResource.Id,
                request.Id!);
        }

        if (await unitOfWork.ScopeRepository.ExistsAsync(
                x => x.Id != entity.Id
                     && x.Name == request.Name
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.Scope,
                StringResource.Name,
                request.Name!);
        }

        entity.Update(request.Name!);

        // Persist the aggregate
        unitOfWork.ScopeRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}