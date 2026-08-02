using GM.API.Startup;
using GM.Identity;
using GM.Identity.Sample.Infrastructure;
using GM.Identity.Sample.Persistence;
using GM.Identity.Sample.Persistence.Context;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = ProgramExtension.CreateGMBuilder(args);

builder.Services.ConfigureGMServices(
    builder.Configuration,
    "policyName",
    "SwaggerDocOptions");

builder.Services.AddGMIdentity();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication();
    // .AddAuthentication(options =>
    // {
    //     options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    //     options.DefaultChallengeScheme = "Google";
    // })
    // .AddCookie(options =>
    // {
    //     options.Cookie.SameSite = SameSiteMode.Lax; // important for external redirects
    //     options.Cookie.SecurePolicy = CookieSecurePolicy.None; // ok for localhost
    //     options.Cookie.Path = "/";
    // })
    // .AddGoogle("Google", options =>
    // {
    //     options.ClientId = "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com";
    //     options.ClientSecret = "YOUR_GOOGLE_CLIENT_SECRET";
    //     options.CallbackPath = new PathString("/connect/google/callback");
    //
    //     // Optional
    //     options.SaveTokens = true;
    // })
    // // .AddFacebook("Facebook", options =>
    // // {
    // //     options.ClientId = "";
    // //     options.ClientSecret = "";
    // //     options.CallbackPath = "";
    // // })
    ;


var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseGMServices();



using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (context != null)
        {
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                await context.Database.MigrateAsync();
            }

            var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContextSeed>>();
            if (logger != null)
                new ApplicationDbContextSeed().SeedAsync(context, logger).Wait();
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
    }
}

app.Run();