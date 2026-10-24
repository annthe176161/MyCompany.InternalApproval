using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MyCompany.InternalApproval.Requests;

// Repository chuyên biệt cho thực thể Đề xuất (Request)
public interface IRequestRepository : IRepository<Request, Guid>
{
    // Lấy danh sách đề xuất kèm danh sách bình luận (có lọc, sắp xếp và phân trang)
    Task<List<Request>> GetListWithDetailsAsync(
        string? filter = null,
        RequestStatus? status = null,
        RequestType? type = null,
        Guid? creatorId = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default);

    // Đếm tổng số đề xuất thỏa mãn điều kiện lọc
    Task<long> GetCountAsync(
        string? filter = null,
        RequestStatus? status = null,
        RequestType? type = null,
        Guid? creatorId = null,
        CancellationToken cancellationToken = default);

    // Lấy chi tiết đề xuất kèm bình luận theo Id
    Task<Request?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
