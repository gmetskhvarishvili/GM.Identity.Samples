using GM.Identity.Sample.Application.Infrastructure.Services.Logout;
using GM.Mediator.Contracts;

using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Queries.GetJwks;

/// <summary>
/// Returns the OP's JSON Web Key Set — the public signing keys relying parties use to verify back-channel
/// logout tokens (and future id_tokens). Served at <c>/.well-known/jwks.json</c>.
/// </summary>
public record GetJwksQuery : IRequest<JsonWebKeySetDto>;

public class GetJwksQueryHandler(IJwksProvider jwksProvider) : IRequestHandler<GetJwksQuery, JsonWebKeySetDto>
{
    public Task<JsonWebKeySetDto> Handle(GetJwksQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(jwksProvider.GetKeys());
}
