namespace GM.Identity.Sample.Application.Infrastructure.Services.OAuth;

public class GetEmailDto
{
    public string Code { get; set; } = null!; 
    public string State { get; set; } = null!;
    public string RedirectUri { get; set; } = null!;
    public string Provider { get; set; } = null!;
}