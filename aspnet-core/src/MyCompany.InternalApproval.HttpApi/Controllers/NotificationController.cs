using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCompany.InternalApproval.Notifications;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace MyCompany.InternalApproval.Controllers;

/// <summary>
/// Controller cung cấp các API thông báo cho người dùng.
/// </summary>
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

    /// <summary>
    /// Lấy danh sách thông báo của người dùng đăng nhập.
    /// GET /api/app/notification
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<ApprovalNotificationDto>> GetListAsync(GetNotificationListInput input)
    {
        return _notificationAppService.GetListAsync(input);
    }

    /// <summary>
    /// Lấy số lượng thông báo chưa đọc.
    /// GET /api/app/notification/unread-count
    /// </summary>
    [HttpGet("unread-count")]
    public virtual Task<int> GetUnreadCountAsync()
    {
        return _notificationAppService.GetUnreadCountAsync();
    }

    /// <summary>
    /// Đánh dấu một thông báo là đã đọc.
    /// POST /api/app/notification/{id}/mark-as-read
    /// </summary>
    [HttpPost("{id}/mark-as-read")]
    public virtual Task MarkAsReadAsync(Guid id)
    {
        return _notificationAppService.MarkAsReadAsync(id);
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo là đã đọc.
    /// POST /api/app/notification/mark-all-as-read
    /// </summary>
    [HttpPost("mark-all-as-read")]
    public virtual Task MarkAllAsReadAsync()
    {
        return _notificationAppService.MarkAllAsReadAsync();
    }
}

