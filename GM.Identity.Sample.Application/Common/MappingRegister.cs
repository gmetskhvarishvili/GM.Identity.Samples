using GM.Identity.Sample.Application.Users.Queries.GetUserDetails;
using GM.Identity.Sample.Application.Users.Queries.GetUsersList;
using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using Mapster;
using System.Linq;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Mapster configuration for the application layer. Registered by scanning this assembly at
/// startup. GM.API formats the audit dates; this covers what Mapster can't match by name.
/// </summary>
public class MappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // The entity says UserName, the DTOs say Username; Mapster matches names case-sensitively, so without
        // these the username was silently left null in every user response.
        config.NewConfig<User, UserDto>()
            .Map(dest => dest.Username, src => src.UserName);
        config.NewConfig<User, UserDetailsDto>()
            .Map(dest => dest.Username, src => src.UserName)
            .Map(dest => dest.RoleIds, src => src.UserRoles
                .Where(x => x.IsActive && !x.IsDeleted).Select(x => x.RoleId).ToList())
            .Map(dest => dest.TwoFactorAuthTypeIds, src => src.UserTwoFactorAuthTypes
                .Where(x => x.IsActive && !x.IsDeleted && !x.IsHidden).Select(x => x.TwoFactorAuthTypeId).ToList());
    }
}
