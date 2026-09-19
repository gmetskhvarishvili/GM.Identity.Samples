using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Get user sessions list model.</summary>
public class GetUserSessionsListModel: GetBaseListModel
{
    /// <summary>The client id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "ClientId")]
    public Guid? ClientId { get; set; }
    /// <summary>The is revoked.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsRevoked")]
    public bool? IsRevoked { get; set; }
    /// <summary>The is expired.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsExpired")]
    public bool? IsExpired  { get; set; }
}

public class GetUserSessionsListModelValidator : AbstractValidator<GetUserSessionsListModel>;
