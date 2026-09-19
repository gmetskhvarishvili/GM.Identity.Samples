using FluentValidation;

using System;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>Body for a bulk block/unblock operation.</summary>
public class BulkSetUserBlockModel
{
    [Display(ResourceType = typeof(StringResource), Name = "UserIds")]
    public List<Guid> UserIds { get; set; } = new();
    [Display(ResourceType = typeof(StringResource), Name = "Block")]
    public bool Block { get; set; }
}

public class BulkSetUserBlockModelValidator : AbstractValidator<BulkSetUserBlockModel>
{
    public BulkSetUserBlockModelValidator() => RuleFor(x => x.UserIds).NotNull().NotEmpty();
}
