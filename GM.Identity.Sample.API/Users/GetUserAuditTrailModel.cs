using FluentValidation;
using GM.API.Models;

namespace GM.Identity.Sample.API.Users;

/// <summary>Paging inputs for a user's audit-trail (domain-event history) query.</summary>
public class GetUserAuditTrailModel : GetBaseListModel;

public class GetUserAuditTrailModelValidator : AbstractValidator<GetUserAuditTrailModel>;
