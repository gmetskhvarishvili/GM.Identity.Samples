using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Roles;

/// <summary>Get roles list model.</summary>
public class GetRolesListModel : GetBaseListModel
{
    /// <summary>
    /// The Id of the Role
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// The Name of the Role
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
}

/// <inheritdoc />
public class GetRolesListModelValidator : AbstractValidator<GetRolesListModel>
{
    /// <inheritdoc />
    public GetRolesListModelValidator()
    {
    }
}
