using System.Security.Cryptography;
using System.Text;
using InventoryWarehouseApi.Application.Authentication;
namespace InventoryWarehouseApi.Infrastructure.Security;
internal sealed class RefreshTokenService:IRefreshTokenService
{
 public RefreshTokenValue Generate(){string raw=Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)).TrimEnd('=').Replace('+','-').Replace('/','_');return new(raw,Hash(raw));}
 public string Hash(string rawToken)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
