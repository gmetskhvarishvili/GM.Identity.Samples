using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Scopes.Commands.ActivateScope;

public class ActivateScopeCommand : IRequest
{
    public Guid Id { get; set; }
}

public class ActivateScopeCommandValidator : AbstractValidator<ActivateScopeCommand>
{
    public ActivateScopeCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class ActivateScopeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ActivateScopeCommand>
{
    public async Task Handle(ActivateScopeCommand request, CancellationToken cancellationToken)
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

        entity.Activate();

        unitOfWork.ScopeRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
