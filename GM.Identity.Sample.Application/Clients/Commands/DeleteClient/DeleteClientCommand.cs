using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Clients.Commands.DeleteClient;

public class DeleteClientCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteClientCommandValidator : AbstractValidator<DeleteClientCommand>
{
    public DeleteClientCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class DeleteClientCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteClientCommand>
{
    public async Task Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.ClientRepository
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
                StringResource.Client,
                StringResource.Id,
                request.Id);
        }

        entity.SoftRemove();

        // Persist the aggregate
        unitOfWork.ClientRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}