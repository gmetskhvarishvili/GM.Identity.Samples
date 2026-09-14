using System.Collections.Generic;

namespace GM.Identity.Sample.API.Users;

/// <summary>The freshly generated recovery codes, returned exactly once. The user must store them now.</summary>
public class RecoveryCodesModel
{
    public IReadOnlyList<string> Codes { get; set; } = new List<string>();
}
