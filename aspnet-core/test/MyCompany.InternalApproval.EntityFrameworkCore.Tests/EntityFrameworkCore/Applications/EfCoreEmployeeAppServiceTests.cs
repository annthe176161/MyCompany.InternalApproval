using MyCompany.InternalApproval.Employees;
using Xunit;

namespace MyCompany.InternalApproval.EntityFrameworkCore.Applications;

[Collection(InternalApprovalTestConsts.CollectionDefinitionName)]
public class EfCoreEmployeeAppServiceTests : EmployeeAppService_Tests<InternalApprovalEntityFrameworkCoreTestModule>
{

}
