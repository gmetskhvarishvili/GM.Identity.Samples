using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Scopes.Commands.HideScope;

public class HideScopeCommand : IRequest
{
    public Guid Id { get; set; }
}

public class HideScopeCommandValidator : AbstractValidator<HideScopeCommand>
{
    public HideScopeCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class HideScopeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<HideScopeCommand>
{
    public async Task Handle(HideScopeCommand request, CancellationToken cancellationToken)
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

        entity.Hide();

        unitOfWork.ScopeRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
