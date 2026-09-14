using FluentValidation;
using GM.Exceptions;
using GM.Identity.Sample.Common.Resources;
using GM.Identity.Sample.Domain.SeedWork;
using GM.Mediator.Contracts;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GM.Identity.Sample.Application.Users.Queries.ExportCurrentUserData;

/// <summary>
/// GDPR-style data export: gathers everything this identity server holds about a user — profile, roles, 2FA
/// enrolments, active sessions — into a single document the user can download. Password hashes and code/token
/// secrets are deliberately excluded.
/// </summary>
public class ExportCurrentUserDataQuery : IRequest<UserDataExportDto>
{
    public Guid UserId { get; set; }
}

public class ExportCurrentUserDataQueryValidator : AbstractValidator<ExportCurrentUserDataQuery>
{
    public ExportCurrentUserDataQueryValidator() => RuleFor(x => x.UserId).NotNull().NotEmpty();
}

public class ExportCurrentUserDataQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ExportCurrentUserDataQuery, UserDataExportDto>
{
    public async Task<UserDataExportDto> Handle(ExportCurrentUserDataQuery request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository
            .Query(false, null)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.UserId && !x.IsDeleted, cancellationToken);
        if (user == null)
            throw new NotFoundException(StringResource.User, StringResource.Id, request.UserId);

        var roleIds = await unitOfWork.UserRoleRepository
            .Query(false, null).IgnoreQueryFilters()
            .Where(x => x.UserId == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        var twoFactorTypeIds = await unitOfWork.UserTwoFactorAuthTypeRepository
            .Query(false, null).IgnoreQueryFilters()
            .Where(x => x.UserId == request.UserId && x.IsActive && !x.IsDeleted && !x.IsHidden)
            .Select(x => x.TwoFactorAuthTypeId)
            .ToListAsync(cancellationToken);

        var hasTotp = await unitOfWork.UserTotpDeviceRepository
            .Query(false, null).IgnoreQueryFilters()
            .AnyAsync(x => x.UserId == request.UserId && x.IsConfirmed && x.IsActive && !x.IsDeleted && !x.IsHidden, cancellationToken);

        var sessions = await unitOfWork.UserSessionRepository
            .Query(false, null).IgnoreQueryFilters()
            .Where(x => x.UserId == request.UserId && !x.IsRevoked)
            .Select(x => new ExportedSessionDto
            {
                ClientId = x.ClientId,
                CreatedAt = x.CreatedAt,
                ExpiresAt = x.ExpiresAt,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
            })
            .ToListAsync(cancellationToken);

        return new UserDataExportDto
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            CreatedAt = user.CreatedAt,
            RoleIds = roleIds,
            TwoFactorAuthTypeIds = twoFactorTypeIds,
            HasAuthenticatorApp = hasTotp,
            ActiveSessions = sessions,
        };
    }
}

public class UserDataExportDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyCollection<Guid> RoleIds { get; set; } = new List<Guid>();
    public IReadOnlyCollection<int> TwoFactorAuthTypeIds { get; set; } = new List<int>();
    public bool HasAuthenticatorApp { get; set; }
    public IReadOnlyCollection<ExportedSessionDto> ActiveSessions { get; set; } = new List<ExportedSessionDto>();
}

public class ExportedSessionDto
{
    public Guid? ClientId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
