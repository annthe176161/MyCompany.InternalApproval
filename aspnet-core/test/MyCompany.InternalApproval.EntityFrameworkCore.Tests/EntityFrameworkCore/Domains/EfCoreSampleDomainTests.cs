using MyCompany.InternalApproval.Samples;
using Xunit;

namespace MyCompany.InternalApproval.EntityFrameworkCore.Domains;

[Collection(InternalApprovalTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<InternalApprovalEntityFrameworkCoreTestModule>
{

}
