using System;

namespace GM.Identity.Sample.Application.Users.Commands.CreateUser;

/// <summary>Personal details supplied when creating a user; stored 1:1 with the user.</summary>
public class CreateUserPersonalInfoCommand
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PersonalNumber { get; set; }
    public DateTime? BirthDate { get; set; }
}
