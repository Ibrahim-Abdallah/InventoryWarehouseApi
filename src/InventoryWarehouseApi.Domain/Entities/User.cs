using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Domain.Entities;

public sealed class User
{
    public const int EmailMaxLength = 128;
    public const int DisplayNameMaxLength = 128;
    private User() { }

    public User(Guid id, string email, string displayName, string passwordHash, UserRole role,
        DateTimeOffset createdAtUtc, DateTimeOffset? updatedAtUtc = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(id));
        Email = NormalizeEmail(email);
        if (Email.Length > EmailMaxLength) throw new ArgumentException($"Email cannot exceed {EmailMaxLength} characters.", nameof(email));
        NormalizedEmail = Email.ToUpperInvariant();
        DisplayName = Required(displayName, nameof(displayName));
        if (DisplayName.Length > DisplayNameMaxLength) throw new ArgumentException($"Display name cannot exceed {DisplayNameMaxLength} characters.", nameof(displayName));
        PasswordHash = Required(passwordHash, nameof(passwordHash));
        EnsureRole(role);
        Id = id;
        Role = role;
        IsActive = true;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        UpdatedAtUtc = (updatedAtUtc ?? createdAtUtc).ToUniversalTime();
        if (UpdatedAtUtc < CreatedAtUtc) throw new ArgumentException("Updated timestamp cannot precede creation timestamp.", nameof(updatedAtUtc));
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void ChangeRole(UserRole role, DateTimeOffset now) { EnsureRole(role); Role = role; Touch(now); }
    public void Activate(DateTimeOffset now) { IsActive = true; Touch(now); }
    public void Deactivate(DateTimeOffset now) { IsActive = false; Touch(now); }
    public static string NormalizeEmail(string email) => Required(email, nameof(email)).ToLowerInvariant();
    private void Touch(DateTimeOffset now) { var utc = now.ToUniversalTime(); if (utc < CreatedAtUtc) throw new ArgumentException("Updated timestamp cannot precede creation timestamp.", nameof(now)); UpdatedAtUtc = utc; }
    private static void EnsureRole(UserRole role) { if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role), "Role is not supported."); }
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
}
