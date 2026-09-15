using FluentValidation;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.EmailTemplateAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.EmailTemplates.Commands.SaveEmailTemplate;

/// <summary>Creates or updates the email template with the given key (upsert). Returns its id.</summary>
public class SaveEmailTemplateCommand : IRequest<Guid>
{
    public string Key { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}

public class SaveEmailTemplateCommandValidator : AbstractValidator<SaveEmailTemplateCommand>
{
    public SaveEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Key).NotNull().NotEmpty();
        RuleFor(x => x.Subject).NotNull().NotEmpty();
        RuleFor(x => x.Body).NotNull().NotEmpty();
    }
}

public class SaveEmailTemplateCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SaveEmailTemplateCommand, Guid>
{
    public async Task<Guid> Handle(SaveEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.EmailTemplateRepository
            .FirstOrDefaultAsync(x => x.Key == request.Key && x.IsActive && !x.IsDeleted && !x.IsHidden,
                true, null, cancellationToken);

        if (existing != null)
        {
            existing.Update(request.Subject, request.Body);
            unitOfWork.EmailTemplateRepository.Update(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var template = EmailTemplate.Create(request.Key, request.Subject, request.Body);
        await unitOfWork.EmailTemplateRepository.AddAsync(template, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return template.Id;
    }
}
