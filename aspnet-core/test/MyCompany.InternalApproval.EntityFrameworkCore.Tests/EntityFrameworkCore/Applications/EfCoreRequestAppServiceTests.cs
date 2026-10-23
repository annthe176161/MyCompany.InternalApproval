using MyCompany.InternalApproval.Requests;
using Xunit;

namespace MyCompany.InternalApproval.EntityFrameworkCore.Applications;

[Collection(InternalApprovalTestConsts.CollectionDefinitionName)]
public class EfCoreRequestAppServiceTests : RequestAppService_Tests<InternalApprovalEntityFrameworkCoreTestModule>
{

}

