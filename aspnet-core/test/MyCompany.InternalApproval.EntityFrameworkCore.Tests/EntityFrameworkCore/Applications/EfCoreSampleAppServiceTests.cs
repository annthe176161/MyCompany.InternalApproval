using MyCompany.InternalApproval.Samples;
using Xunit;

namespace MyCompany.InternalApproval.EntityFrameworkCore.Applications;

[Collection(InternalApprovalTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<InternalApprovalEntityFrameworkCoreTestModule>
{

}
