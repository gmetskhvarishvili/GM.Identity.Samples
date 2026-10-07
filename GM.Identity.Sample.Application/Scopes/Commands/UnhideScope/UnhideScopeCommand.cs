using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Scopes.Commands.UnhideScope;

public class UnhideScopeCommand : IRequest
{
    public Guid Id { get; set; }
}

public class UnhideScopeCommandValidator : AbstractValidator<UnhideScopeCommand>
{
    public UnhideScopeCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class UnhideScopeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UnhideScopeCommand>
{
    public async Task Handle(UnhideScopeCommand request, CancellationToken cancellationToken)
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

        entity.Unhide();

        unitOfWork.ScopeRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
