using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Permissions;

/// <summary>
/// Get Permissions List
/// </summary>
public class GetPermissionsListModel : GetBaseListModel
{
    /// <summary>
    /// The Id of the Permission
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// The Name of the Permission
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }

    /// <summary>
    /// The Description of the Permission
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Description")]
    public string? Description { get; set; }
}

/// <inheritdoc />
public class GetPermissionsListModelValidator : AbstractValidator<GetPermissionsListModel>
{
    /// <inheritdoc />
    public GetPermissionsListModelValidator()
    {
    }
}
