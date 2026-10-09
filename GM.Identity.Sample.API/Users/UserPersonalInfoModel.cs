using System;

namespace GM.Identity.Sample.API.Users;

/// <summary>A user's optional personal details, carried 1:1 with the user.</summary>
public class UserPersonalInfoModel
{
    /// <summary>Given name.</summary>
    public string? FirstName { get; set; }

    /// <summary>Family name.</summary>
    public string? LastName { get; set; }

    /// <summary>A national / personal identification number.</summary>
    public string? PersonalNumber { get; set; }

    /// <summary>Date of birth.</summary>
    public DateTime? BirthDate { get; set; }
}
