using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GM.Identity.Sample.Application.Infrastructure.Services.Logout;

/// <summary>
/// Publishes the OP's public signing keys as a JWK Set, so relying parties can verify the signature on
/// back-channel logout tokens (and, in future, id_tokens). Served at <c>/.well-known/jwks.json</c>.
/// </summary>
public interface IJwksProvider
{
    JsonWebKeySetDto GetKeys();
}

public sealed class JsonWebKeySetDto
{
    [JsonPropertyName("keys")] public IReadOnlyCollection<JsonWebKeyDto> Keys { get; set; } = new List<JsonWebKeyDto>();
}

/// <summary>A single public JWK for an EC (P-256 / ES256) signing key.</summary>
public sealed class JsonWebKeyDto
{
    [JsonPropertyName("kty")] public string KeyType { get; set; } = null!;
    [JsonPropertyName("use")] public string Use { get; set; } = null!;
    [JsonPropertyName("alg")] public string Algorithm { get; set; } = null!;
    [JsonPropertyName("kid")] public string KeyId { get; set; } = null!;
    [JsonPropertyName("crv")] public string Curve { get; set; } = null!;
    [JsonPropertyName("x")] public string X { get; set; } = null!;
    [JsonPropertyName("y")] public string Y { get; set; } = null!;
}
