using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace InventoryWarehouseApi.Api.Authentication;
internal sealed class JwtAccessTokenService(IOptions<JwtOptions> options):IAccessTokenService
{
 private readonly JwtOptions _o=options.Value;public int RefreshTokenDays=>_o.RefreshTokenDays;
 public AccessTokenResult Create(User user,DateTimeOffset now){var expires=now.AddMinutes(_o.AccessTokenMinutes);var claims=new List<Claim>{new(JwtRegisteredClaimNames.Sub,user.Id.ToString()),new(JwtRegisteredClaimNames.Email,user.Email),new(ClaimTypes.Name,user.DisplayName),new(ClaimTypes.Role,user.Role.ToString()),new(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())};claims.AddRange(Permissions.ForRole(user.Role).Select(x=>new Claim(Permissions.ClaimType,x)));var jwt=new JwtSecurityToken(_o.Issuer,_o.Audience,claims,now.UtcDateTime,expires.UtcDateTime,new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.SigningKey)),SecurityAlgorithms.HmacSha256));return new(new JwtSecurityTokenHandler().WriteToken(jwt),expires);}
}
