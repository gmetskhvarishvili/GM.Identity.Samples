using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.MessagingBoundedContext.EmailTemplateAggregate.Interfaces;

public interface IEmailTemplateRepository : IGenericRepository<EmailTemplate>;
