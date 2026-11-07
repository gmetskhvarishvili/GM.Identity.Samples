using FluentValidation;
using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;

namespace GM.Identity.Sample.API.Users;

/// <summary>Update user model.</summary>
public class UpdateUserModel
{
    /// <summary>The username.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string? Username { get; set; }
    /// <summary>The email.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string? Email { get; set; }
    /// <summary>The phone number.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "PhoneNumber")]
    public string? PhoneNumber { get; set; }

    /// <summary>Optional personal details, stored 1:1 with the user.</summary>
    public UpdateUserPersonalInfoModel? PersonalInfo { get; set; }

    /// <summary>
    /// File id of the user's profile image in file storage (a GM.FileStorage file id). Omit to keep it; send
    /// <c>00000000-0000-0000-0000-000000000000</c> to clear it.
    /// </summary>
    public Guid? ProfileImageFileId { get; set; }
}

public class UpdateUserModelValidator : AbstractValidator<UpdateUserModel>
{
    public UpdateUserModelValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotNull().NotEmpty();
    }
}