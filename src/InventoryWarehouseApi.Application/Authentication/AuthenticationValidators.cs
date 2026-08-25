using FluentValidation;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.Authentication;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest> { public LoginRequestValidator() { RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(User.EmailMaxLength); RuleFor(x => x.Password).NotEmpty().MaximumLength(128); } }
public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest> { public RefreshTokenRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512); }
public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest> { public LogoutRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512); }
public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(User.EmailMaxLength);
        RuleFor(x => x.DisplayName).NotEmpty().Must(x => x is null || x.Trim().Length <= User.DisplayNameMaxLength);
        RuleFor(x => x.Password).NotEmpty().Length(12, 128).Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]").Matches("[^a-zA-Z0-9]");
        RuleFor(x => x.Role).IsInEnum();
    }
}
public sealed class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest> { public UpdateUserRoleRequestValidator() => RuleFor(x => x.Role).IsInEnum(); }
public sealed class UserQueryValidator : AbstractValidator<UserQuery> { public UserQueryValidator() { RuleFor(x => x.PageNumber).GreaterThan(0); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); RuleFor(x => x.Role).IsInEnum().When(x => x.Role.HasValue); } }
