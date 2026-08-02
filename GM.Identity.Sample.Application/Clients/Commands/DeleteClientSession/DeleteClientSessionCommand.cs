using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Clients.Commands.DeleteClientSession;

public class DeleteClientSessionCommand : IRequest
{
    public Guid ClientId { get; set; }
    public Guid Id { get; set; }
}

public class DeleteClientSessionCommandValidator : AbstractValidator<DeleteClientSessionCommand>
{
    public DeleteClientSessionCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.Id).NotNull().NotEmpty();
    }
}

public class DeleteClientSessionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteClientSessionCommand>
{
    public async Task Handle(DeleteClientSessionCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.ClientSessionRepository
            .FirstOrDefaultAsync(x => x.ClientId == request.ClientId
                                      && x.Id == request.Id,
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.ClientSession,
                StringResource.Id,
                request.Id!);
        }
        
        entity.Revoke();

        // Persist the aggregate
        unitOfWork.ClientSessionRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}