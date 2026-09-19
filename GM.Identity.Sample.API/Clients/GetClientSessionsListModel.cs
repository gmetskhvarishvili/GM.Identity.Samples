using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Clients;

public class GetClientSessionsListModel: GetBaseListModel
{
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid? Id { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "IsRevoked")]
    public bool? IsRevoked { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "IsExpired")]
    public bool? IsExpired  { get; set; }
}

public class GetClientSessionsListModelValidator : AbstractValidator<GetClientSessionsListModel>;
