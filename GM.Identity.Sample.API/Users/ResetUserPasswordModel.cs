using FluentValidation;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Reset user password model.</summary>
public class ResetUserPasswordModel
{
    /// <summary>The email.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string Email { get; set; } = null!;
    /// <summary>The notification type.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "NotificationType")]
    public int NotificationType { get; set; }
}

public class ResetUserPasswordModelValidator : AbstractValidator<ResetUserPasswordModel>
{
    public ResetUserPasswordModelValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty();
        RuleFor(x => x.NotificationType).NotNull();
    }
}