using FluentValidation;
using GM.API.Models;

namespace GM.Identity.Sample.API.Scopes;

public class GetScopesListModel : GetBaseListModel
{
    /// <summary>
    /// The Id of the Scope
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// The Name of the Scope
    /// </summary>
    public string? Name { get; set; }
}

/// <inheritdoc />
public class GetScopesListModelValidator : AbstractValidator<GetScopesListModel>
{
    /// <inheritdoc />
    public GetScopesListModelValidator()
    {
    }
}