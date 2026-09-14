using Microsoft.AspNetCore.Mvc;

using System;

namespace GM.Identity.Sample.API.Accounts;

public class RevokeTokenModel
{
    [FromForm(Name = "token")]
    public string Token { get; set; } = null!;

    [FromForm(Name = "client_id")]
    public Guid ClientId { get; set; }

    [FromForm(Name = "client_secret")]
    public string ClientSecret { get; set; } = null!;
}
