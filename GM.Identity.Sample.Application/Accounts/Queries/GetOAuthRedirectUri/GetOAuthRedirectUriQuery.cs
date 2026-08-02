using FluentValidation;
using GM.Identity.Sample.Application.Infrastructure.Services.OAuth;
using GM.Mediator.Contracts;
using Mapster;

namespace GM.Identity.Sample.Application.Accounts.Queries.GetOAuthRedirectUri;

public class GetOAuthRedirectUriQuery : IRequest<string>
{
    public string RedirectUri { get; set; } = null!;
    public string Provider { get; set; } = null!;
}

public class GetOAuthRedirectUriQueryValidator : AbstractValidator<GetOAuthRedirectUriQuery>
{
    public GetOAuthRedirectUriQueryValidator()
    {
        RuleFor(x => x.Provider).NotEmpty();
        RuleFor(x => x.Provider).NotEmpty();
    }
}

public class GetOAuthRedirectUriQueryHandler(IOAuthService oAuthService)
    : IRequestHandler<GetOAuthRedirectUriQuery, string>
{
    public Task<string> Handle(GetOAuthRedirectUriQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(oAuthService.GetRedirectUri(
            request.Adapt<GetRedirectUriDto>()));
    }
}