using FluentValidation;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Scopes;

/// <summary>Create scope operation model.</summary>
public class CreateScopeOperationModel
{
    /// <summary>The operation id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "OperationId")]
    public Guid OperationId { get; set; }
}

public class CreateScopeOperationModelValidator : AbstractValidator<CreateScopeOperationModel>
{
    public CreateScopeOperationModelValidator()
    {
        RuleFor(x => x.OperationId).NotNull().NotEmpty();
    }
}
