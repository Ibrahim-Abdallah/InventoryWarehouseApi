using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InventoryWarehouseApi.Application.Authentication;
namespace InventoryWarehouseApi.Api.Authentication;
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor):ICurrentUser
{private ClaimsPrincipal User=>accessor.HttpContext?.User??new();public bool IsAuthenticated=>User.Identity?.IsAuthenticated==true;public Guid UserId=>Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)??User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:Guid.Empty;public string Email=>User.FindFirstValue(JwtRegisteredClaimNames.Email)??User.FindFirstValue(ClaimTypes.Email)??"";public string DisplayName=>User.FindFirstValue(ClaimTypes.Name)??"";public string Role=>User.FindFirstValue(ClaimTypes.Role)??"";}
