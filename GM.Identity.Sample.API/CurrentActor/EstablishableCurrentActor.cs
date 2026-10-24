using System;
using GM.API;
using GM.EntityFramework.Domain.Common;
using GM.Identity.Sample.Application.Common;
using Microsoft.AspNetCore.Http;

namespace GM.Identity.Sample.API.CurrentActor;

/// <summary>
/// The current actor from GM.API's <see cref="HttpContextCurrentActor"/> (gateway-forwarded headers), plus
/// what a handler established during the request via <see cref="IActorEstablisher"/>. Header values always
/// win; an established user/client/session only fills a field the request left empty — so a login or sign-up
/// (anonymous at the edge) is audited as the user who signed in, while an authenticated caller stays the actor.
/// The tenant is never established: it drives the query filter and must not change mid-request. A singleton
/// over <see cref="IHttpContextAccessor"/> like the actor it wraps, keeping per-request state in
/// <see cref="HttpContext.Items"/>.
/// </summary>
public sealed class EstablishableCurrentActor(HttpContextCurrentActor inner, IHttpContextAccessor httpContextAccessor)
    : ICurrentActor, IActorEstablisher
{
    private static readonly object ItemKey = new();

    private sealed class Established
    {
        public Guid? UserId;
        public Guid? ClientId;
        public Guid? SessionId;
    }

    private Established? Current =>
        httpContextAccessor.HttpContext?.Items.TryGetValue(ItemKey, out var value) == true ? value as Established : null;

    public void Establish(Guid? userId, Guid? clientId, Guid? sessionId)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return; // outside a request (jobs, direct mediator calls) there is nothing to attribute

        var established = Current ?? new Established();
        established.UserId = userId ?? established.UserId;
        established.ClientId = clientId ?? established.ClientId;
        established.SessionId = sessionId ?? established.SessionId;
        context.Items[ItemKey] = established;
    }

    public Guid? UserId => inner.UserId ?? Current?.UserId;
    public Guid? ClientId => inner.ClientId ?? Current?.ClientId;
    public Guid? SessionId => inner.SessionId ?? Current?.SessionId;
    public Guid? TenantId => inner.TenantId;
    public string? ChannelId => inner.ChannelId;
    public string? Culture => inner.Culture;
    public string? IpAddress => inner.IpAddress;
    public string? CorrelationId => inner.CorrelationId;
    public string? UserAgent => inner.UserAgent;
    public string? IdempotencyKey => inner.IdempotencyKey;
}
