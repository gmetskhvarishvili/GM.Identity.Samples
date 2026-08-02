using GM.Identity.Sample.API.Operations;

namespace GM.Identity.Sample.API.Scopes;

public class ScopeOperationModel
{
    public Guid Id { get; set; }
    public ScopeModel? Scope { get; set; }
    public OperationModel? Operation { get; set; }
}