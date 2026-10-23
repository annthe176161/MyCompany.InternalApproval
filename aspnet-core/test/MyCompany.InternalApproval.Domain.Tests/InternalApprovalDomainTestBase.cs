using Volo.Abp.Modularity;

namespace MyCompany.InternalApproval;

/* Inherit from this class for your domain layer tests. */
public abstract class InternalApprovalDomainTestBase<TStartupModule> : InternalApprovalTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
