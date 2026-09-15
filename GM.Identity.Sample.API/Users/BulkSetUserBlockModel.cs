using FluentValidation;

using System;
using System.Collections.Generic;

namespace GM.Identity.Sample.API.Users;

/// <summary>Body for a bulk block/unblock operation.</summary>
public class BulkSetUserBlockModel
{
    public List<Guid> UserIds { get; set; } = new();
    public bool Block { get; set; }
}

public class BulkSetUserBlockModelValidator : AbstractValidator<BulkSetUserBlockModel>
{
    public BulkSetUserBlockModelValidator() => RuleFor(x => x.UserIds).NotNull().NotEmpty();
}
