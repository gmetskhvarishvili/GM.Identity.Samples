using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Clients;

public class GetClientsListModel : GetBaseListModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid? Id { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
}

public class GetClientsListModelValidator : AbstractValidator<GetClientsListModel>;
