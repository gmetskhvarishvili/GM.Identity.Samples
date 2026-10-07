using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Clients.Commands.UnhideClient;

public class UnhideClientCommand : IRequest
{
    public Guid Id { get; set; }
}

public class UnhideClientCommandValidator : AbstractValidator<UnhideClientCommand>
{
    public UnhideClientCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class UnhideClientCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UnhideClientCommand>
{
    public async Task Handle(UnhideClientCommand request, CancellationToken cancellationToken)
    {
        // Fetch by id via the predicate path (no visibility filter), so the entity is found whatever its state.
        var entity = await unitOfWork.ClientRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id, true, null, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.Client,
                StringResource.Id,
                request.Id);
        }

        entity.Unhide();

        unitOfWork.ClientRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
