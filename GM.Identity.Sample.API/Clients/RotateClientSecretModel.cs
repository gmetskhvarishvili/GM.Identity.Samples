using FluentValidation;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;

namespace GM.Identity.Sample.API.Clients;

/// <summary>Rotate client secret model.</summary>
public class RotateClientSecretModel
{
    /// <summary>The new secret.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Secret")]
    public string? Secret { get; set; }
}

public class RotateClientSecretModelValidator : AbstractValidator<RotateClientSecretModel>
{
    public RotateClientSecretModelValidator()
    {
        RuleFor(x => x.Secret).NotNull().NotEmpty();
    }
}
