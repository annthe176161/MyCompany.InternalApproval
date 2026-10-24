using Volo.Abp.Application.Dtos;

namespace MyCompany.InternalApproval.Notifications;

// Tham số phân trang lấy danh sách thông báo
public class GetNotificationListInput : PagedAndSortedResultRequestDto
{
    public GetNotificationListInput()
    {
        MaxResultCount = 15;
    }
}
