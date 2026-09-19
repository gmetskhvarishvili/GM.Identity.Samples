using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

public class GetUsersListModel : GetBaseListModel
{
    /// <summary>
    /// The Id of the User
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// The Email of the User
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "Email")]
    public string? Email { get; set; }
    
    /// <summary>
    /// The Username of the User
    /// </summary>
    [Display(ResourceType = typeof(StringResource), Name = "UserName")]
    public string? Username { get; set; }
}

/// <inheritdoc />
public class GetUsersListModelValidator : AbstractValidator<GetUsersListModel>
{
    /// <inheritdoc />
    public GetUsersListModelValidator()
    {
    }
}
