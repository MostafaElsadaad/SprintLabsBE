using System.Reflection;
using API.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Swagger;

public class GameServerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.MethodInfo.GetCustomAttributes<AuthorizeAttribute>()
            .Any(x => x.AuthenticationSchemes == GameServerAuthenticationHandler.SchemeName)) return;
        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new() { [new OpenApiSecurityScheme { Reference = new OpenApiReference
            { Type = ReferenceType.SecurityScheme, Id = GameServerAuthenticationHandler.SchemeName } }] = Array.Empty<string>() }
        };
    }
}
