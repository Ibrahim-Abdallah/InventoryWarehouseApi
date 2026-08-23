using FluentValidation;
using InventoryWarehouseApi.Application.Common;
using InventoryWarehouseApi.Domain.Entities;

namespace InventoryWarehouseApi.Application.Warehouses;

internal sealed class WarehouseService(IWarehouseRepository repository, IValidator<CreateWarehouseRequest> createValidator,
    IValidator<UpdateWarehouseRequest> updateValidator, IValidator<WarehouseQuery> queryValidator) : IWarehouseService
{
    public async Task<PagedResult<WarehouseResponse>> ListAsync(WarehouseQuery query, CancellationToken cancellationToken)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        PagedResult<Warehouse> page = await repository.ListAsync(query, cancellationToken);
        return new(page.Items.Select(Map).ToList(), page.PageNumber, page.PageSize, page.TotalCount);
    }
    public async Task<WarehouseResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await repository.GetAsync(id, false, cancellationToken) ?? throw new NotFoundException("Warehouse was not found."));
    public async Task<WarehouseResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        string code = Warehouse.NormalizeCode(request.Code);
        if (await repository.CodeExistsAsync(code, null, cancellationToken))
            throw new ConflictException($"A warehouse with code '{code}' already exists.");
        Warehouse warehouse = new(request.Code, request.Name, request.Description, DateTimeOffset.UtcNow);
        await repository.AddAsync(warehouse, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(warehouse);
    }
    public async Task<WarehouseResponse> UpdateAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        Warehouse warehouse = await repository.GetAsync(id, true, cancellationToken) ?? throw new NotFoundException("Warehouse was not found.");
        string code = Warehouse.NormalizeCode(request.Code);
        if (await repository.CodeExistsAsync(code, id, cancellationToken))
            throw new ConflictException($"A warehouse with code '{code}' already exists.");
        warehouse.Update(request.Code, request.Name, request.Description, request.IsActive, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken);
        return Map(warehouse);
    }
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Warehouse warehouse = await repository.GetAsync(id, true, cancellationToken) ?? throw new NotFoundException("Warehouse was not found.");
        if (await repository.HasLocationsAsync(id, cancellationToken))
            throw new ConflictException("The warehouse cannot be deleted because it has warehouse locations.");
        repository.Remove(warehouse);
        await repository.SaveChangesAsync(cancellationToken);
    }
    private static WarehouseResponse Map(Warehouse x) => new(x.Id, x.Code, x.Name, x.Description, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
}
