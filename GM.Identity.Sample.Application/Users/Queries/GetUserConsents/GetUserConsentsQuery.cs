using FluentValidation;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Queries.GetUserConsents;

/// <summary>Returns a user's recorded consent acceptances, newest first.</summary>
public class GetUserConsentsQuery : IRequest<IReadOnlyList<UserConsentDto>>
{
    public Guid UserId { get; set; }
}

public class GetUserConsentsQueryValidator : AbstractValidator<GetUserConsentsQuery>
{
    public GetUserConsentsQueryValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class GetUserConsentsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserConsentsQuery, IReadOnlyList<UserConsentDto>>
{
    public async Task<IReadOnlyList<UserConsentDto>> Handle(GetUserConsentsQuery request, CancellationToken cancellationToken)
    {
        return await unitOfWork.UserConsentRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .Where(x => x.UserId == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .OrderByDescending(x => x.AcceptedAt)
            .Select(x => new UserConsentDto
            {
                ConsentType = x.ConsentType,
                DocumentVersion = x.DocumentVersion,
                AcceptedAt = x.AcceptedAt,
            })
            .ToListAsync(cancellationToken);
    }
}

public class UserConsentDto
{
    public string ConsentType { get; set; } = null!;
    public string DocumentVersion { get; set; } = null!;
    public DateTime AcceptedAt { get; set; }
}
