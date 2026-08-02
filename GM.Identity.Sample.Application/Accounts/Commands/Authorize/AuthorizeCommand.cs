using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.ClientSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using ValidationException = GM.Exceptions.ValidationException;

namespace GM.Identity.Sample.Application.Accounts.Commands.Authorize;

public class AuthorizeCommand : IRequest<AuthorizeResponseDto>
{
    public string? UserName { get; set; }
    public string? Password { get; set; }

    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; }
    
    public string GrantType { get; set; }
}

public class AuthorizeCommandValidator : AbstractValidator<AuthorizeCommand>
{
    public AuthorizeCommandValidator()
    {
        RuleFor(x => x.ClientId).NotNull().NotEmpty();
        RuleFor(x => x.ClientSecret).NotNull().NotEmpty();
        RuleFor(x => x.GrantType).NotNull().NotEmpty();
    }
}

public class AuthorizeCommandHandler(
    IUnitOfWork unitOfWork) : IRequestHandler<AuthorizeCommand, AuthorizeResponseDto>
{
    public async Task<AuthorizeResponseDto> Handle(AuthorizeCommand request, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientRepository
            .FirstOrDefaultAsync(
                x => x.Id == request.ClientId &&
                     x.IsActive &&
                     !x.IsDeleted &&
                     !x.IsHidden,
                true,
                null,
                cancellationToken);

        if (client == null)
        {
            throw new NotFoundException(
                StringResource.Client,
                StringResource.Id,
                request.ClientId);
        }
        
        if (!PasswordHasher.Verify(
                request.ClientSecret, 
                client.SecretHash, 
                client.SecretSalt))
        {
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }

        if (request.GrantType == "ClientCredentials")
        {
            var clientToken = TokenGenerator.Generate();
            var clientTokenExpiration = DateTime.UtcNow.AddDays(30);

            await unitOfWork.ClientSessionRepository.AddAsync(
                ClientSession.Create(
                    client.Id,
                    TokenGenerator.Hash(clientToken),
                    clientTokenExpiration
                ), cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new AuthorizeResponseDto(clientToken, clientTokenExpiration, "bearer");
        }
        
        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(
                x => x.UserName == request.UserName &&
                     x.IsActive &&
                     !x.IsDeleted &&
                     !x.IsHidden,
                true,
                null,
                cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(
                StringResource.User,
                StringResource.UserName,
                request.UserName);
        }
        
        if (!PasswordHasher.Verify(
                request.Password, 
                user.PasswordHash, 
                user.PasswordSalt))
        {
            throw new ValidationException(ExceptionsResource.InvalidCredentials);
        }
        
        var token = TokenGenerator.Generate();
        var expiration = DateTime.UtcNow.AddDays(30);

        await unitOfWork.UserSessionRepository.AddAsync(
            UserSession.Create(
                user.Id,
                client.Id,
                null,
                TokenGenerator.Hash(token),
                expiration
            ), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthorizeResponseDto(token, expiration, "bearer");
    }
}

public record AuthorizeResponseDto(
    string AccessToken,
    DateTime ExpiresAt,
    string TokenType
);