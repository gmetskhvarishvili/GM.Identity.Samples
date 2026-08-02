using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;

namespace GM.Identity.Sample.Application.Users.Commands.UpdateUser;

public class UpdateUserCommand : IRequest
{
    public Guid? Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEmpty();
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotNull().NotEmpty();
    }
}

public class UpdateUserCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
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

        if (await unitOfWork.UserRepository.ExistsAsync(
                x => x.Id != entity.Id
                     && x.Email == request.Email
                     && x.IsActive
                     && !x.IsDeleted
                     && !x.IsHidden,
                cancellationToken))
        {
            throw new AlreadyExistsException(
                StringResource.User,
                StringResource.Email,
                request.Email!);
        }

        entity.Update(request.Username!, request.Email!, request.PhoneNumber);

        // Persist the aggregate
        unitOfWork.UserRepository.Update(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}