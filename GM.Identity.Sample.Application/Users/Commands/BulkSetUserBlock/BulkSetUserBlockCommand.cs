using FluentValidation;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.Events.Users;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.BulkSetUserBlock;

/// <summary>
/// Blocks or unblocks many users in one admin operation. Blocking revokes each affected user's sessions and
/// queues a security alert, mirroring the single-user command. Returns how many users were changed.
/// </summary>
public class BulkSetUserBlockCommand : IRequest<int>
{
    public IReadOnlyCollection<Guid> UserIds { get; set; } = new List<Guid>();
    public bool Block { get; set; }
}

public class BulkSetUserBlockCommandValidator : AbstractValidator<BulkSetUserBlockCommand>
{
    public BulkSetUserBlockCommandValidator() =>
        RuleFor(x => x.UserIds).NotNull().NotEmpty().WithMessage("At least one user id is required.");
}

public class BulkSetUserBlockCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache) : IRequestHandler<BulkSetUserBlockCommand, int>
{
    public async Task<int> Handle(BulkSetUserBlockCommand request, CancellationToken cancellationToken)
    {
        var ids = request.UserIds.Distinct().ToList();
        var users = await unitOfWork.UserRepository
            .Query(true, null)
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);
        if (users.Count == 0)
            return 0;

        foreach (var user in users)
        {
            if (request.Block)
            {
                user.Block();
                await unitOfWork.QueueSecurityAlertAsync(
                    user.Id, user.Email, user.PhoneNumber, SecurityAlertTypes.AccountBlocked, cancellationToken);
            }
            else
            {
                user.UnBlock();
            }
        }

        unitOfWork.UserRepository.UpdateRange(users);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Blocking must revoke live sessions immediately.
        if (request.Block)
            foreach (var user in users)
                await unitOfWork.RevokeAllUserSessionsAsync(sessionCache, user.Id, cancellationToken);

        return users.Count;
    }
}
