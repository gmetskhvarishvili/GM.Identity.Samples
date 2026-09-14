using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserRecoveryCodeAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Commands.GenerateRecoveryCodes;

/// <summary>
/// (Re)generates a user's set of single-use backup codes. Any existing codes are invalidated and replaced.
/// Returns the fresh plaintext codes exactly once — only their hashes are stored, so they are never
/// recoverable afterwards. A user can present one of these in place of their normal second factor.
/// </summary>
public class GenerateRecoveryCodesCommand : IRequest<IReadOnlyList<string>>
{
    public Guid UserId { get; set; }

    /// <summary>How many codes to generate. Defaults to 10.</summary>
    public int Count { get; set; } = 10;
}

public class GenerateRecoveryCodesCommandValidator : AbstractValidator<GenerateRecoveryCodesCommand>
{
    public GenerateRecoveryCodesCommandValidator()
    {
        RuleFor(x => x.UserId).NotNull().NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 20);
    }
}

public class GenerateRecoveryCodesCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<GenerateRecoveryCodesCommand, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(
        GenerateRecoveryCodesCommand request, CancellationToken cancellationToken)
    {
        var userExists = await unitOfWork.UserRepository.ExistsAsync(
            x => x.Id == request.UserId && !x.IsDeleted, cancellationToken);
        if (!userExists)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        // Invalidate the old set — regenerating replaces every prior code.
        var existing = await unitOfWork.UserRecoveryCodeRepository
            .Query(true, null)
            .Where(x => x.UserId == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
            unitOfWork.UserRecoveryCodeRepository.RemoveRange(existing);

        var codes = RecoveryCodeGenerator.Generate(request.Count);
        foreach (var code in codes)
        {
            await unitOfWork.UserRecoveryCodeRepository.AddAsync(
                UserRecoveryCode.Create(request.UserId, TokenGenerator.Hash(code)), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Returned once, in plaintext — the caller must surface these to the user now.
        return codes;
    }
}
