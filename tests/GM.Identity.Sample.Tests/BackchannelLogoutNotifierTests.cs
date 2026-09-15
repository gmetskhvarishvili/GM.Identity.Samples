using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
using GM.Identity.Sample.Infrastructure.Services.Logout;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Unit tests for the back-channel logout notifier: it POSTs a <c>logout_token</c> form field to each relying
/// party's endpoint, and a failing relying party never aborts delivery to the others. Uses a capturing HTTP
/// handler — no network, no infrastructure.
/// </summary>
public sealed class BackchannelLogoutNotifierTests
{
    [Fact]
    public async Task Posts_a_logout_token_to_every_target_and_isolates_failures()
    {
        var handler = new CapturingHandler();
        var notifier = new BackchannelLogoutNotifier(
            new SingleClientFactory(handler),
            new LogoutTokenGenerator(new OidcSigningKey(pem: null)),
            NullLogger<BackchannelLogoutNotifier>.Instance);

        var okTarget = new BackchannelLogoutTarget(Guid.NewGuid(), "https://rp-ok.example/logout", Guid.NewGuid(), Guid.NewGuid());
        var failTarget = new BackchannelLogoutTarget(Guid.NewGuid(), "https://rp-fail.example/logout", Guid.NewGuid(), Guid.NewGuid());
        handler.FailFor.Add(failTarget.LogoutUri);

        // Must not throw even though one relying party fails.
        await notifier.NotifyAsync("https://op.example", new[] { failTarget, okTarget }, CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, r =>
        {
            Assert.Equal(HttpMethod.Post, r.Method);
            Assert.Contains("logout_token=", r.Body);
        });
        Assert.Contains(handler.Requests, r => r.Uri == okTarget.LogoutUri);
        Assert.Contains(handler.Requests, r => r.Uri == failTarget.LogoutUri);
    }

    private sealed record CapturedRequest(HttpMethod Method, string? Uri, string Body);

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public readonly List<CapturedRequest> Requests = new();
        public readonly HashSet<string> FailFor = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.ToString();
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.Method, uri, body));

            var status = uri is not null && FailFor.Contains(uri)
                ? HttpStatusCode.InternalServerError
                : HttpStatusCode.OK;
            return new HttpResponseMessage(status);
        }
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public System.Net.Http.HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
