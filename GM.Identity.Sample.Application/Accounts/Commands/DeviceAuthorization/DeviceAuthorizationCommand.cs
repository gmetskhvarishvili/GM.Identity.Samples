using FluentValidation;
using GM.Exceptions;
using GM.Identity;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.DeviceCodeAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.DeviceAuthorization;

/// <summary>
/// The device authorization endpoint (RFC 8628 §3.1/§3.2). Authenticates the client and issues a device_code
/// (polled at the token endpoint) paired with a short, human-readable user_code the user approves at the
/// verification URI on a second device.
/// </summary>
public class DeviceAuthorizationCommand : IRequest<DeviceAuthorizationResponseDto>
{
    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; } = null!;
    public string? Scope { get; set; }

    /// <summary>This OP's issuer (scheme+host), used to build the verification URIs.</summary>
    public string? Issuer { get; set; }
}

public class DeviceAuthorizationCommandValidator : AbstractValidator<DeviceAuthorizationCommand>
{
    public DeviceAuthorizationCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ClientSecret).NotNull().NotEmpty();
    }
}

public class DeviceAuthorizationCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeviceAuthorizationCommand, DeviceAuthorizationResponseDto>
{
    private const int IntervalSeconds = 5;
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    // Unambiguous alphabet: no vowels (avoids words) and no easily confused characters (0/O, 1/I).
    private const string UserCodeAlphabet = "BCDFGHJKLMNPQRSTVWXZ23456789";

    public async Task<DeviceAuthorizationResponseDto> Handle(
        DeviceAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.ClientId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (client == null)
            throw new NotFoundException(StringResource.Client, StringResource.Id, request.ClientId);
        if (!PasswordHasher.Verify(request.ClientSecret, client.SecretHash, client.SecretSalt))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var deviceCode = TokenGenerator.Generate();
        var (displayCode, normalizedCode) = GenerateUserCode();
        var expiresAt = DateTime.UtcNow.Add(Lifetime);

        await unitOfWork.DeviceCodeRepository.AddAsync(
            DeviceCode.Create(client.Id, TokenGenerator.Hash(deviceCode), normalizedCode, IntervalSeconds, expiresAt),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var issuer = (request.Issuer ?? string.Empty).TrimEnd('/');
        var verificationUri = $"{issuer}/connect/device";

        return new DeviceAuthorizationResponseDto
        {
            DeviceCode = deviceCode,
            UserCode = displayCode,
            VerificationUri = verificationUri,
            VerificationUriComplete = $"{verificationUri}?user_code={Uri.EscapeDataString(displayCode)}",
            ExpiresIn = (int)Lifetime.TotalSeconds,
            Interval = IntervalSeconds,
        };
    }

    // Returns a display code ("ABCD-EFGH") and its normalized form ("ABCDEFGH") for storage/lookup.
    private static (string Display, string Normalized) GenerateUserCode()
    {
        var chars = new char[8];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = UserCodeAlphabet[RandomNumberGenerator.GetInt32(UserCodeAlphabet.Length)];
        var normalized = new string(chars);
        var display = new StringBuilder().Append(normalized, 0, 4).Append('-').Append(normalized, 4, 4).ToString();
        return (display, normalized);
    }
}

/// <summary>RFC 8628 §3.2 device authorization response (snake_case).</summary>
public class DeviceAuthorizationResponseDto
{
    [JsonPropertyName("device_code")] public string DeviceCode { get; set; } = null!;
    [JsonPropertyName("user_code")] public string UserCode { get; set; } = null!;
    [JsonPropertyName("verification_uri")] public string VerificationUri { get; set; } = null!;
    [JsonPropertyName("verification_uri_complete")] public string VerificationUriComplete { get; set; } = null!;
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("interval")] public int Interval { get; set; }
}
