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

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}