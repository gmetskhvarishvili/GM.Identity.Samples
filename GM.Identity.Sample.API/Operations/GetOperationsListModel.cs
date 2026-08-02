using FluentValidation;
using GM.API.Models;

namespace GM.Identity.Sample.API.Operations;

/// <summary>
/// Get Operations List
/// </summary>
public class GetOperationsListModel : GetBaseListModel
{
    /// <summary>
    /// The Id of the Operation
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// The Name of the Operation
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The Description of the Operation
    /// </summary>
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