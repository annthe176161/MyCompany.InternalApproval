using System.Threading.Tasks;

namespace MyCompany.InternalApproval.Data;

public interface IInternalApprovalDbSchemaMigrator
{
    Task MigrateAsync();
}
