using FluentValidation;
using GM.API.Models;

using System;
namespace GM.Identity.Sample.API.Users;

public class GetUserSessionsListModel: GetBaseListModel
{
    public Guid? ClientId { get; set; }
    public bool? IsRevoked { get; set; }
    public bool? IsExpired  { get; set; }
}

public class GetUserSessionsListModelValidator : AbstractValidator<GetUserSessionsListModel>;
