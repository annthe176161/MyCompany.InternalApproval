using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCompany.InternalApproval.Notifications;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Controllers;

// Controller cung cấp các API thông báo phê duyệt cho người dùng (/api/app/notification)
[Authorize]
[RemoteService(Name = "Default")]
[Route("api/app/notification")]
public class NotificationController : InternalApprovalController, INotificationAppService
{
    private readonly INotificationAppService _notificationAppService;

    public NotificationController(INotificationAppService notificationAppService)
    {
        _notificationAppService = notificationAppService;
    }

    // Lấy danh sách thông báo của người dùng đăng nhập
    [HttpGet]
    public virtual Task<PagedResultDto<ApprovalNotificationDto>> GetListAsync(GetNotificationListInput input)
    {
        return _notificationAppService.GetListAsync(input);
    }

    // Lấy số lượng thông báo chưa đọc
    [HttpGet("unread-count")]
    public virtual Task<int> GetUnreadCountAsync()
    {
        return _notificationAppService.GetUnreadCountAsync();
    }

    // Đánh dấu một thông báo là đã đọc
    [HttpPost("{id}/mark-as-read")]
    public virtual Task MarkAsReadAsync(Guid id)
    {
        return _notificationAppService.MarkAsReadAsync(id);
    }

    // Đánh dấu tất cả thông báo là đã đọc
    [HttpPost("mark-all-as-read")]
    public virtual Task MarkAllAsReadAsync()
    {
        return _notificationAppService.MarkAllAsReadAsync();
    }
}
