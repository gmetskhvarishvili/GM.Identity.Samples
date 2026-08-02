namespace GM.Identity.Sample.Gateway.API.Authorization;

public interface ITokenManager
{
    public string? GetClaim(string key);
}