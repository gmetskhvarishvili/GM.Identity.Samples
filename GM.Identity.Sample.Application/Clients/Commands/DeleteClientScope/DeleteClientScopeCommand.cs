using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Clients.Commands.DeleteClientScope;

public class DeleteClientScopeCommand : IRequest
{
    public Guid ClientId { get; set; }
    public Guid ScopeId { get; set; }
}

public class DeleteClientScopeCommandValidator : AbstractValidator<DeleteClientScopeCommand>
{
    public DeleteClientScopeCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ScopeId).NotNull().NotEmpty();
    }
}

public class DeleteClientScopeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteClientScopeCommand>
{
    public async Task Handle(DeleteClientScopeCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.ClientScopeRepository
            .FirstOrDefaultAsync(x => x.ClientId == request.ClientId
                                      && x.ScopeId == request.ScopeId,
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.ClientScope,
                StringResource.ScopeId,
                request.ScopeId!);
        }

        // Persist the aggregate
        unitOfWork.ClientScopeRepository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}