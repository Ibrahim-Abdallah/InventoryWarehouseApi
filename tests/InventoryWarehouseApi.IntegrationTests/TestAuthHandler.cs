using System.Security.Claims;
using System.Text.Encodings.Web;
using InventoryWarehouseApi.Application.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
namespace InventoryWarehouseApi.IntegrationTests;
internal sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
 public const string SchemeName="IntegrationTest";public static readonly Guid UserId=Guid.Parse("11111111-1111-1111-1111-111111111111");
 protected override Task<AuthenticateResult>HandleAuthenticateAsync(){var claims=new List<Claim>{new("sub",UserId.ToString()),new("email","integration-admin@tests.local"),new(ClaimTypes.Name,"Integration Admin"),new(ClaimTypes.Role,"Admin")};claims.AddRange(Permissions.All.Select(x=>new Claim(Permissions.ClaimType,x)));var principal=new ClaimsPrincipal(new ClaimsIdentity(claims,SchemeName,ClaimTypes.Name,ClaimTypes.Role));return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal,SchemeName)));}
}
