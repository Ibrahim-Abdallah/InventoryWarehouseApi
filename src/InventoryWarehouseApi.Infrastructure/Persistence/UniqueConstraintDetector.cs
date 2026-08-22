namespace InventoryWarehouseApi.Infrastructure.Persistence;

internal static class UniqueConstraintDetector
{
    public static bool Matches(Exception exception, params string[] signatures)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (signatures.Any(signature => current.Message.Contains(signature, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }
}
