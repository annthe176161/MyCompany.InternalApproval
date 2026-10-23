using MyCompany.InternalApproval.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace MyCompany.InternalApproval.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(InternalApprovalEntityFrameworkCoreModule),
    typeof(InternalApprovalApplicationContractsModule)
    )]
public class InternalApprovalDbMigratorModule : AbpModule
{
}
