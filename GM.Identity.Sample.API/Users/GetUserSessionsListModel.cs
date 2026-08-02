using FluentValidation;
using GM.API.Models;

namespace GM.Identity.Sample.API.Users;

public class GetUserSessionsListModel: GetBaseListModel
{
    public Guid? Id { get; set; }
    public Guid? ClientId { get; set; }
    public bool? IsRevoked { get; set; }
    public bool? IsExpired  { get; set; }
}

public class GetUserSessionsListModelValidator : AbstractValidator<GetUserSessionsListModel>;