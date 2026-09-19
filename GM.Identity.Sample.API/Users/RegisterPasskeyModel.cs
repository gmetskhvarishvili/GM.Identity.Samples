using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Body to register a passkey: the credential id, its ES256 public key (SPKI, base64), and a label.</summary>
public class RegisterPasskeyModel
{
    [Display(ResourceType = typeof(StringResource), Name = "CredentialId")]
    public string CredentialId { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "PublicKeySpkiBase64")]
    public string PublicKeySpkiBase64 { get; set; } = null!;
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string Name { get; set; } = null!;
}

public class RegisterPasskeyModelValidator : AbstractValidator<RegisterPasskeyModel>
{
    public RegisterPasskeyModelValidator()
    {
        RuleFor(x => x.CredentialId).NotNull().NotEmpty();
        RuleFor(x => x.PublicKeySpkiBase64).NotNull().NotEmpty();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}
