using MyCompany.InternalApproval.Notifications;
using Xunit;

namespace MyCompany.InternalApproval.EntityFrameworkCore.Applications;

[Collection(InternalApprovalTestConsts.CollectionDefinitionName)]
public class EfCoreNotificationAppServiceTests : NotificationAppService_Tests<InternalApprovalEntityFrameworkCoreTestModule>
{

}

