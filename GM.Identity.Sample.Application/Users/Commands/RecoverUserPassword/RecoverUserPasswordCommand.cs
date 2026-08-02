using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Users.Commands.RecoverUserPassword;

public class RecoverUserPasswordCommand : IRequest
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class RecoverUserPasswordCommandValidator : AbstractValidator<RecoverUserPasswordCommand>
{
    public RecoverUserPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotNull().NotEmpty();
        RuleFor(x => x.Code).NotNull().NotEmpty();
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}

public class RecoverUserPasswordCommandHandler(
    IUnitOfWork unitOfWork
) : IRequestHandler<RecoverUserPasswordCommand>
{
    public async Task Handle(RecoverUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var entity = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Email == request.Email
                                      && x.IsActive
                                      && !x.IsDeleted
                                      && !x.IsHidden,
                false,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.User,
                StringResource.Email,
                request.Email);
        }

        // var userCode = await unitOfWork.UserCodeRepository
        //     .FirstOrDefaultAsync(x => x.UserId == entity.Id
        //                               && x.Code == request.Code
        //                               && x.IsActive
        //                               && !x.IsDeleted
        //                               && !x.IsHidden,
        //         true,
        //         null,
        //         cancellationToken);
        //
        // if (userCode == null)
        // {
        //     throw new NotFoundException(
        //         StringResource.User,
        //         StringResource.Email,
        //         request.Code);
        // }
        //
        // if (userCode.ExpirationDate < DateTime.UtcNow)
        // {
        //     throw new CustomException("Code Expired");
        // }
        //
        // if (userCode.Status == 2)
        // {
        //     throw new CustomException("Code Already Used");
        // }
        //
        // if (request.Code != userCode.Code)
        // {
        //     throw new CustomException("Invalid Code");
        // }
        //
        // userCode.UpdateStatus(2);
        // unitOfWork.UserCodeRepository.Update(userCode);
        //
        // entity.UpdatePassword(userPasswordHasher.HashPassword(entity, request.Password));
        // unitOfWork.UserRepository.Update(entity);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}