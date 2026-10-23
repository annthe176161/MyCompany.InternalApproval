using Volo.Abp.Modularity;

namespace MyCompany.InternalApproval;

[DependsOn(
    typeof(InternalApprovalDomainModule),
    typeof(InternalApprovalTestBaseModule)
)]
public class InternalApprovalDomainTestModule : AbpModule
{

}
