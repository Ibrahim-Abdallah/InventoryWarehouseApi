using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.Authentication;

internal sealed class AuthenticationService(IAuthenticationRepository repository, IPasswordHashService passwords,
    IRefreshTokenService refreshTokens, IAccessTokenService accessTokens, ICurrentUser currentUser,
    IValidator<LoginRequest> loginValidator, IValidator<RefreshTokenRequest> refreshValidator,
    IValidator<LogoutRequest> logoutValidator) : IAuthenticationService
{
    public async Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await loginValidator.ValidateAndThrowAsync(request, ct);
        User? user = await repository.FindUserByEmailAsync(User.NormalizeEmail(request.Email).ToUpperInvariant(), true, ct);
        if (user is null || !user.IsActive || !passwords.VerifyPassword(user, user.PasswordHash, request.Password)) throw new UnauthorizedException("Invalid email or password.");
        var now = DateTimeOffset.UtcNow; RefreshTokenValue value = refreshTokens.Generate();
        RefreshToken token = new(Guid.NewGuid(), user.Id, value.Hash, now, now.AddDays(accessTokens.RefreshTokenDays));
        await repository.AddRefreshTokenAsync(token, ct); await repository.SaveChangesAsync(ct);
        return Response(user, value.RawToken, token.ExpiresAtUtc, now);
    }
    public async Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        await refreshValidator.ValidateAndThrowAsync(request, ct); var now = DateTimeOffset.UtcNow; RefreshTokenValue value = refreshTokens.Generate();
        AuthRotationResult? result = await repository.RotateAsync(refreshTokens.Hash(request.RefreshToken),
            user => new RefreshToken(Guid.NewGuid(), user.Id, value.Hash, now, now.AddDays(accessTokens.RefreshTokenDays)), now, ct);
        if (result is null) throw new UnauthorizedException("Invalid refresh token.");
        return Response(result.User, value.RawToken, result.Replacement.ExpiresAtUtc, now);
    }
    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct) { await logoutValidator.ValidateAndThrowAsync(request, ct); await repository.RevokeAsync(refreshTokens.Hash(request.RefreshToken), DateTimeOffset.UtcNow, ct); }
    public AuthenticatedUserResponse Me()
    {
        if (!currentUser.IsAuthenticated) throw new UnauthorizedException("Authentication is required.");
        var role = Enum.Parse<Domain.Enums.UserRole>(currentUser.Role); return new(currentUser.UserId, currentUser.Email, currentUser.DisplayName, role.ToString(), Permissions.ForRole(role));
    }
    private AuthTokenResponse Response(User user, string raw, DateTimeOffset refreshExpiry, DateTimeOffset now)
    { var access = accessTokens.Create(user, now); return new(access.Token, access.ExpiresAtUtc, raw, refreshExpiry, new(user.Id, user.Email, user.DisplayName, user.Role.ToString(), Permissions.ForRole(user.Role))); }
}
