using FluentValidation;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Scopes;

public class CreateScopeOperationModel
{
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
