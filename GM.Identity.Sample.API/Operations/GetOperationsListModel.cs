using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Operations;

/// <summary>
/// Get Operations List
/// </summary>
public class GetOperationsListModel : GetBaseListModel
{
    /// <summary>
    /// The Id of the Operation
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// The Name of the Operation
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }

    /// <summary>
    /// The Description of the Operation
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Description")]
    public string? Description { get; set; }
}

/// <inheritdoc />
public class GetOperationsListModelValidator : AbstractValidator<GetOperationsListModel>
{
    /// <inheritdoc />
    public GetOperationsListModelValidator()
    {
    }
}
