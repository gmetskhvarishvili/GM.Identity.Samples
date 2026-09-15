using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.EmailTemplates.Queries.GetEmailTemplates;

/// <summary>Lists all managed email templates.</summary>
public class GetEmailTemplatesQuery : IRequest<IReadOnlyList<EmailTemplateDto>>;

public class GetEmailTemplatesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplateDto>>
{
    public async Task<IReadOnlyList<EmailTemplateDto>> Handle(GetEmailTemplatesQuery request, CancellationToken cancellationToken)
    {
        return await unitOfWork.EmailTemplateRepository
            .Query(false, null)
            .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden)
            .OrderBy(x => x.Key)
            .Select(x => new EmailTemplateDto
            {
                Id = x.Id,
                Key = x.Key,
                Subject = x.Subject,
                Body = x.Body,
            })
            .ToListAsync(cancellationToken);
    }
}

public class EmailTemplateDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}
