using FluentValidation;
using GM.API.Models;

namespace GM.Identity.Sample.API.Users;

public class GetUsersListModel : GetBaseListModel
{
    /// <summary>
    /// The Id of the User
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// The Email of the User
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// The Username of the User
    /// </summary>
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