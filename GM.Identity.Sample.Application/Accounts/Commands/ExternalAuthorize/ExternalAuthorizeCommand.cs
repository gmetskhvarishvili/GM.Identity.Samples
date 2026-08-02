using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Application.Accounts.Commands.Authorize;
using GM.Identity.Sample.Application.Infrastructure.Services.OAuth;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using GM.Identity.Sample.Application.Users.Commands.CreateUserRole;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.UserRoleAggregate;
using GM.Identity.Sample.Domain.BoundedContext.AuthorizationBoundedContext.UserSessionAggregate;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Mapster;
using Microsoft.AspNetCore.Http;
using ValidationException = FluentValidation.ValidationException;

namespace GM.Identity.Sample.Application.Accounts.Commands.ExternalAuthorize;

public class ExternalAuthorizeCommand : IRequest<AuthorizeResponseDto>
{
    public string Code { get; set; } = null!;
    public string State { get; set; } = null!;
    public string RedirectUri { get; set; } = null!;
    public string Provider { get; set; } = null!;
    
    public Guid ClientId { get; set; }
    public string ClientSecret { get; set; }
}

public class ExternalAuthorizeCommandValidator : AbstractValidator<ExternalAuthorizeCommand>
{
    public ExternalAuthorizeCommandValidator()
    {
        RuleFor(x => x.Code).NotNull().NotEmpty();
        RuleFor(x => x.State).NotNull().NotEmpty();
        RuleFor(x => x.RedirectUri).NotNull().NotEmpty();
        RuleFor(x => x.Provider).NotNull().NotEmpty();
    }
}

public class ExternalAuthorizeCommandHandler(
    IOAuthService oAuthService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ExternalAuthorizeCommand, AuthorizeResponseDto>
{
    public async Task<AuthorizeResponseDto> Handle(ExternalAuthorizeCommand request, CancellationToken cancellationToken)
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
        
        var email = await oAuthService.GetEmail(
            request.Adapt<GetEmailDto>(),
            cancellationToken);
        
        var user = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(
                x => x.Email ==email &&
                     x.IsActive &&
                     !x.IsDeleted &&
                     !x.IsHidden,
                true,
                null,
                cancellationToken);

        if (user == null)
        {
            if (await unitOfWork.UserRepository.ExistsAsync(
                    x => x.UserName == email
                         && x.IsActive
                         && !x.IsDeleted
                         && !x.IsHidden,
                    cancellationToken))
            {
                throw new AlreadyExistsException(
                    StringResource.User,
                    StringResource.UserName,
                    email);
            }
            
            user = User
                .Create(email, email, null);
            
            await unitOfWork.UserRepository.AddAsync(user, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var token = TokenGenerator.Generate();
        var expiration = DateTime.UtcNow.AddDays(30);

        await unitOfWork.UserSessionRepository.AddAsync(
            UserSession.Create(
                user.Id,
                client.Id,
                request.Provider,
                TokenGenerator.Hash(token),
                expiration
            ), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthorizeResponseDto(token, expiration, "bearer");
    }
}