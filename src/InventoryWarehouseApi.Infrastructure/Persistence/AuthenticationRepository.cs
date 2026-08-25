using System.Data;
using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace InventoryWarehouseApi.Infrastructure.Persistence;
internal sealed class AuthenticationRepository(InventoryWarehouseDbContext db):IAuthenticationRepository
{
 public Task<User?> FindUserByEmailAsync(string normalizedEmail,bool tracking,CancellationToken ct)=>(tracking?db.Users:db.Users.AsNoTracking()).SingleOrDefaultAsync(x=>x.NormalizedEmail==normalizedEmail,ct);
 public Task<User?> FindUserAsync(Guid id,bool tracking,CancellationToken ct)=>(tracking?db.Users:db.Users.AsNoTracking()).SingleOrDefaultAsync(x=>x.Id==id,ct);
 public Task AddRefreshTokenAsync(RefreshToken token,CancellationToken ct)=>db.RefreshTokens.AddAsync(token,ct).AsTask();
 public Task SaveChangesAsync(CancellationToken ct)=>db.SaveChangesAsync(ct);
 public async Task<AuthRotationResult?> RotateAsync(string hash,Func<User,RefreshToken> factory,DateTimeOffset now,CancellationToken ct)
 {await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);var old=await db.RefreshTokens.SingleOrDefaultAsync(x=>x.TokenHash==hash,ct);if(old is null||!old.IsActive(now)){await tx.RollbackAsync(ct);return null;}var user=await db.Users.SingleAsync(x=>x.Id==old.UserId,ct);if(!user.IsActive){await tx.RollbackAsync(ct);return null;}old.Revoke(now);var replacement=factory(user);db.RefreshTokens.Add(replacement);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return new(user,replacement);}
 public async Task RevokeAsync(string hash,DateTimeOffset now,CancellationToken ct){var token=await db.RefreshTokens.SingleOrDefaultAsync(x=>x.TokenHash==hash,ct);if(token is not null&&token.IsActive(now)){token.Revoke(now);await db.SaveChangesAsync(ct);}}
}
