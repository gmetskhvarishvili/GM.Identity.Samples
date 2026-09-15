using System;
using System.Collections.Generic;

namespace GM.Identity.Sample.Application.Common;

/// <summary>
/// Expands the role→permission graph along the role hierarchy: a role's effective permissions are its own plus
/// every ancestor's (transitively). Pure and cycle-safe, so both the write-through path and the reconcile job
/// compute the same projection.
/// </summary>
public static class RoleHierarchyExpansion
{
    /// <summary>
    /// Computes effective permissions per role from each role's own permissions and the child→parent map.
    /// </summary>
    public static Dictionary<Guid, HashSet<Guid>> ComputeEffective(
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> ownPermissionsByRole,
        IReadOnlyDictionary<Guid, Guid> parentByRole)
    {
        var effective = new Dictionary<Guid, HashSet<Guid>>();

        // Every role that has own permissions or appears in the hierarchy needs an entry.
        var roles = new HashSet<Guid>(ownPermissionsByRole.Keys);
        foreach (var (child, parent) in parentByRole)
        {
            roles.Add(child);
            roles.Add(parent);
        }

        foreach (var role in roles)
        {
            var acc = new HashSet<Guid>();
            var current = role;
            var guard = new HashSet<Guid>(); // break cycles / repeated visits
            while (guard.Add(current))
            {
                if (ownPermissionsByRole.TryGetValue(current, out var own))
                    acc.UnionWith(own);
                if (!parentByRole.TryGetValue(current, out var parent))
                    break;
                current = parent;
            }
            effective[role] = acc;
        }

        return effective;
    }
}
