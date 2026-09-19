using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Users;

/// <summary>The freshly generated recovery codes, returned exactly once. The user must store them now.</summary>
public class RecoveryCodesModel
{
    /// <summary>The codes.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Codes")]
    public IReadOnlyList<string> Codes { get; set; } = new List<string>();
}
