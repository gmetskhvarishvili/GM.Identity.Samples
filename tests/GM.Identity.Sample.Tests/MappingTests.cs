using GM.Identity.Sample.API.Operations;
using GM.Identity.Sample.API.Users;
using GM.Identity.Sample.Application.Operations.Commands.CreateOperation;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using Mapster;
using Xunit;

using System;
using System.Linq;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Pure unit tests for the Mapster request-model → command adaptations the controllers rely on (no
/// infrastructure). They pin that scalar fields, nested child collections, and the 2FA-enrolment list all
/// carry across.
/// </summary>
public sealed class MappingTests
{
    [Fact]
    public void CreateUserModel_maps_to_command_including_roles_and_2fa()
    {
        var roleId = Guid.NewGuid();
        var model = new CreateUserModel
        {
            Username = "jane",
            Email = "jane@example.com",
            Password = "Secret123!",
            PhoneNumber = "+995500000000",
            UserRoles = [new CreateUserRoleModel { RoleId = roleId }],
            TwoFactorAuthTypeIds = [1, 2],
        };

        var command = model.Adapt<CreateUserCommand>();

        Assert.Equal("jane", command.Username);
        Assert.Equal("jane@example.com", command.Email);
        Assert.Equal("Secret123!", command.Password);
        Assert.Equal("+995500000000", command.PhoneNumber);
        Assert.Equal(roleId, Assert.Single(command.UserRoles!).RoleId);
        Assert.Equal([1, 2], command.TwoFactorAuthTypeIds!.ToArray());
    }

    [Fact]
    public void CreateOperationModel_maps_to_command()
    {
        var model = new CreateOperationModel { Name = "read:reports", Description = "Read reports." };

        var command = model.Adapt<CreateOperationCommand>();

        Assert.Equal("read:reports", command.Name);
        Assert.Equal("Read reports.", command.Description);
    }
}
