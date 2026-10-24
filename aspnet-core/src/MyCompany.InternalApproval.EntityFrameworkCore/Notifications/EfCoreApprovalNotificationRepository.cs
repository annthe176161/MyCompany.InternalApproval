using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyCompany.InternalApproval.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MyCompany.InternalApproval.Notifications;

// Triển khai EF Core Repository cho thực thể Thông báo phê duyệt (ApprovalNotification)
public class EfCoreApprovalNotificationRepository : EfCoreRepository<InternalApprovalDbContext, ApprovalNotification, Guid>, IApprovalNotificationRepository
{
    public EfCoreApprovalNotificationRepository(IDbContextProvider<InternalApprovalDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    // Lấy danh sách thông báo của người nhận có phân trang, sắp xếp mới nhất
    public virtual async Task<List<ApprovalNotification>> GetListByReceiverAsync(
        Guid receiverId,
        int maxResultCount = 15,
        int skipCount = 0,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Where(x => x.ReceiverId == receiverId)
            .OrderByDescending(x => x.CreationTime)
            .PageBy(skipCount, maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    // Đếm tổng số thông báo của người nhận
    public virtual async Task<long> GetCountByReceiverAsync(
        Guid receiverId,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Where(x => x.ReceiverId == receiverId)
            .LongCountAsync(GetCancellationToken(cancellationToken));
    }

    // Đếm số lượng thông báo chưa đọc của người nhận
    public virtual async Task<int> GetUnreadCountAsync(
        Guid receiverId,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Where(x => x.ReceiverId == receiverId && !x.IsRead)
            .CountAsync(GetCancellationToken(cancellationToken));
    }

    // Lấy toàn bộ thông báo chưa đọc của người nhận
    public virtual async Task<List<ApprovalNotification>> GetUnreadListAsync(
        Guid receiverId,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Where(x => x.ReceiverId == receiverId && !x.IsRead)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }
}

