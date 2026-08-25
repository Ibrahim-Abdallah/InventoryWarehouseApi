using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Domain.Entities;
using Microsoft.AspNetCore.Identity;
namespace InventoryWarehouseApi.Infrastructure.Security;
internal sealed class PasswordHashService : IPasswordHashService
{
 private readonly PasswordHasher<User> _hasher=new();
 public string HashPassword(User user,string password)=>_hasher.HashPassword(user,password);
 public bool VerifyPassword(User user,string hash,string password)=>_hasher.VerifyHashedPassword(user,hash,password)!=PasswordVerificationResult.Failed;
}
