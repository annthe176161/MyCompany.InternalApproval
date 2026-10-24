using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MyCompany.InternalApproval.Departments;

// Giao diện dịch vụ quản lý phòng ban và phân bổ nhân sự
public interface IDepartmentAppService : IApplicationService
{
    // Lấy danh sách phòng ban có phân trang và sắp xếp
    Task<PagedResultDto<DepartmentDto>> GetListAsync(PagedAndSortedResultRequestDto input);

    // Lấy thông tin chi tiết một phòng ban theo Id
    Task<DepartmentDto> GetAsync(Guid id);

    // Tạo mới phòng ban
    Task<DepartmentDto> CreateAsync(CreateUpdateDepartmentDto input);

    // Cập nhật thông tin phòng ban
    Task<DepartmentDto> UpdateAsync(Guid id, CreateUpdateDepartmentDto input);

    // Xóa phòng ban
    Task DeleteAsync(Guid id);

    // Lấy danh sách thành viên thuộc phòng ban
    Task<List<DepartmentMemberDto>> GetMembersAsync(Guid departmentId);

    // Thêm nhân viên vào phòng ban
    Task AddMemberAsync(Guid departmentId, AddDepartmentMemberDto input);

    // Cập nhật chức danh hoặc ngày vào làm của thành viên
    Task UpdateMemberAsync(Guid departmentId, Guid userId, UpdateDepartmentMemberDto input);

    // Gỡ nhân viên khỏi phòng ban
    Task RemoveMemberAsync(Guid departmentId, Guid userId);

    // Bổ nhiệm hoặc hủy bổ nhiệm trưởng phòng
    Task SetManagerAsync(Guid departmentId, Guid? userId);

    // Lấy danh sách người dùng khả dụng để thêm vào phòng ban
    Task<List<DepartmentUserLookupDto>> GetAvailableUsersAsync(Guid departmentId);
}
