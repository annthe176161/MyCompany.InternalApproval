using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Notifications;

/// <summary>
/// Tham số phân trang lấy danh sách thông báo.
/// </summary>
public class GetNotificationListInput : PagedAndSortedResultRequestDto
{
    public GetNotificationListInput()
    {
        MaxResultCount = 15;
    }
}

