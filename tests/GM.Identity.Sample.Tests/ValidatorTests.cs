using GM.Identity.Sample.Application.Accounts.Commands.Authorize;
using GM.Identity.Sample.Application.Clients.Commands.CreateClientScope;
using GM.Identity.Sample.Application.Common;
using GM.Identity.Sample.Application.Operations.Commands.CreateOperation;
using GM.Identity.Sample.Application.Scopes.Commands.CreateScopeOperation;
using GM.Identity.Sample.Application.Users.Commands.CreateUser;
using Microsoft.Extensions.Options;
using Xunit;

using System;

namespace GM.Identity.Sample.Tests;

/// <summary>
/// Pure unit tests for the FluentValidation validators guarding the command inputs — no infrastructure, so
/// they always run. They pin the request contracts: which fields are required, and (for the user command)
/// that the email must be well-formed.
/// </summary>
public sealed class ValidatorTests
{
    [Fact]
    public void Authorize_requires_client_id_secret_and_grant_type()
    {
        var validator = new AuthorizeCommandValidator();

        Assert.True(validator.Validate(new AuthorizeCommand
        {
            ClientId = Guid.NewGuid(),
            ClientSecret = "secret",
            GrantType = "password",
        }).IsValid);

        var result = validator.Validate(new AuthorizeCommand
        {
            ClientId = Guid.Empty,      // empty Guid is rejected
            ClientSecret = "",
            GrantType = "",
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AuthorizeCommand.ClientId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AuthorizeCommand.ClientSecret));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AuthorizeCommand.GrantType));
    }

    [Fact]
    public void CreateUser_requires_username_password_and_a_valid_email()
    {
        var validator = new CreateUserCommandValidator(Options.Create(new PasswordPolicyOptions()));

        Assert.True(validator.Validate(new CreateUserCommand
        {
            Username = "jane",
            Email = "jane@example.com",
            Password = "Secret123!",
        }).IsValid);

        // Missing username/password and a malformed email are all rejected.
        var result = validator.Validate(new CreateUserCommand
        {
            Username = "",
            Email = "not-an-email",
            Password = "",
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Username));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Password));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Fact]
    public void CreateOperation_requires_name_and_description()
    {
        var validator = new CreateOperationCommandValidator();

        Assert.True(validator.Validate(new CreateOperationCommand
        {
            Name = "read:identity",
            Description = "Read identity configuration.",
        }).IsValid);

        var result = validator.Validate(new CreateOperationCommand { Name = "", Description = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOperationCommand.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOperationCommand.Description));
    }

    [Fact]
    public void CreateClientScope_requires_client_and_scope()
    {
        var validator = new CreateClientScopeCommandValidator();

        Assert.True(validator.Validate(new CreateClientScopeCommand
        {
            ClientId = Guid.NewGuid(),
            ScopeId = Guid.NewGuid(),
        }).IsValid);

        var result = validator.Validate(new CreateClientScopeCommand
        {
            ClientId = Guid.Empty,
            ScopeId = Guid.Empty,
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateClientScopeCommand.ClientId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateClientScopeCommand.ScopeId));
    }

    [Fact]
    public void CreateScopeOperation_requires_scope_and_operation()
    {
        var validator = new CreateScopeOperationCommandValidator();

        Assert.True(validator.Validate(new CreateScopeOperationCommand
        {
            ScopeId = Guid.NewGuid(),
            OperationId = Guid.NewGuid(),
        }).IsValid);

        var result = validator.Validate(new CreateScopeOperationCommand
        {
            ScopeId = Guid.Empty,
            OperationId = Guid.Empty,
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateScopeOperationCommand.ScopeId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateScopeOperationCommand.OperationId));
    }
}
