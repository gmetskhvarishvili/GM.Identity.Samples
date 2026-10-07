using GM.EntityFramework.Domain.Repositories;

namespace GM.Identity.Sample.Domain.BoundedContext.IdentityBoundedContext.PasskeyAggregate.Interfaces;

public interface IUserPasskeyRepository : IGenericRepository<UserPasskey>;

public interface IPasskeyChallengeRepository : IGenericRepository<PasskeyChallenge>;
