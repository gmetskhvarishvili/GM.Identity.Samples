using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Clients;

/// <summary>Get client sessions list model.</summary>
public class GetClientSessionsListModel: GetBaseListModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid? Id { get; set; }
    /// <summary>The is revoked.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsRevoked")]
    public bool? IsRevoked { get; set; }
    /// <summary>The is expired.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "IsExpired")]
    public bool? IsExpired  { get; set; }
}

public class GetClientSessionsListModelValidator : AbstractValidator<GetClientSessionsListModel>;
