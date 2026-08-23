using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.WarehouseLocations;

internal sealed class WarehouseLocationService(IWarehouseLocationRepository repository,
    IValidator<CreateWarehouseLocationRequest> createValidator, IValidator<UpdateWarehouseLocationRequest> updateValidator,
    IValidator<WarehouseLocationQuery> queryValidator) : IWarehouseLocationService
{
    public async Task<PagedResult<WarehouseLocationResponse>> ListAsync(Guid warehouseId, WarehouseLocationQuery query, CancellationToken ct)
    {
        await EnsureWarehouseAsync(warehouseId, ct);
        await queryValidator.ValidateAndThrowAsync(query, ct);
        PagedResult<WarehouseLocation> page = await repository.ListAsync(warehouseId, query, ct);
        return new(page.Items.Select(Map).ToList(), page.PageNumber, page.PageSize, page.TotalCount);
    }
    public async Task<WarehouseLocationResponse> GetAsync(Guid warehouseId, Guid id, CancellationToken ct)
    {
        await EnsureWarehouseAsync(warehouseId, ct);
        return Map(await repository.GetAsync(warehouseId, id, false, ct) ?? throw new NotFoundException("Warehouse location was not found."));
    }
    public async Task<WarehouseLocationResponse> CreateAsync(Guid warehouseId, CreateWarehouseLocationRequest request, CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        await EnsureWarehouseAsync(warehouseId, ct);
        string code = WarehouseLocation.NormalizeCode(request.Code);
        if (await repository.CodeExistsAsync(warehouseId, code, null, ct)) throw new ConflictException($"A location with code '{code}' already exists in this warehouse.");
        WarehouseLocation location = new(warehouseId, request.Code, request.Name, request.Description, DateTimeOffset.UtcNow);
        await repository.AddAsync(location, ct);
        await repository.SaveChangesAsync(ct);
        return Map(location);
    }
    public async Task<WarehouseLocationResponse> UpdateAsync(Guid warehouseId, Guid id, UpdateWarehouseLocationRequest request, CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);
        await EnsureWarehouseAsync(warehouseId, ct);
        WarehouseLocation location = await repository.GetAsync(warehouseId, id, true, ct) ?? throw new NotFoundException("Warehouse location was not found.");
        string code = WarehouseLocation.NormalizeCode(request.Code);
        if (await repository.CodeExistsAsync(warehouseId, code, id, ct)) throw new ConflictException($"A location with code '{code}' already exists in this warehouse.");
        location.Update(request.Code, request.Name, request.Description, request.IsActive, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(ct);
        return Map(location);
    }
    public async Task DeleteAsync(Guid warehouseId, Guid id, CancellationToken ct)
    {
        await EnsureWarehouseAsync(warehouseId, ct);
        WarehouseLocation location = await repository.GetAsync(warehouseId, id, true, ct) ?? throw new NotFoundException("Warehouse location was not found.");
        if (await repository.HasInventoryAsync(warehouseId, id, ct)) throw new ConflictException("The warehouse location cannot be deleted because it has inventory balances.");
        repository.Remove(location);
        await repository.SaveChangesAsync(ct);
    }
    private async Task EnsureWarehouseAsync(Guid id, CancellationToken ct)
    { if (!await repository.WarehouseExistsAsync(id, ct)) throw new NotFoundException("Warehouse was not found."); }
    private static WarehouseLocationResponse Map(WarehouseLocation x) => new(x.Id, x.WarehouseId, x.Code, x.Name,
        x.Description, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
}
