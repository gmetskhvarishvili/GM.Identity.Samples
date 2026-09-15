using FluentValidation;

using System;

namespace GM.Identity.Sample.API.Accounts;

/// <summary>Body to begin a passkey login.</summary>
public class BeginPasskeyModel
{
    public string UserName { get; set; } = null!;
}

public class BeginPasskeyModelValidator : AbstractValidator<BeginPasskeyModel>
{
    public BeginPasskeyModelValidator() => RuleFor(x => x.UserName).NotNull().NotEmpty();
}

/// <summary>Body to complete a passkey login (WebAuthn assertion, all binary fields base64url).</summary>
public class CompletePasskeyModel
{
    public string UserName { get; set; } = null!;
    public Guid ClientId { get; set; }
    public string CredentialId { get; set; } = null!;
    public string AuthenticatorData { get; set; } = null!;
    public string ClientDataJson { get; set; } = null!;
    public string Signature { get; set; } = null!;
}

public class CompletePasskeyModelValidator : AbstractValidator<CompletePasskeyModel>
{
    public CompletePasskeyModelValidator()
    {
        RuleFor(x => x.UserName).NotNull().NotEmpty();
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.CredentialId).NotNull().NotEmpty();
        RuleFor(x => x.AuthenticatorData).NotNull().NotEmpty();
        RuleFor(x => x.ClientDataJson).NotNull().NotEmpty();
        RuleFor(x => x.Signature).NotNull().NotEmpty();
    }
}
