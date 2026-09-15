using FluentValidation;

namespace GM.Identity.Sample.API.Users;

/// <summary>Body to record acceptance of a consent document.</summary>
public class RecordConsentModel
{
    public string ConsentType { get; set; } = null!;
    public string DocumentVersion { get; set; } = null!;
}

public class RecordConsentModelValidator : AbstractValidator<RecordConsentModel>
{
    public RecordConsentModelValidator()
    {
        RuleFor(x => x.ConsentType).NotNull().NotEmpty();
        RuleFor(x => x.DocumentVersion).NotNull().NotEmpty();
    }
}
