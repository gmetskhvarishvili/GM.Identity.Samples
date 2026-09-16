using FluentValidation;
using GM.Identity;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.ClientRedirectUriAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Clients.Commands.RegisterClient;

/// <summary>
/// OAuth 2.0 Dynamic Client Registration (RFC 7591): provisions a new client and returns its generated
/// credentials plus the registered metadata. The client secret is returned once, in the clear — it is only
/// stored hashed thereafter. Registration is admin-gated in this sample (exposed via the protected clients API)
/// rather than open.
/// </summary>
public class RegisterClientCommand : IRequest<RegisterClientResponseDto>
{
    public string ClientName { get; set; } = null!;
    public IReadOnlyCollection<string> RedirectUris { get; set; } = new List<string>();
    public string? Scope { get; set; }
    public string? BackchannelLogoutUri { get; set; }
    public string? FrontchannelLogoutUri { get; set; }
    public bool RequireConsent { get; set; }
}

public class RegisterClientCommandValidator : AbstractValidator<RegisterClientCommand>
{
    public RegisterClientCommandValidator()
    {
        RuleFor(x => x.ClientName).NotNull().NotEmpty();
    }
}

public class RegisterClientCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterClientCommand, RegisterClientResponseDto>
{
    public async Task<RegisterClientResponseDto> Handle(RegisterClientCommand request, CancellationToken cancellationToken)
    {
        var client = Client.Create(request.ClientName);

        // Generate the client secret; store only its hash, return the plaintext to the caller this once.
        var secret = TokenGenerator.Generate();
        var (hash, salt) = PasswordHasher.Hash(secret);
        client.UpdateSecret(hash, salt);

        if (!string.IsNullOrWhiteSpace(request.BackchannelLogoutUri))
            client.SetBackchannelLogoutUri(request.BackchannelLogoutUri);
        if (!string.IsNullOrWhiteSpace(request.FrontchannelLogoutUri))
            client.SetFrontchannelLogoutUri(request.FrontchannelLogoutUri);
        client.SetRequireConsent(request.RequireConsent);

        await unitOfWork.ClientRepository.AddAsync(client, cancellationToken);

        var redirectUris = request.RedirectUris
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct()
            .ToList();
        foreach (var uri in redirectUris)
            await unitOfWork.ClientRedirectUriRepository.AddAsync(
                ClientRedirectUri.Create(client.Id, uri), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterClientResponseDto
        {
            ClientId = client.Id,
            ClientSecret = secret,
            ClientName = request.ClientName,
            RedirectUris = redirectUris,
            Scope = request.Scope,
            ClientIdIssuedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
            ClientSecretExpiresAt = 0, // never expires
        };
    }
}

/// <summary>RFC 7591 client registration response (snake_case).</summary>
public class RegisterClientResponseDto
{
    [JsonPropertyName("client_id")] public Guid ClientId { get; set; }
    [JsonPropertyName("client_secret")] public string ClientSecret { get; set; } = null!;
    [JsonPropertyName("client_name")] public string ClientName { get; set; } = null!;
    [JsonPropertyName("redirect_uris")] public IReadOnlyCollection<string> RedirectUris { get; set; } = new List<string>();
    [JsonPropertyName("scope")] public string? Scope { get; set; }
    [JsonPropertyName("client_id_issued_at")] public long ClientIdIssuedAt { get; set; }
    [JsonPropertyName("client_secret_expires_at")] public long ClientSecretExpiresAt { get; set; }
}
