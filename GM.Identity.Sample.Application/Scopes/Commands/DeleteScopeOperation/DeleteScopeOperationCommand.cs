using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Common.Authorization;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Scopes.Commands.DeleteScopeOperation;

public class DeleteScopeOperationCommand : IRequest
{
    public Guid ScopeId { get; set; }
    public Guid OperationId { get; set; }
}

public class DeleteScopeOperationCommandValidator : AbstractValidator<DeleteScopeOperationCommand>
{
    public DeleteScopeOperationCommandValidator()
    {
        RuleFor(x => x.ScopeId).NotNull().NotEmpty();
        RuleFor(x => x.OperationId).NotNull().NotEmpty();
    }
}

public class DeleteScopeOperationCommandHandler(
    IUnitOfWork unitOfWork,
    IScopeCache scopeCache) : IRequestHandler<DeleteScopeOperationCommand>
{
    public async Task Handle(DeleteScopeOperationCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.ScopeOperationRepository
            .FirstOrDefaultAsync(x => x.ScopeId == request.ScopeId
                                      && x.OperationId == request.OperationId
                                      && x.IsActive
                                      && !x.IsDeleted
                                      && !x.IsHidden,
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.ScopeOperation,
                StringResource.OperationId,
                request.OperationId);
        }

        entity.SoftRemove();

        // Persist the aggregate
        unitOfWork.ScopeOperationRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Write-through: drop the operation from the scope's projected set.
        await scopeCache.RemoveScopeOperationAsync(request.ScopeId, request.OperationId, cancellationToken);
    }
}
