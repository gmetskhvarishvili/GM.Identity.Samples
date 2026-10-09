using System;

namespace GM.Identity.Sample.Application.Users;

/// <summary>A user's optional personal details. Used both as input (create/update) and output (details/list).</summary>
public class UserPersonalInfoDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PersonalNumber { get; set; }
    public DateTime? BirthDate { get; set; }
}
