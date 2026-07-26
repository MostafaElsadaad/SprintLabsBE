using System.Text;

using Domain.Repositories;
using Domain.Services;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore.Sqlite;
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
