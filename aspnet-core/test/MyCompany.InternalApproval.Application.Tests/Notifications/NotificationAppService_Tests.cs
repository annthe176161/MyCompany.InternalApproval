using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MyCompany.InternalApproval.Notifications;
using MyCompany.InternalApproval.Requests;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Xunit;

namespace MyCompany.InternalApproval.Notifications;

public abstract class NotificationAppService_Tests<TStartupModule> : InternalApprovalApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly INotificationAppService NotificationAppService;
    protected readonly IRepository<ApprovalNotification, Guid> NotificationRepository;
    protected readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    protected readonly IGuidGenerator GuidGenerator;

    protected readonly Guid EmployeeId = Guid.NewGuid();
    protected readonly string EmployeeName = "employee";

    protected readonly Guid ManagerId = Guid.NewGuid();
    protected readonly string ManagerName = "manager";

    protected NotificationAppService_Tests()
    {
        NotificationAppService = GetRequiredService<INotificationAppService>();
        NotificationRepository = GetRequiredService<IRepository<ApprovalNotification, Guid>>();
        CurrentPrincipalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        GuidGenerator = GetRequiredService<IGuidGenerator>();
    }

    protected IDisposable LoginAs(Guid userId, string userName, string role = "Employee")
    {
        var claims = new[]
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString()),
            new Claim(AbpClaimTypes.UserName, userName),
            new Claim(AbpClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        return CurrentPrincipalAccessor.Change(new ClaimsPrincipal(identity));
    }

    // Test: Lấy danh sách thông báo gửi cho người nhận
    [Fact]
    public async Task Should_Get_Notification_List_For_Receiver()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            var notif = new ApprovalNotification(
                GuidGenerator.Create(),
                Guid.NewGuid(),
                "Đề xuất nghỉ phép đã duyệt",
                RequestStatus.Approved,
                EmployeeId,
                ManagerId,
                "Đề xuất của bạn đã được phê duyệt."
            );
            await NotificationRepository.InsertAsync(notif, autoSave: true);

            // Act
            var result = await NotificationAppService.GetListAsync(new GetNotificationListInput
            {
                MaxResultCount = 10,
                SkipCount = 0
            });

            // Assert
            result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
            result.Items.Any(x => x.Title == "Đề xuất nghỉ phép đã duyệt").ShouldBeTrue();
        }
    }

    // Test: Đếm chính xác số lượng thông báo chưa đọc
    [Fact]
    public async Task Should_Get_Unread_Count()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange: Thêm 2 thông báo chưa đọc
            await NotificationRepository.InsertAsync(new ApprovalNotification(
                GuidGenerator.Create(),
                Guid.NewGuid(),
                "Thông báo 1",
                RequestStatus.Approved,
                EmployeeId,
                ManagerId,
                "Nội dung 1"
            ), autoSave: true);

            await NotificationRepository.InsertAsync(new ApprovalNotification(
                GuidGenerator.Create(),
                Guid.NewGuid(),
                "Thông báo 2",
                RequestStatus.Rejected,
                EmployeeId,
                ManagerId,
                "Nội dung 2"
            ), autoSave: true);

            // Act
            var count = await NotificationAppService.GetUnreadCountAsync();

            // Assert
            count.ShouldBeGreaterThanOrEqualTo(2);
        }
    }

    // Test: Đánh dấu một thông báo là đã đọc
    [Fact]
    public async Task Should_Mark_Notification_As_Read()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            var notif = new ApprovalNotification(
                GuidGenerator.Create(),
                Guid.NewGuid(),
                "Đề xuất được duyệt",
                RequestStatus.Approved,
                EmployeeId,
                ManagerId,
                "Chi tiết phê duyệt"
            );
            await NotificationRepository.InsertAsync(notif, autoSave: true);

            // Act
            await NotificationAppService.MarkAsReadAsync(notif.Id);

            // Assert
            var updated = await NotificationRepository.GetAsync(notif.Id);
            updated.IsRead.ShouldBeTrue();
        }
    }

    // Test: Đánh dấu tất cả thông báo là đã đọc
    [Fact]
    public async Task Should_Mark_All_Notifications_As_Read()
    {
        using (LoginAs(EmployeeId, EmployeeName))
        {
            // Arrange
            await NotificationRepository.InsertAsync(new ApprovalNotification(
                GuidGenerator.Create(),
                Guid.NewGuid(),
                "Thông báo A",
                RequestStatus.Approved,
                EmployeeId,
                ManagerId,
                "A"
            ), autoSave: true);

            await NotificationRepository.InsertAsync(new ApprovalNotification(
                GuidGenerator.Create(),
                Guid.NewGuid(),
                "Thông báo B",
                RequestStatus.Rejected,
                EmployeeId,
                ManagerId,
                "B"
            ), autoSave: true);

            // Act
            await NotificationAppService.MarkAllAsReadAsync();

            // Assert
            var unreadCount = await NotificationAppService.GetUnreadCountAsync();
            unreadCount.ShouldBe(0);
        }
    }

    // Test: Khi gửi đề xuất, Job tạo thông báo chờ duyệt gửi tới Admin
    [Fact]
    public async Task Should_Create_Notification_For_Admin_When_Request_Submitted()
    {
        var job = GetRequiredService<SendApprovalNotificationJob>();
        var userAppService = GetRequiredService<IIdentityUserAppService>();

        var users = await userAppService.GetListAsync(new GetIdentityUsersInput());
        var admin = users.Items.FirstOrDefault(u => u.UserName == "admin");
        admin.ShouldNotBeNull();

        var requestId = Guid.NewGuid();

        // Act: Thực thi background job với status Submitted bên trong UnitOfWork
        await WithUnitOfWorkAsync(async () =>
        {
            await job.ExecuteAsync(new SendApprovalNotificationArgs
            {
                RequestId = requestId,
                Title = "Đơn xin nghỉ phép",
                Status = RequestStatus.Submitted,
                CreatorId = EmployeeId
            });
        });

        // Assert: Admin phải nhận được thông báo với trạng thái Submitted
        using (LoginAs(admin.Id, admin.UserName, "admin"))
        {
            var notifs = await NotificationAppService.GetListAsync(new GetNotificationListInput());
            notifs.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
            var item = notifs.Items.FirstOrDefault(x => x.RequestId == requestId);
            item.ShouldNotBeNull();
            item.Status.ShouldBe(RequestStatus.Submitted);
            item.Message.ShouldContain("Đơn xin nghỉ phép");
            item.Message.ShouldContain("Vui lòng kiểm tra và phê duyệt");
        }
    }
}

