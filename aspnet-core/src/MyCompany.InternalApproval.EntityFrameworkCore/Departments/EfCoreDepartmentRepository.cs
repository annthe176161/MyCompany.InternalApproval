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

namespace MyCompany.InternalApproval.Departments;

// Triển khai EF Core Repository cho thực thể Phòng ban (Department)
public class EfCoreDepartmentRepository : EfCoreRepository<InternalApprovalDbContext, Department, Guid>, IDepartmentRepository
{
    public EfCoreDepartmentRepository(IDbContextProvider<InternalApprovalDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    // Lấy danh sách phòng ban kèm danh sách thành viên (có sắp xếp và phân trang)
    public virtual async Task<List<Department>> GetListWithDetailsAsync(
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Include(x => x.Members)
            .OrderBy(string.IsNullOrWhiteSpace(sorting) ? $"{nameof(Department.CreationTime)} desc" : sorting)
            .PageBy(skipCount, maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    // Lấy chi tiết phòng ban kèm thành viên theo Id
    public virtual async Task<Department?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == id, GetCancellationToken(cancellationToken));
    }

    // Tìm phòng ban theo mã phòng ban
    public virtual async Task<Department?> FindByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .FirstOrDefaultAsync(x => x.Code == code, GetCancellationToken(cancellationToken));
    }

    // Tự động nạp kèm danh sách thành viên khi gọi WithDetailsAsync
    public override async Task<IQueryable<Department>> WithDetailsAsync()
    {
        return (await GetQueryableAsync()).Include(x => x.Members);
    }
}

