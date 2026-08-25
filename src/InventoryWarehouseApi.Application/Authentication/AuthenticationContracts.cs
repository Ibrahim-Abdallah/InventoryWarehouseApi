using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Application.Authentication;

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record AuthenticatedUserResponse(Guid Id, string Email, string DisplayName, string Role, IReadOnlyList<string> Permissions);
public sealed record AuthTokenResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc, AuthenticatedUserResponse User);
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);
public sealed record RefreshTokenValue(string RawToken, string Hash);

public interface IPasswordHashService { string HashPassword(User user, string password); bool VerifyPassword(User user, string hash, string password); }
public interface IRefreshTokenService { RefreshTokenValue Generate(); string Hash(string rawToken); }
public interface IAccessTokenService { int RefreshTokenDays { get; } AccessTokenResult Create(User user, DateTimeOffset now); }
public interface ICurrentUser { bool IsAuthenticated { get; } Guid UserId { get; } string Email { get; } string DisplayName { get; } string Role { get; } }
public interface IAuthenticationRepository
{
    Task<User?> FindUserByEmailAsync(string normalizedEmail, bool tracking, CancellationToken ct);
    Task<User?> FindUserAsync(Guid id, bool tracking, CancellationToken ct);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task<AuthRotationResult?> RotateAsync(string hash, Func<User, RefreshToken> replacementFactory, DateTimeOffset now, CancellationToken ct);
    Task RevokeAsync(string hash, DateTimeOffset now, CancellationToken ct);
}
public sealed record AuthRotationResult(User User, RefreshToken Replacement);
public interface IAuthenticationService
{
    Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct);
    AuthenticatedUserResponse Me();
}

public sealed record CreateUserRequest(string Email, string DisplayName, string Password, UserRole Role);
public sealed record UpdateUserRoleRequest(UserRole Role);
public sealed record UpdateUserStatusRequest(bool IsActive);
public sealed record UserQuery(int PageNumber = 1, int PageSize = 20, string? Search = null, UserRole? Role = null, bool? IsActive = null);
public sealed record UserResponse(Guid Id, string Email, string DisplayName, UserRole Role, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public interface IUserRepository
{
    Task<User?> GetAsync(Guid id, bool tracking, CancellationToken ct);
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken ct);
    Task<int> ActiveAdminCountAsync(Guid excludingId, CancellationToken ct);
    Task<PagedResult<User>> ListAsync(UserQuery query, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task RevokeActiveTokensAsync(Guid userId, DateTimeOffset now, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<PagedResult<UserResponse>> ListAsync(UserQuery query, CancellationToken ct);
    Task<UserResponse> GetAsync(Guid id, CancellationToken ct);
    Task<UserResponse> ChangeRoleAsync(Guid id, UpdateUserRoleRequest request, CancellationToken ct);
    Task<UserResponse> ChangeStatusAsync(Guid id, UpdateUserStatusRequest request, CancellationToken ct);
}
