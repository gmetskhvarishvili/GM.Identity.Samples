using GM.Identity.Sample.Domain.BoundedContext.AccessControlBoundedContext.RoleAggregate;
using Xunit;

namespace GM.Identity.Sample.Tests;

public class RoleAggregateTests
{
    [Fact]
    public void Create_SetsTheName()
    {
        var role = Role.Create("Administrator");

        Assert.Equal("Administrator", role.Name);
    }

    [Fact]
    public void Create_StartsWithEmptyRelationshipCollections()
    {
        var role = Role.Create("Administrator");

        Assert.Empty(role.UserRoles);
        Assert.Empty(role.RolePermissions);
    }

    [Fact]
    public void Update_ChangesTheName()
    {
        var role = Role.Create("Administrator");

        role.Update("SuperAdmin");

        Assert.Equal("SuperAdmin", role.Name);
    }
}
