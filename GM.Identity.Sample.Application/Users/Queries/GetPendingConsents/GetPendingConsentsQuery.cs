using FluentValidation;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Queries.GetPendingConsents;

/// <summary>Returns the mandatory consent documents the user must still accept (never accepted, or an older version).</summary>
public class GetPendingConsentsQuery : IRequest<IReadOnlyList<PendingConsentDto>>
{
    public Guid UserId { get; set; }
}

public class GetPendingConsentsQueryValidator : AbstractValidator<GetPendingConsentsQuery>
{
    public GetPendingConsentsQueryValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class GetPendingConsentsQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPendingConsentsQuery, IReadOnlyList<PendingConsentDto>>
{
    public async Task<IReadOnlyList<PendingConsentDto>> Handle(
        GetPendingConsentsQuery request, CancellationToken cancellationToken)
    {
        var pending = await unitOfWork.GetPendingAsync(request.UserId, cancellationToken);

        return pending
            .Select(d => new PendingConsentDto
            {
                ConsentType = d.ConsentType,
                Title = d.Title,
                Content = d.Content,
                Version = d.Version,
            })
            .ToList();
    }
}

public class PendingConsentDto
{
    public string ConsentType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string Version { get; set; } = null!;
}
