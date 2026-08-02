using FluentValidation;
using GM.API.Models;

namespace GM.Identity.Sample.API.Clients;

public class GetClientSessionsListModel: GetBaseListModel
{
    public Guid? Id { get; set; }
    public bool? IsRevoked { get; set; }
    public bool? IsExpired  { get; set; }
}

public class GetClientSessionsListModelValidator : AbstractValidator<GetClientSessionsListModel>;