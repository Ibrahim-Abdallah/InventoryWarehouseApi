using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace InventoryWarehouseApi.Infrastructure.Persistence;
internal sealed class UserRepository(InventoryWarehouseDbContext db):IUserRepository
{
 public Task<User?> GetAsync(Guid id,bool tracking,CancellationToken ct)=>(tracking?db.Users:db.Users.AsNoTracking()).SingleOrDefaultAsync(x=>x.Id==id,ct);
 public Task<bool>EmailExistsAsync(string n,CancellationToken ct)=>db.Users.AnyAsync(x=>x.NormalizedEmail==n,ct);
 public Task<int>ActiveAdminCountAsync(Guid excludingId,CancellationToken ct)=>db.Users.CountAsync(x=>x.Id!=excludingId&&x.IsActive&&x.Role==UserRole.Admin,ct);
 public async Task<PagedResult<User>>ListAsync(UserQuery q,CancellationToken ct){IQueryable<User>x=db.Users.AsNoTracking();if(!string.IsNullOrWhiteSpace(q.Search)){var s=q.Search.Trim();x=x.Where(u=>u.Email.Contains(s)||u.DisplayName.Contains(s));}if(q.Role.HasValue)x=x.Where(u=>u.Role==q.Role);if(q.IsActive.HasValue)x=x.Where(u=>u.IsActive==q.IsActive);var count=await x.CountAsync(ct);var items=await x.OrderByDescending(u=>u.CreatedAtUtc).ThenByDescending(u=>u.Id).Skip((q.PageNumber-1)*q.PageSize).Take(q.PageSize).ToListAsync(ct);return new(items,q.PageNumber,q.PageSize,count);}
 public Task AddAsync(User u,CancellationToken ct)=>db.Users.AddAsync(u,ct).AsTask();
 public async Task RevokeActiveTokensAsync(Guid id,DateTimeOffset now,CancellationToken ct){var tokens=await db.RefreshTokens.Where(x=>x.UserId==id&&x.RevokedAtUtc==null&&x.ExpiresAtUtc>now).ToListAsync(ct);foreach(var t in tokens)t.Revoke(now);}
 public async Task SaveChangesAsync(CancellationToken ct){try{await db.SaveChangesAsync(ct);}catch(DbUpdateException ex)when(UniqueConstraintDetector.Matches(ex,"UX_Users_NormalizedEmail","Users.NormalizedEmail")){throw new ConflictException("A user with this email already exists.");}}
}
