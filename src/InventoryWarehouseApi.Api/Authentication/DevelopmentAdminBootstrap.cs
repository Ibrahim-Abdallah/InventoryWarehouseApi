using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using InventoryWarehouseApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace InventoryWarehouseApi.Api.Authentication;
internal sealed class DevelopmentAdminBootstrap(IServiceProvider services,IHostEnvironment environment,IOptions<DevelopmentAdminOptions> options,ILogger<DevelopmentAdminBootstrap> logger):IHostedService
{
 public async Task StartAsync(CancellationToken ct){var o=options.Value;if(!environment.IsDevelopment()||!o.Enabled)return;if(string.IsNullOrWhiteSpace(o.Password))throw new InvalidOperationException("Authentication:DevelopmentAdmin:Password is required when bootstrap is enabled.");using var scope=services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<InventoryWarehouseDbContext>();var normalized=User.NormalizeEmail(o.Email).ToUpperInvariant();if(await db.Users.AnyAsync(x=>x.NormalizedEmail==normalized,ct))return;var hashing=scope.ServiceProvider.GetRequiredService<IPasswordHashService>();var now=DateTimeOffset.UtcNow;var draft=new User(Guid.NewGuid(),o.Email,o.DisplayName,"pending",UserRole.Admin,now);var user=new User(draft.Id,draft.Email,draft.DisplayName,hashing.HashPassword(draft,o.Password),UserRole.Admin,now);db.Users.Add(user);await db.SaveChangesAsync(ct);logger.LogInformation("Development Admin bootstrap created user {UserId}.",user.Id);}
 public Task StopAsync(CancellationToken ct)=>Task.CompletedTask;
}
