using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Scopes;

/// <summary>Get scopes list model.</summary>
public class GetScopesListModel : GetBaseListModel
{
    /// <summary>
    /// The Id of the Scope
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// The Name of the Scope
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
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
