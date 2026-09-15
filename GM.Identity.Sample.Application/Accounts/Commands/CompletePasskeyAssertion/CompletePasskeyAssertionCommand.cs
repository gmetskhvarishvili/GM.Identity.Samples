using FluentValidation;
using GM.Exceptions;
using GM.Identity.Authorization;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Application.Common.Security;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ValidationException = GM.Exceptions.ValidationException;

using System;
using System.Buffers.Binary;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Accounts.Commands.CompletePasskeyAssertion;

/// <summary>
/// Completes a passkey login: verifies the WebAuthn assertion signature against the registered credential's
/// public key and the issued challenge, then mints a user session. The signature proves possession of the
/// authenticator's private key without any password.
/// </summary>
public class CompletePasskeyAssertionCommand : IRequest<PasskeyTokenDto>
{
    public string UserName { get; set; } = null!;
    public Guid ClientId { get; set; }
    public string CredentialId { get; set; } = null!;
    public string AuthenticatorDataBase64Url { get; set; } = null!;
    public string ClientDataJsonBase64Url { get; set; } = null!;
    public string SignatureBase64Url { get; set; } = null!;
}

public class CompletePasskeyAssertionCommandValidator : AbstractValidator<CompletePasskeyAssertionCommand>
{
    public CompletePasskeyAssertionCommandValidator()
    {
        RuleFor(x => x.UserName).NotNull().NotEmpty();
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.CredentialId).NotNull().NotEmpty();
        RuleFor(x => x.AuthenticatorDataBase64Url).NotNull().NotEmpty();
        RuleFor(x => x.ClientDataJsonBase64Url).NotNull().NotEmpty();
        RuleFor(x => x.SignatureBase64Url).NotNull().NotEmpty();
    }
}

public class CompletePasskeyAssertionCommandHandler(
    IUnitOfWork unitOfWork,
    ISessionCache sessionCache,
    IOptions<AuthOptions> options) : IRequestHandler<CompletePasskeyAssertionCommand, PasskeyTokenDto>
{
    public async Task<PasskeyTokenDto> Handle(CompletePasskeyAssertionCommand request, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientRepository
            .Query(false, null).IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.ClientId && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (client == null)
            throw new NotFoundException(StringResource.Client, StringResource.Id, request.ClientId);

        var user = await unitOfWork.UserRepository
            .Query(true, null).IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.UserName == request.UserName && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (user == null || user.IsBlocked)
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var passkey = await unitOfWork.UserPasskeyRepository
            .Query(true, null).IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.CredentialId == request.CredentialId && x.UserId == user.Id
                                      && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);
        if (passkey == null)
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        var now = DateTime.UtcNow;
        var challenge = await unitOfWork.PasskeyChallengeRepository
            .Query(true, null).IgnoreQueryFilters()
            .Where(x => x.UserId == user.Id && x.ExpiresAt > now && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .OrderByDescending(x => x.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (challenge == null)
            throw new ValidationException("No active passkey challenge; begin the assertion first.");

        var authenticatorData = WebAuthnAssertion.Base64UrlDecode(request.AuthenticatorDataBase64Url);
        var clientDataJson = WebAuthnAssertion.Base64UrlDecode(request.ClientDataJsonBase64Url);
        var signature = WebAuthnAssertion.Base64UrlDecode(request.SignatureBase64Url);

        // The clientDataJSON must be a webauthn.get whose challenge matches the one we issued.
        using (var doc = JsonDocument.Parse(clientDataJson))
        {
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
            var presented = root.TryGetProperty("challenge", out var c) ? c.GetString() : null;
            if (type != "webauthn.get" || presented != challenge.Challenge)
                throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }

        if (!WebAuthnAssertion.VerifyEs256(passkey.PublicKeySpki, authenticatorData, clientDataJson, signature))
            throw new ValidationException(ExceptionsResource.InvalidCredentials);

        // Consume the challenge and record the authenticator's signature counter.
        if (authenticatorData.Length >= 37)
            passkey.RecordUse(BinaryPrimitives.ReadUInt32BigEndian(authenticatorData.AsSpan(33, 4)));
        unitOfWork.UserPasskeyRepository.Update(passkey);
        unitOfWork.PasskeyChallengeRepository.Remove(challenge);

        var settings = options.Value;
        var accessToken = TokenGenerator.Generate();
        var refreshToken = TokenGenerator.Generate();
        var accessExpiry = now.AddMinutes(settings.AccessTokenMinutes);
        var accessHash = TokenGenerator.Hash(accessToken);
        var refreshExpiry = now.AddDays(settings.RefreshTokenDays);

        var session = UserSession.Create(
            user.Id, request.ClientId, "passkey", accessHash, refreshExpiry, TokenGenerator.Hash(refreshToken));
        await unitOfWork.UserSessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await sessionCache.SetAsync(
            accessHash, new SessionInfo(user.Id, session.Id, request.ClientId, accessExpiry), cancellationToken);

        return new PasskeyTokenDto(accessToken, accessExpiry, "bearer", refreshToken, refreshExpiry);
    }
}

public record PasskeyTokenDto(
    string AccessToken, DateTime ExpiresAt, string TokenType, string RefreshToken, DateTime RefreshTokenExpiresAt);
