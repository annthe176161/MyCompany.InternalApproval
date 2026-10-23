using Volo.Abp.Modularity;

namespace MyCompany.InternalApproval;

[DependsOn(
    typeof(InternalApprovalApplicationModule),
    typeof(InternalApprovalDomainTestModule)
)]
public class InternalApprovalApplicationTestModule : AbpModule
{

}
