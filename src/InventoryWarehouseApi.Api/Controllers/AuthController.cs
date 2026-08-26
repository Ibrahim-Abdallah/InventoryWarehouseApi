using InventoryWarehouseApi.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace InventoryWarehouseApi.Api.Controllers;
[ApiController][Route("api/auth")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError,"application/problem+json",Description="An unexpected server error occurred.")]
public sealed class AuthController(IAuthenticationService service):ControllerBase
{
 [AllowAnonymous][HttpPost("login")]
 [EndpointSummary("Log in")][EndpointDescription("Authenticates an active user and returns a short-lived JWT access token with a rotating refresh token.")]
 [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK,Description="Returns access and refresh tokens with the authenticated user.")]
 [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,"application/problem+json",Description="The request failed validation.")]
 [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized,"application/problem+json",Description="The credentials are invalid or the user is inactive.")]
 public Task<AuthTokenResponse>Login(LoginRequest request,CancellationToken ct)=>service.LoginAsync(request,ct);

 [AllowAnonymous][HttpPost("refresh")]
 [EndpointSummary("Rotate refresh token")][EndpointDescription("Validates and revokes the supplied refresh token, then returns a new access token and refresh token pair atomically.")]
 [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK,Description="Returns the rotated token pair.")]
 [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,"application/problem+json",Description="The request failed validation.")]
 [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized,"application/problem+json",Description="The refresh token is invalid, expired, revoked, or belongs to an inactive user.")]
 public Task<AuthTokenResponse>Refresh(RefreshTokenRequest request,CancellationToken ct)=>service.RefreshAsync(request,ct);

 [AllowAnonymous][HttpPost("logout")]
 [EndpointSummary("Log out")][EndpointDescription("Revokes the supplied refresh token when it exists; repeated logout requests remain safe.")]
 [ProducesResponseType(StatusCodes.Status204NoContent,Description="Logs out and revokes the supplied refresh token.")]
 [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest,"application/problem+json",Description="The request failed validation.")]
 public async Task<IActionResult>Logout(LogoutRequest request,CancellationToken ct){await service.LogoutAsync(request,ct);return NoContent();}

 [HttpGet("me")]
 [EndpointSummary("Get current user")][EndpointDescription("Returns the authenticated user's identity, role, and effective permissions from the access token.")]
 [ProducesResponseType<AuthenticatedUserResponse>(StatusCodes.Status200OK,Description="Returns the current authenticated user.")]
 [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized,"application/problem+json",Description="Authentication is required or the access token is invalid.")]
 public AuthenticatedUserResponse Me()=>service.Me();
}
