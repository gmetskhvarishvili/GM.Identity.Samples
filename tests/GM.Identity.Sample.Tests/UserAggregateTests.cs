using GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.UserAggregate;
using Xunit;

namespace GM.Identity.Sample.Tests;

public class UserAggregateTests
{
    [Fact]
    public void Create_SetsTheProvidedIdentityFields()
    {
        var user = User.Create("alice", "alice@example.com", "+995555123456");

        Assert.Equal("alice", user.UserName);
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal("+995555123456", user.PhoneNumber);
    }

    [Fact]
    public void Create_StartsWithEmptyRelationshipCollectionsAndUnconfirmedState()
    {
        var user = User.Create("bob", null, null);

        Assert.Empty(user.UserRoles);
        Assert.Empty(user.UserSessions);
        Assert.Empty(user.UserTwoFactorAuthTypes);
        Assert.False(user.EmailConfirmed);
        Assert.False(user.PhoneNumberConfirmed);
        Assert.False(user.IsBlocked);
    }

    [Fact]
    public void ConfirmEmail_MarksTheEmailConfirmedWithATimestamp()
    {
        var user = User.Create("bob", "bob@example.com", null);

        user.ConfirmEmail();

        Assert.True(user.EmailConfirmed);
        Assert.NotNull(user.EmailConfirmedAt);
    }

    [Fact]
    public void ConfirmPhoneNumber_MarksThePhoneConfirmedWithATimestamp()
    {
        var user = User.Create("bob", null, "+995555000000");

        user.ConfirmPhoneNumber();

        Assert.True(user.PhoneNumberConfirmed);
        Assert.NotNull(user.PhoneNumberConfirmedAt);
    }

    [Fact]
    public void BlockThenUnBlock_TogglesTheBlockedState()
    {
        var user = User.Create("bob", null, null);

        user.Block();
        Assert.True(user.IsBlocked);
        Assert.NotNull(user.BlockedAt);

        user.UnBlock();
        Assert.False(user.IsBlocked);
        Assert.Null(user.BlockedAt);
    }

    [Fact]
    public void IncreaseAccessFailedCount_IsIgnored_WhenLockoutIsDisabled()
    {
        var user = User.Create("bob", null, null);

        user.IncreaseAccessFailedCount(lockout: true, DateTime.UtcNow.AddMinutes(5));

        Assert.Equal(0, user.AccessFailedCount);
        Assert.Null(user.LockoutEnd);
    }

    [Fact]
    public void IncreaseAccessFailedCount_IncrementsAndLocksOut_WhenEnabled()
    {
        var user = User.Create("bob", null, null);
        user.EnableLockOut();
        var until = DateTime.UtcNow.AddMinutes(15);

        user.IncreaseAccessFailedCount(lockout: true, until);

        Assert.Equal(1, user.AccessFailedCount);
        Assert.Equal(until, user.LockoutEnd);
    }

    [Fact]
    public void UpdatePassword_StoresTheHashAndSalt()
    {
        var user = User.Create("bob", null, null);

        user.UpdatePassword("the-hash", "the-salt");

        Assert.Equal("the-hash", user.PasswordHash);
        Assert.Equal("the-salt", user.PasswordSalt);
    }

    [Fact]
    public void Update_ChangesUsernameEmailAndPhone()
    {
        var user = User.Create("bob", "bob@example.com", "+100");

        user.Update("bobby", "bobby@example.com", "+200");

        Assert.Equal("bobby", user.UserName);
        Assert.Equal("bobby@example.com", user.Email);
        Assert.Equal("+200", user.PhoneNumber);
    }
}
