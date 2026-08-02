using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Users.Commands.UpdateUserPassword;

public class UpdateUserPasswordCommand : IRequest
{
    public Guid Id { get; set; }
    public string Password { get; set; } = null!;
}

public class UpdateUserPasswordCommandValidator : AbstractValidator<UpdateUserPasswordCommand>
{
    public UpdateUserPasswordCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}

public class UpdateUserPasswordCommandHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUserPasswordCommand>
{
    public async Task Handle(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
    {
        // the root aggregate
        var entity = await unitOfWork.UserRepository
            .FirstOrDefaultAsync(x => x.Id == request.Id
                                      && x.IsActive
                                      && !x.IsDeleted
                                      && !x.IsHidden,
                true,
                null,
                cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(
                StringResource.User,
                StringResource.Id,
                request.Id!);
        }

        var (hash, salt) = PasswordHasher
            .Hash(request.Password!);
        
        entity.UpdatePassword(hash, salt);

        // Persist the aggregate
        unitOfWork.UserRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}