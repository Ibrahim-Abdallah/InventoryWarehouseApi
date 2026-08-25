namespace InventoryWarehouseApi.Domain.Entities;

public sealed class RefreshToken
{
    public const int TokenHashMaxLength = 64;
    private RefreshToken() { }
    public RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("Refresh token ID is required.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        Id = id; UserId = userId; TokenHash = tokenHash.Trim();
        CreatedAtUtc = createdAtUtc.ToUniversalTime(); ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        if (ExpiresAtUtc <= CreatedAtUtc) throw new ArgumentException("Expiry must follow creation.", nameof(expiresAtUtc));
    }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && ExpiresAtUtc > now.ToUniversalTime();
    public void Revoke(DateTimeOffset now) { if (RevokedAtUtc is not null) return; var utc = now.ToUniversalTime(); if (utc < CreatedAtUtc) throw new ArgumentException("Revocation cannot precede creation.", nameof(now)); RevokedAtUtc = utc; }
}
