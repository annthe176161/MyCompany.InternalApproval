using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MyCompany.InternalApproval.Notifications;

// Repository chuyên biệt cho thực thể Thông báo phê duyệt (ApprovalNotification)
public interface IApprovalNotificationRepository : IRepository<ApprovalNotification, Guid>
{
    // Lấy danh sách thông báo của người nhận có phân trang, sắp xếp mới nhất
    Task<List<ApprovalNotification>> GetListByReceiverAsync(
        Guid receiverId,
        int maxResultCount = 15,
        int skipCount = 0,
        CancellationToken cancellationToken = default);

    // Đếm tổng số thông báo của người nhận
    Task<long> GetCountByReceiverAsync(
        Guid receiverId,
        CancellationToken cancellationToken = default);

    // Đếm số lượng thông báo chưa đọc của người nhận
    Task<int> GetUnreadCountAsync(
        Guid receiverId,
        CancellationToken cancellationToken = default);

    // Lấy toàn bộ thông báo chưa đọc của người nhận
    Task<List<ApprovalNotification>> GetUnreadListAsync(
        Guid receiverId,
        CancellationToken cancellationToken = default);
}

