using FluentValidation;
using GM.API.Models;

namespace GM.Identity.Sample.API.Clients;

public class GetClientsListModel : GetBaseListModel
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
}

public class GetClientsListModelValidator : AbstractValidator<GetClientsListModel>;