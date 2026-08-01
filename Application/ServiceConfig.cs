using System.Reflection;

using Application.Features.Accounts.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class ServiceConfig
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(x => x.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            services.AddScoped<IExternalPlayerLoginWorkflow, ExternalPlayerLoginWorkflow>();

            return services;
        }
    }
}
