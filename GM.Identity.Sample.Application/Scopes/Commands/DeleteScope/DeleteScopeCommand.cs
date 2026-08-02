using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Scopes.Commands.DeleteScope;

public class DeleteScopeCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteScopeCommandValidator : AbstractValidator<DeleteScopeCommand>
{
    public DeleteScopeCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class DeleteScopeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteScopeCommand>
{
    public async Task Handle(DeleteScopeCommand request, CancellationToken cancellationToken)
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

        entity.SoftRemove();

        // Persist the aggregate
        unitOfWork.ScopeRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}