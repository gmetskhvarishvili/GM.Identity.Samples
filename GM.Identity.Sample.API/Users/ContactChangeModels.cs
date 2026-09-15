using FluentValidation;
using GM.Identity.Sample.Domain.Enums;

namespace GM.Identity.Sample.API.Users;

/// <summary>Request to change the current user's email or phone (starts verify-before-apply).</summary>
public class RequestContactChangeModel
{
    public ConfirmationType ConfirmationType { get; set; }
    public string NewContact { get; set; } = null!;
}

public class RequestContactChangeModelValidator : AbstractValidator<RequestContactChangeModel>
{
    public RequestContactChangeModelValidator()
    {
        RuleFor(x => x.ConfirmationType).IsInEnum();
        RuleFor(x => x.NewContact).NotNull().NotEmpty();
    }
}

/// <summary>The one-time code sent to the pending new contact.</summary>
public class ConfirmContactChangeModel
{
    public string Code { get; set; } = null!;
}

public class ConfirmContactChangeModelValidator : AbstractValidator<ConfirmContactChangeModel>
{
    public ConfirmContactChangeModelValidator() => RuleFor(x => x.Code).NotNull().NotEmpty();
}
