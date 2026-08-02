using GM.API.Models;

namespace GM.Identity.Sample.Gateway.API.Services.Permissions;

/// <summary>
/// Get Permissions List
/// </summary>
public class GetPermissionsListRequestModel : GetBaseListModel
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