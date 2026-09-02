using API.Exceptions;

using Application;

using Asp.Versioning;
using Asp.Versioning;

using Dsquares.Logging;

using Infrastructure;
using Infrastructure.DataAccess;
using Infrastructure.Identity;
using Infrastructure.Seed;

using Domain.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Shared.Options;

using Swashbuckle.AspNetCore.Filters;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.WebHost.UseUrls("http://localhost:5005", "https://localhost:7005");

builder.Services.AddControllers();


#region Swagger
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    setup =>
    {
        var jwtSecurityScheme = new OpenApiSecurityScheme
        {
            BearerFormat = "JWT",
            Name = "JWT Authentication",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

            Reference = new OpenApiReference
            {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };

        setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

        setup.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { jwtSecurityScheme, Array.Empty<string>() }
        });
    }
);
#endregion

var teacherAuthenticationSettings = builder.Configuration
    .GetSection("TeacherAuthentication")
    .Get<TeacherAuthenticationOptions>() ?? new TeacherAuthenticationOptions();

builder.Services.Configure<EmailConfirmationTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(teacherAuthenticationSettings.ConfirmationTokenLifetimeHours));
builder.Services.Configure<PasswordResetTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(teacherAuthenticationSettings.PasswordResetTokenLifetimeHours));

builder.Services.AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = teacherAuthenticationSettings.MaxFailedAccessAttempts;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(teacherAuthenticationSettings.LockoutMinutes);
        options.Tokens.EmailConfirmationTokenProvider = "TeacherEmailConfirmation";
        options.Tokens.PasswordResetTokenProvider = "TeacherPasswordReset";
    })
    .AddRoles<IdentityRole<long>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddTokenProvider<EmailConfirmationTokenProvider>("TeacherEmailConfirmation")
    .AddTokenProvider<PasswordResetTokenProvider>("TeacherPasswordReset");
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHttpClient();

// API Versioning
builder.Services.AddApiVersioning(o =>
{
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.ReportApiVersions = true;
    o.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});


UnifiedLogger.UseLogger(builder);



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});



builder.Services.AddHealthChecks();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
}


UnifiedLogger.UseMiddleware(app);
app.UseMiddleware<GlobalExceptionHandler>();


app.UseSwagger();
app.UseSwaggerUI();


#region Auto Migrate
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // Check if there are any pending migrations
        var pendingMigrations = dbContext.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            dbContext.Database.Migrate();
        }

    }
    catch (Exception ex)
    {
        //change color to Red
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(ex.Message);
    }
}

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("DemoCommunitySeed:Enabled"))
{
    using var scope = app.Services.CreateScope();
    await DemoCommunitySeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
        scope.ServiceProvider.GetRequiredService<UserManager<User>>());
}

using (var scope = app.Services.CreateScope())
{
    var guard = scope.ServiceProvider.GetRequiredService<IDevelopmentAuthenticationGuard>();
    if (guard.CanSeed)
    {
        var seededPlayers = await DevelopmentPlayerSeeder.SeedAsync(
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
            scope.ServiceProvider.GetRequiredService<UserManager<User>>());
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentPlayerSeeder");
        logger.LogInformation("Ensured {DevelopmentPlayerCount} development player accounts.", seededPlayers.Count);
        foreach (var seededPlayer in seededPlayers)
        {
            logger.LogInformation(
                "Ensured development account {DevelopmentAccountKey} with UserId {UserId} and PlayerProfileId {PlayerProfileId}.",
                seededPlayer.AccountKey,
                seededPlayer.UserId,
                seededPlayer.PlayerProfileId);
        }
    }
}
#endregion

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    await PlatformAdminSeeder.SeedAsync(userManager, builder.Configuration);
}


app.UseHttpsRedirection();

app.UseAuthentication();

app.UseCors("AllowAll");

app.UseAuthorization();

app.UseHealthChecks("/");

app.MapControllers();

app.Run();
