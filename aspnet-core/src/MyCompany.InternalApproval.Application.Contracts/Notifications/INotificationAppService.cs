using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MyCompany.InternalApproval.Notifications;

public interface INotificationAppService : IApplicationService
{
    // Lấy danh sách thông báo của người dùng
    Task<PagedResultDto<ApprovalNotificationDto>> GetListAsync(GetNotificationListInput input);

    // Lấy số lượng thông báo chưa đọc
    Task<int> GetUnreadCountAsync();

    // Đánh dấu một thông báo là đã đọc
    Task MarkAsReadAsync(Guid id);

    // Đánh dấu tất cả thông báo là đã đọc
    Task MarkAllAsReadAsync();
}
