using Volo.Abp.Modularity;

namespace MyCompany.InternalApproval;

public abstract class InternalApprovalApplicationTestBase<TStartupModule> : InternalApprovalTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
