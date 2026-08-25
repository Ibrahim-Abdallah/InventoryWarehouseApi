namespace InventoryWarehouseApi.Domain.Entities;

public sealed class LowStockAlert
{
    private LowStockAlert() { }
    public LowStockAlert(Guid id, Guid thresholdId, decimal thresholdQuantity, decimal availableQuantity, DateTimeOffset triggeredAtUtc)
    {
        if(id==Guid.Empty) throw new ArgumentException("Alert ID is required.",nameof(id));
        if(thresholdId==Guid.Empty) throw new ArgumentException("Threshold ID is required.",nameof(thresholdId));
        ValidateQuantity(thresholdQuantity); ValidateQuantity(availableQuantity);
        Id=id; LowStockThresholdId=thresholdId; ThresholdQuantity=thresholdQuantity; AvailableQuantity=availableQuantity;
        TriggeredAtUtc=triggeredAtUtc.ToUniversalTime(); LastObservedAtUtc=TriggeredAtUtc;
    }
    public Guid Id { get; private set; }
    public Guid LowStockThresholdId { get; private set; }
    public decimal ThresholdQuantity { get; private set; }
    public decimal AvailableQuantity { get; private set; }
    public DateTimeOffset TriggeredAtUtc { get; private set; }
    public DateTimeOffset LastObservedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public bool IsActive => ResolvedAtUtc is null;
    public void Observe(decimal thresholdQuantity, decimal availableQuantity, DateTimeOffset observedAtUtc)
    { EnsureActive(); ValidateQuantity(thresholdQuantity); ValidateQuantity(availableQuantity); observedAtUtc=observedAtUtc.ToUniversalTime(); if(observedAtUtc<TriggeredAtUtc) throw new ArgumentOutOfRangeException(nameof(observedAtUtc)); ThresholdQuantity=thresholdQuantity; AvailableQuantity=availableQuantity; LastObservedAtUtc=observedAtUtc; }
    public void Resolve(decimal availableQuantity, DateTimeOffset resolvedAtUtc)
    { EnsureActive(); ValidateQuantity(availableQuantity); resolvedAtUtc=resolvedAtUtc.ToUniversalTime(); if(resolvedAtUtc<TriggeredAtUtc) throw new ArgumentOutOfRangeException(nameof(resolvedAtUtc)); AvailableQuantity=availableQuantity; LastObservedAtUtc=resolvedAtUtc; ResolvedAtUtc=resolvedAtUtc; }
    private void EnsureActive(){ if(!IsActive) throw new InvalidOperationException("Resolved alerts cannot be changed."); }
    private static void ValidateQuantity(decimal value){ if(value<0) throw new ArgumentOutOfRangeException(nameof(value)); if(decimal.Round(value,3)!=value) throw new ArgumentException("Quantity cannot have more than 3 decimal places.",nameof(value)); }
}
