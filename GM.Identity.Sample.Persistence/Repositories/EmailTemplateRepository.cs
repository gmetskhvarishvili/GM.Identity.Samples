using GM.EntityFramework.Persistence.Repositories;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.EmailTemplateAggregate;
using GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.EmailTemplateAggregate.Interfaces;
using GM.Identity.Sample.Persistence.Context;

namespace GM.Identity.Sample.Persistence.Repositories;

public class EmailTemplateRepository(ApplicationDbContext context)
    : GenericRepository<EmailTemplate, ApplicationDbContext>(context), IEmailTemplateRepository;
