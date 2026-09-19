using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Body to register a passkey: the credential id, its ES256 public key (SPKI, base64), and a label.</summary>
public class RegisterPasskeyModel
{
    /// <summary>The credential id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "CredentialId")]
    public string CredentialId { get; set; } = null!;
    /// <summary>The public key spki base64.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "PublicKeySpkiBase64")]
    public string PublicKeySpkiBase64 { get; set; } = null!;
    /// <summary>The name.</summary>
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
