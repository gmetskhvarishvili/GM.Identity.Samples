using GM.API.Startup;
using GM.HttpClient;
using GM.Identity.Sample.Gateway.API.Authorization;
using GM.Identity.Sample.Gateway.API.Services.Permissions;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using OpenIddict.Validation.AspNetCore;

var builder = ProgramExtension.CreateGMBuilder(args);

builder.Services.ConfigureGMServices(
    builder.Configuration,
    "policyName",
    "SwaggerDocOptions");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ClientCredentials", policy =>
        policy.Requirements.Add(new GrantTypeRequirement(OpenIddictConstants.GrantTypes.ClientCredentials)));

    options.AddPolicy("Password", policy =>
        policy.Requirements.Add(new GrantTypeRequirement(OpenIddictConstants.GrantTypes.Password)));
});

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, GrantTypeAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>(); 
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddGMHttpClient<IPermissionsService, GMAPIClientOptions>(
    builder.Configuration.GetSection("ApiServices:PermissionsService"),
    "PermissionsService");

// S1075: the issuer below is a fixed localhost URL on purpose — it points at the demo identity
// server this sample runs against locally, so it stays inline rather than moving to configuration.
#pragma warning disable S1075
builder.Services.AddOpenIddict()

    // Register the OpenIddict client components.
    .AddClient(options =>
    {
        // Allow grant_type=password to be negotiated.
        options.AllowPasswordFlow();
        
        // Allow grant_type=client_credentials to be negotiated.
        options.AllowClientCredentialsFlow();

        // Disable token storage, which is not necessary for non-interactive flows like
        // grant_type=password, grant_type=client_credentials or grant_type=refresh_token.
        options.DisableTokenStorage();

        // Register the System.Net.Http integration and use the identity of the current
        // assembly as a more specific user agent, which can be useful when dealing with
        // providers that use the user agent as a way to throttle requests (e.g Reddit).
        options.UseSystemNetHttp()
            .SetProductInformation(typeof(Program).Assembly);

        // Add a client registration without a client identifier/secret attached.
        options.AddRegistration(new OpenIddictClientRegistration
        {
            Issuer = new Uri("https://localhost:7104/", UriKind.Absolute),
            // ClientId = "MyIdentityClient",
            // ClientSecret = "YOUR_CLIENT_SECRET"
        });
    })
    .AddValidation(options =>
    {
        options.SetIssuer("https://localhost:7104/"); // must match your server's issuer
        options.UseSystemNetHttp();
        options.UseAspNetCore(); // Enables integration with ASP.NET Core authentication

        // Optional: use introspection or local validation depending on your setup
        //options.UseLocalServer(); // If validation happens on the same server
        options.UseIntrospection()
            .SetClientId("MyIdentityClient")
            .SetClientSecret("YOUR_CLIENT_SECRET");  // for remote token validation
    });
#pragma warning restore S1075
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITokenManager, TokenManager>();

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseGMServices();

await app.RunAsync();