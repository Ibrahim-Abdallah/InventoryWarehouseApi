namespace InventoryWarehouseApi.Api;

using InventoryWarehouseApi.Api.BackgroundJobs;
using System.Security.Claims;
using System.Text;
using InventoryWarehouseApi.Api.Authentication;
using InventoryWarehouseApi.Application.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "Inventory Warehouse API";
            document.Info.Version = "v1";
            document.Info.Description = "A production-style inventory and warehouse management REST API with transactional stock workflows, JWT authorization, low-stock monitoring, and Dapper reporting.";
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", Description = "JWT Bearer access token" };
            return Task.CompletedTask;
        }));
        services.AddHealthChecks();
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddOptions<LowStockMonitoringOptions>().BindConfiguration(LowStockMonitoringOptions.SectionName)
            .Validate(x => !x.Enabled || x.IntervalSeconds is >= 5 and <= 86400, "IntervalSeconds must be between 5 and 86400 when enabled.").ValidateOnStart();
        services.AddHostedService<LowStockMonitoringWorker>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddOptions<DevelopmentAdminOptions>().BindConfiguration(DevelopmentAdminOptions.SectionName);
        services.AddHostedService<DevelopmentAdminBootstrap>();
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.SectionName)
            .Validate(x=>!string.IsNullOrWhiteSpace(x.Issuer),"Issuer is required.")
            .Validate(x=>!string.IsNullOrWhiteSpace(x.Audience),"Audience is required.")
            .Validate(x=>Encoding.UTF8.GetByteCount(x.SigningKey)>=32,"SigningKey must be at least 32 bytes.")
            .Validate(x=>x.AccessTokenMinutes is >=1 and <=1440,"AccessTokenMinutes must be between 1 and 1440.")
            .Validate(x=>x.RefreshTokenDays is >=1 and <=90,"RefreshTokenDays must be between 1 and 90.").ValidateOnStart();
        JwtOptions jwt=configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()??new();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>
        {
            o.MapInboundClaims=false;
            o.TokenValidationParameters=new(){ValidateIssuer=true,ValidIssuer=jwt.Issuer,ValidateAudience=true,ValidAudience=jwt.Audience,ValidateIssuerSigningKey=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),ValidateLifetime=true,ClockSkew=TimeSpan.FromSeconds(30),NameClaimType=ClaimTypes.Name,RoleClaimType=ClaimTypes.Role};
            o.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers.WWWAuthenticate = JwtBearerDefaults.AuthenticationScheme;
                    await WriteAuthenticationProblemAsync(context.HttpContext, "Unauthorized",
                        "Authentication is required or the supplied access token is invalid.");
                },
                OnForbidden = context => WriteAuthenticationProblemAsync(context.HttpContext, "Forbidden",
                    "The authenticated user does not have permission to perform this operation.")
            };
        });
        services.AddAuthorization(o=>
        {o.FallbackPolicy=new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
         o.AddPolicy(AuthorizationPolicies.CatalogRead,p=>p.RequireClaim(Permissions.ClaimType,Permissions.CatalogRead));o.AddPolicy(AuthorizationPolicies.CatalogManage,p=>p.RequireClaim(Permissions.ClaimType,Permissions.CatalogManage));o.AddPolicy(AuthorizationPolicies.InventoryRead,p=>p.RequireClaim(Permissions.ClaimType,Permissions.InventoryRead));o.AddPolicy(AuthorizationPolicies.InventoryOperate,p=>p.RequireClaim(Permissions.ClaimType,Permissions.InventoryOperate));o.AddPolicy(AuthorizationPolicies.InventoryAdjust,p=>p.RequireClaim(Permissions.ClaimType,Permissions.InventoryAdjust));o.AddPolicy(AuthorizationPolicies.LowStockManage,p=>p.RequireClaim(Permissions.ClaimType,Permissions.LowStockManage));o.AddPolicy(AuthorizationPolicies.AdminOnly,p=>p.RequireRole("Admin"));});

        return services;
    }

    private static async Task WriteAuthenticationProblemAsync(HttpContext context, string title, string detail)
    {
        ProblemDetails problem = new() { Status = context.Response.StatusCode, Title = title, Detail = detail };
        IProblemDetailsService service = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await service.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
    }
}
