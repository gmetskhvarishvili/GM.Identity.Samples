using FluentValidation;
using GM.Exceptions;
using GM.Identity;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

using System.Threading;
using System.Threading.Tasks;
namespace GM.Identity.Sample.Application.Users.Commands.RecoverUserPassword;

public class RecoverUserPasswordCommand : IRequest
{
    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class RecoverUserPasswordCommandValidator : AbstractValidator<RecoverUserPasswordCommand>
{
    public RecoverUserPasswordCommandValidator(
        IPasswordPolicyOptions passwordPolicy, IBreachedPasswordChecker breachedPasswordChecker)
    {
        RuleFor(x => x.Email).NotNull().NotEmpty();
        RuleFor(x => x.Code).NotNull().NotEmpty();
        RuleFor(x => x.Password).StrongPassword(passwordPolicy);
        RuleFor(x => x.Password).NotBreached(breachedPasswordChecker);
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
