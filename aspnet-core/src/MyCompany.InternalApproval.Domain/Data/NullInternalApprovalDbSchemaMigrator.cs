using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace MyCompany.InternalApproval.Data;

/* This is used if database provider does't define
 * IInternalApprovalDbSchemaMigrator implementation.
 */
public class NullInternalApprovalDbSchemaMigrator : IInternalApprovalDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
