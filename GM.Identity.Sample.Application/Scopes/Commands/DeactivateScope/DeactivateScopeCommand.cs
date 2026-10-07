using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Scopes.Commands.DeactivateScope;

public class DeactivateScopeCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeactivateScopeCommandValidator : AbstractValidator<DeactivateScopeCommand>
{
    public DeactivateScopeCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class DeactivateScopeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeactivateScopeCommand>
{
    public async Task Handle(DeactivateScopeCommand request, CancellationToken cancellationToken)
    {
        // Fetch by id via the predicate path (no visibility filter), so the entity is found whatever its state.
        var entity = await unitOfWork.ScopeRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id, true, null, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.Scope,
                StringResource.Id,
                request.Id);
        }

        entity.Deactivate();

        unitOfWork.ScopeRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
