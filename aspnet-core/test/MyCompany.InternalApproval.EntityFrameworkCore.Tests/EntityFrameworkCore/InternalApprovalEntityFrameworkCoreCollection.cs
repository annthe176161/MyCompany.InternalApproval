using Xunit;

namespace MyCompany.InternalApproval.EntityFrameworkCore;

[CollectionDefinition(InternalApprovalTestConsts.CollectionDefinitionName)]
public class InternalApprovalEntityFrameworkCoreCollection : ICollectionFixture<InternalApprovalEntityFrameworkCoreFixture>
{

}
