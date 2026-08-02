using FluentValidation;
using GM.API.Models;

namespace GM.Identity.Sample.Gateway.API.Permissions;

/// <summary>
/// Get Permissions List
/// </summary>
public class GetPermissionsListModel : GetBaseListModel
{
    /// <summary>
    /// The Id of the Permission
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The Name of the Permission
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The Description of the Permission
    /// </summary>
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