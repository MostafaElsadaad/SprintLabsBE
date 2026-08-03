using System.Text;

using FirebaseAdmin;

using Google.Apis.Auth.OAuth2;

using Domain.Repositories;
using Domain.Services;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore.Sqlite;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Options;

namespace Infrastructure
{
    public static class ServiceConfig
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            #region Db Context
            string connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddScoped<IPlayerRepository, PlayerRepository>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            //services.AddDbContext<ApplicationDbContext>(options =>
            //   {
            //       options.UseSqlite(connectionString);
            //});

            #endregion

            #region Binding
            services.Configure<BitbucketOptions>(configuration.GetSection("Bitbucket"));
            services.Configure<GoogleCloudStorageOptions>(configuration.GetSection("GoogleCloudStorage"));
            services.Configure<JWTOptions>(configuration.GetSection("JWTOptions"));
            services.Configure<TeacherAuthenticationOptions>(configuration.GetSection("TeacherAuthentication"));
            services.Configure<EmailOptions>(configuration.GetSection("Email"));
            services.Configure<FrontendOptions>(configuration.GetSection("Frontend"));
            services.AddOptions<FirebaseAuthenticationOptions>()
                .Bind(configuration.GetSection("Authentication:Firebase"))
                .Validate(x => !string.IsNullOrWhiteSpace(x.ProjectId), "Authentication:Firebase:ProjectId is required.");
            #endregion  

            #region Repositories

            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            #endregion

            #region Services    
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IFileDownloadService, FileDownloadService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICommunityAccessService, CommunityAccessService>();
            services.AddScoped<ICommunityLoginActivationService, CommunityLoginActivationService>();
            services.AddScoped<IXpCalculationService, XpCalculationService>();
            services.AddScoped<ILevelProgressionService, LevelProgressionService>();
            services.AddScoped<IRpRankCalculationService, RpRankCalculationService>();
            services.AddScoped<IApisSyncService,ApiSyncService>();
            services.AddScoped<IGoogleAuthenticationService, GoogleAuthenticationService>();
            services.AddSingleton<FirebaseApp>(serviceProvider =>
            {
                var firebaseOptions = serviceProvider.GetRequiredService<IOptions<FirebaseAuthenticationOptions>>().Value;
                var logger = serviceProvider.GetRequiredService<ILogger<FirebaseAuthenticationService>>();
                if (string.IsNullOrWhiteSpace(firebaseOptions.ProjectId))
                {
                    throw new InvalidOperationException("Firebase project ID is not configured.");
                }

                try
                {
                    return FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.GetApplicationDefault(),
                        ProjectId = firebaseOptions.ProjectId
                    });
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Firebase Admin initialization failed for configured project {FirebaseProjectId}.",
                        firebaseOptions.ProjectId);
                    throw new GenericException(Shared.Enums.ErrorCode.Failure, ErrorMessage.InvalidAccessToken, System.Net.HttpStatusCode.Unauthorized);
                }
            });
            services.AddSingleton<IFirebaseAuthenticationService, FirebaseAuthenticationService>();
            services.AddScoped<IAccessTokenService, AccessTokenService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IEmailService, SmtpEmailService>();
            services.AddScoped<ITeacherIdentityService, TeacherIdentityService>();
            services.AddScoped<ITeacherInvitationService, TeacherInvitationService>();
            services.AddSingleton<IGoogleCloudStorageService, GoogleCloudStorageService>();
            #endregion

            #region Authentication
            var jwtSettings = configuration.GetSection("JWTOptions").Get<JWTOptions>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }
                )
                .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,

                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds),

                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                }
                );

            #endregion

            return services;
        }
    }
}
