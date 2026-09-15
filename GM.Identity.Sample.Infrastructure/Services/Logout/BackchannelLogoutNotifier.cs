using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
using Microsoft.Extensions.Logging;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Infrastructure.Services.Logout;

/// <summary>
/// <see cref="IBackchannelLogoutNotifier"/> that POSTs a signed <c>logout_token</c> (as
/// <c>application/x-www-form-urlencoded</c>) to each relying party's back-channel logout endpoint. Delivery is
/// best-effort and isolated per target: a slow or failing RP is logged and skipped so it cannot hold up logout
/// or block the other relying parties.
/// </summary>
public sealed class BackchannelLogoutNotifier(
    IHttpClientFactory httpClientFactory,
    LogoutTokenGenerator logoutTokenGenerator,
    ILogger<BackchannelLogoutNotifier> logger) : IBackchannelLogoutNotifier
{
    public async Task NotifyAsync(
        string issuer, IReadOnlyCollection<BackchannelLogoutTarget> targets, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("BackchannelLogout");

        foreach (var target in targets)
        {
            try
            {
                var logoutToken = logoutTokenGenerator.Generate(issuer, target.ClientId, target.UserId, target.SessionId);
                var content = new FormUrlEncodedContent(
                    new[] { new KeyValuePair<string, string>("logout_token", logoutToken) });

                var response = await client.PostAsync(target.LogoutUri, content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    logger.LogWarning(
                        "Back-channel logout to client {ClientId} at {Uri} returned {StatusCode}",
                        target.ClientId, target.LogoutUri, (int)response.StatusCode);
            }
            catch (System.Exception ex)
            {
                // A relying party being unreachable must not fail the user's logout.
                logger.LogWarning(ex,
                    "Back-channel logout to client {ClientId} at {Uri} failed", target.ClientId, target.LogoutUri);
            }
        }
    }
}
