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
            #endregion  

            #region Repositories

            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            #endregion

            #region Services    
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IFileDownloadService, FileDownloadService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICommunityAccessService, CommunityAccessService>();
            services.AddScoped<IApisSyncService,ApiSyncService>();
            services.AddScoped<IGoogleAuthenticationService, GoogleAuthenticationService>();
            services.AddSingleton<IGoogleCloudStorageService, GoogleCloudStorageService>();
            #endregion

            #region Authentication
            var jwtSettings = configuration.GetSection("JWTOptions").Get<JWTOptions>();
            var googleSettings = configuration.GetSection("GoogleOptions").Get<GoogleOptions>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }
                )
                .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,

                    ValidateAudience = false,
                    ValidateIssuerSigningKey = false,
                    ValidateLifetime = true,

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
