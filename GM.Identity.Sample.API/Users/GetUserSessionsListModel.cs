using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

public class GetUserSessionsListModel: GetBaseListModel
{
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid? ClientId { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "IsRevoked")]
    public bool? IsRevoked { get; set; }
    [Display(ResourceType = typeof(StringResource), Name = "IsExpired")]
    public bool? IsExpired  { get; set; }
}

public class GetUserSessionsListModelValidator : AbstractValidator<GetUserSessionsListModel>;
