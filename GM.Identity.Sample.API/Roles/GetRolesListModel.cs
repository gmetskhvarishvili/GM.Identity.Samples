using FluentValidation;
using GM.API.Models;

namespace GM.Identity.Sample.API.Roles;

public class GetRolesListModel : GetBaseListModel
{
    /// <summary>
    /// The Id of the Role
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// The Name of the Role
    /// </summary>
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