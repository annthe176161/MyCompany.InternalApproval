using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace MyCompany.InternalApproval.Departments;

// Repository chuyên biệt cho thực thể Phòng ban (Department)
public interface IDepartmentRepository : IRepository<Department, Guid>
{
    // Lấy danh sách phòng ban kèm danh sách thành viên (có sắp xếp và phân trang)
    Task<List<Department>> GetListWithDetailsAsync(
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default);

    // Lấy chi tiết phòng ban kèm danh sách thành viên theo Id
    Task<Department?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Tìm phòng ban theo mã phòng ban
    Task<Department?> FindByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);
}

