using InventoryWarehouseApi.Application.Authentication;
using InventoryWarehouseApi.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace InventoryWarehouseApi.Api.Controllers;
[ApiController][Route("api/users")][Authorize(Policy=AuthorizationPolicies.AdminOnly)]
public sealed class UsersController(IUserService service):ControllerBase
{
 [HttpPost]public async Task<ActionResult<UserResponse>>Create(CreateUserRequest request,CancellationToken ct){var u=await service.CreateAsync(request,ct);return CreatedAtAction(nameof(Get),new{id=u.Id},u);}
 [HttpGet]public Task<PagedResult<UserResponse>>List([FromQuery]UserQuery q,CancellationToken ct)=>service.ListAsync(q,ct);
 [HttpGet("{id:guid}")]public Task<UserResponse>Get(Guid id,CancellationToken ct)=>service.GetAsync(id,ct);
 [HttpPut("{id:guid}/role")]public Task<UserResponse>Role(Guid id,UpdateUserRoleRequest request,CancellationToken ct)=>service.ChangeRoleAsync(id,request,ct);
 [HttpPut("{id:guid}/status")]public Task<UserResponse>Status(Guid id,UpdateUserStatusRequest request,CancellationToken ct)=>service.ChangeStatusAsync(id,request,ct);
}
