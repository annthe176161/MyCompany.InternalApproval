using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyCompany.InternalApproval.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace MyCompany.InternalApproval.Requests;

// Triển khai EF Core Repository cho thực thể Đề xuất (Request)
public class EfCoreRequestRepository : EfCoreRepository<InternalApprovalDbContext, Request, Guid>, IRequestRepository
{
    public EfCoreRequestRepository(IDbContextProvider<InternalApprovalDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    // Lấy danh sách đề xuất kèm bình luận (có lọc, sắp xếp và phân trang)
    public virtual async Task<List<Request>> GetListWithDetailsAsync(
        string? filter = null,
        RequestStatus? status = null,
        RequestType? type = null,
        Guid? creatorId = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Include(x => x.Comments)
            .WhereIf(!string.IsNullOrWhiteSpace(filter), x => x.Title.Contains(filter!) || (x.Description != null && x.Description.Contains(filter!)))
            .WhereIf(status.HasValue, x => x.Status == status!.Value)
            .WhereIf(type.HasValue, x => x.Type == type!.Value)
            .WhereIf(creatorId.HasValue, x => x.CreatorId == creatorId!.Value)
            .OrderBy(string.IsNullOrWhiteSpace(sorting) ? $"{nameof(Request.CreationTime)} desc" : sorting)
            .PageBy(skipCount, maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    // Đếm tổng số đề xuất thỏa mãn điều kiện lọc
    public virtual async Task<long> GetCountAsync(
        string? filter = null,
        RequestStatus? status = null,
        RequestType? type = null,
        Guid? creatorId = null,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .WhereIf(!string.IsNullOrWhiteSpace(filter), x => x.Title.Contains(filter!) || (x.Description != null && x.Description.Contains(filter!)))
            .WhereIf(status.HasValue, x => x.Status == status!.Value)
            .WhereIf(type.HasValue, x => x.Type == type!.Value)
            .WhereIf(creatorId.HasValue, x => x.CreatorId == creatorId!.Value)
            .LongCountAsync(GetCancellationToken(cancellationToken));
    }

    // Lấy chi tiết đề xuất kèm danh sách bình luận theo Id
    public virtual async Task<Request?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == id, GetCancellationToken(cancellationToken));
    }

    // Tự động nạp kèm danh sách bình luận khi gọi WithDetailsAsync
    public override async Task<IQueryable<Request>> WithDetailsAsync()
    {
        return (await GetQueryableAsync()).Include(x => x.Comments);
    }
}
