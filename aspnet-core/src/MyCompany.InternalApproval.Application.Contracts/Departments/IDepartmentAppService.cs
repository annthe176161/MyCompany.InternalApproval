using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace MyCompany.InternalApproval.Departments;

public interface IDepartmentAppService : IApplicationService
{
    Task<PagedResultDto<DepartmentDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<DepartmentDto> GetAsync(Guid id);
    Task<DepartmentDto> CreateAsync(CreateUpdateDepartmentDto input);
    Task<DepartmentDto> UpdateAsync(Guid id, CreateUpdateDepartmentDto input);
    Task DeleteAsync(Guid id);

    Task<List<DepartmentMemberDto>> GetMembersAsync(Guid departmentId);
    Task AddMemberAsync(Guid departmentId, AddDepartmentMemberDto input);
    Task UpdateMemberAsync(Guid departmentId, Guid userId, UpdateDepartmentMemberDto input);
    Task RemoveMemberAsync(Guid departmentId, Guid userId);
    Task SetManagerAsync(Guid departmentId, Guid? userId);
    Task<List<DepartmentUserLookupDto>> GetAvailableUsersAsync(Guid departmentId);
}

