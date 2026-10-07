using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Clients.Commands.HideClient;

public class HideClientCommand : IRequest
{
    public Guid Id { get; set; }
}

public class HideClientCommandValidator : AbstractValidator<HideClientCommand>
{
    public HideClientCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class HideClientCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<HideClientCommand>
{
    public async Task Handle(HideClientCommand request, CancellationToken cancellationToken)
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

        entity.Hide();

        unitOfWork.ClientRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
