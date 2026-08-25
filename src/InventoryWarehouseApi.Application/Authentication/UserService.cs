using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;
using InventoryWarehouseApi.Domain.Enums;

namespace InventoryWarehouseApi.Application.Authentication;

internal sealed class UserService(IUserRepository repository, IPasswordHashService passwords,
    IValidator<CreateUserRequest> createValidator, IValidator<UpdateUserRoleRequest> roleValidator,
    IValidator<UserQuery> queryValidator) : IUserService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct) { await createValidator.ValidateAndThrowAsync(request, ct); var normalized = User.NormalizeEmail(request.Email).ToUpperInvariant(); if (await repository.EmailExistsAsync(normalized, ct)) throw new ConflictException("A user with this email already exists."); var now=DateTimeOffset.UtcNow; var user=new User(Guid.NewGuid(),request.Email,request.DisplayName,"pending",request.Role,now); var hash=passwords.HashPassword(user,request.Password); user=new User(user.Id,user.Email,user.DisplayName,hash,user.Role,now); await repository.AddAsync(user,ct); await repository.SaveChangesAsync(ct); return Map(user); }
    public async Task<PagedResult<UserResponse>> ListAsync(UserQuery query,CancellationToken ct) { await queryValidator.ValidateAndThrowAsync(query,ct); var p=await repository.ListAsync(query,ct); return new(p.Items.Select(Map).ToList(),p.PageNumber,p.PageSize,p.TotalCount); }
    public async Task<UserResponse> GetAsync(Guid id,CancellationToken ct)=>Map(await repository.GetAsync(id,false,ct)??throw new NotFoundException("User was not found."));
    public async Task<UserResponse> ChangeRoleAsync(Guid id,UpdateUserRoleRequest request,CancellationToken ct) { await roleValidator.ValidateAndThrowAsync(request,ct); var u=await repository.GetAsync(id,true,ct)??throw new NotFoundException("User was not found."); if(u.IsActive&&u.Role==UserRole.Admin&&request.Role!=UserRole.Admin&&await repository.ActiveAdminCountAsync(id,ct)==0) throw new ConflictException("At least one active Admin must remain."); if(u.Role!=request.Role){var now=DateTimeOffset.UtcNow;u.ChangeRole(request.Role,now);await repository.RevokeActiveTokensAsync(id,now,ct);await repository.SaveChangesAsync(ct);}return Map(u); }
    public async Task<UserResponse> ChangeStatusAsync(Guid id,UpdateUserStatusRequest request,CancellationToken ct) { var u=await repository.GetAsync(id,true,ct)??throw new NotFoundException("User was not found."); if(!request.IsActive&&u.IsActive&&u.Role==UserRole.Admin&&await repository.ActiveAdminCountAsync(id,ct)==0)throw new ConflictException("At least one active Admin must remain.");var now=DateTimeOffset.UtcNow;if(request.IsActive)u.Activate(now);else{u.Deactivate(now);await repository.RevokeActiveTokensAsync(id,now,ct);}await repository.SaveChangesAsync(ct);return Map(u); }
    private static UserResponse Map(User u)=>new(u.Id,u.Email,u.DisplayName,u.Role,u.IsActive,u.CreatedAtUtc,u.UpdatedAtUtc);
}
