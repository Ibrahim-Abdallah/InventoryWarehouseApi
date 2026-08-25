using InventoryWarehouseApi.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace InventoryWarehouseApi.Api.Controllers;
[ApiController][Route("api/auth")]
public sealed class AuthController(IAuthenticationService service):ControllerBase
{
 [AllowAnonymous][HttpPost("login")]public Task<AuthTokenResponse>Login(LoginRequest request,CancellationToken ct)=>service.LoginAsync(request,ct);
 [AllowAnonymous][HttpPost("refresh")]public Task<AuthTokenResponse>Refresh(RefreshTokenRequest request,CancellationToken ct)=>service.RefreshAsync(request,ct);
 [AllowAnonymous][HttpPost("logout")]public async Task<IActionResult>Logout(LogoutRequest request,CancellationToken ct){await service.LogoutAsync(request,ct);return NoContent();}
 [HttpGet("me")]public AuthenticatedUserResponse Me()=>service.Me();
}
