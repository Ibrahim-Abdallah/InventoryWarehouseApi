using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace InventoryWarehouseApi.Infrastructure.Reporting;

public enum ReportingDialect { SqlServer, Sqlite }
public interface IReportingConnectionFactory { DbConnection CreateConnection(); ReportingDialect Dialect { get; } }
internal sealed class SqlServerReportingConnectionFactory(string connectionString):IReportingConnectionFactory
{
    public ReportingDialect Dialect=>ReportingDialect.SqlServer;
    public DbConnection CreateConnection()=>new SqlConnection(connectionString);
}
