using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MyCompany.InternalApproval.Departments;
using MyCompany.InternalApproval.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Volo.Abp.Validation;

namespace MyCompany.InternalApproval.Requests;

// Dịch vụ quản lý các đề xuất nội bộ (CRUD, gửi duyệt, phê duyệt, từ chối và thêm bình luận)
[RemoteService(IsEnabled = false)]
[Authorize(InternalApprovalPermissions.Requests.Default)]
public class RequestAppService : InternalApprovalAppService, IRequestAppService
{
    private readonly IRequestRepository _requestRepository;
    private readonly RequestManager _requestManager;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<DepartmentMember, Guid> _memberRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;

    public RequestAppService(
        IRequestRepository requestRepository,
        RequestManager requestManager,
        IRepository<IdentityUser, Guid> userRepository,
        IRepository<DepartmentMember, Guid> memberRepository,
        IRepository<Department, Guid> departmentRepository)
    {
        _requestRepository = requestRepository;
        _requestManager = requestManager;
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _departmentRepository = departmentRepository;
    }

    // Lấy danh sách đề xuất có phân trang và tìm kiếm
    public virtual async Task<PagedResultDto<RequestDto>> GetListAsync(GetRequestListInput input)
    {
        try
        {
            var canApprove = await AuthorizationService.IsGrantedAsync(InternalApprovalPermissions.Requests.Approve);
            var isAdmin = CurrentUser.IsInRole("admin") || string.Equals(CurrentUser.UserName, "admin", StringComparison.OrdinalIgnoreCase);

            Guid? targetCreatorId = input.CreatorId;

            // Nếu người dùng không có quyền phê duyệt và không phải Admin, bắt buộc chỉ lấy đề xuất của tài khoản đăng nhập
            if (!canApprove && !isAdmin)
            {
                targetCreatorId = CurrentUser.Id;
            }

            var totalCount = await _requestRepository.GetCountAsync(
                input.Filter,
                input.Status,
                input.Type,
                targetCreatorId
            );

            var requests = await _requestRepository.GetListWithDetailsAsync(
                input.Filter,
                input.Status,
                input.Type,
                targetCreatorId,
                input.Sorting,
                input.MaxResultCount,
                input.SkipCount
            );

            var userIds = requests
                .Select(x => x.CreatorId)
                .Concat(requests.Select(x => x.ApproverId))
                .Concat(requests.SelectMany(x => x.Comments.Select(c => c.CreatorId)))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            var users = userIds.Any()
                ? await _userRepository.GetListAsync(x => userIds.Contains(x.Id))
                : new List<IdentityUser>();

            var userDict = users.ToDictionary(
                x => x.Id,
                x => !string.IsNullOrWhiteSpace(x.Name) ? $"{x.Name} ({x.UserName})" : x.UserName
            );

            // Lấy thông tin phòng ban của người tạo đề xuất
            var creatorIds = requests
                .Where(x => x.CreatorId.HasValue)
                .Select(x => x.CreatorId!.Value)
                .Distinct()
                .ToList();

            var memberList = creatorIds.Any()
                ? await _memberRepository.GetListAsync(x => creatorIds.Contains(x.UserId))
                : new List<DepartmentMember>();

            var deptIds = memberList.Select(x => x.DepartmentId).Distinct().ToList();
            var depts = deptIds.Any()
                ? await _departmentRepository.GetListAsync(x => deptIds.Contains(x.Id))
                : new List<Department>();

            var deptDict = depts.ToDictionary(x => x.Id, x => (x.Code, x.Name));
            var userDeptDict = memberList
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => deptDict.TryGetValue(g.First().DepartmentId, out var d) ? d : default
                );

            var requestDtos = MapWithExceptionHandling<List<Request>, List<RequestDto>>(requests)
                .Select(dto =>
                {
                    if (dto.CreatorId.HasValue && userDict.TryGetValue(dto.CreatorId.Value, out var creatorName))
                    {
                        dto.CreatorName = creatorName;
                    }

                    if (dto.CreatorId.HasValue && userDeptDict.TryGetValue(dto.CreatorId.Value, out var deptInfo) && !string.IsNullOrWhiteSpace(deptInfo.Name))
                    {
                        dto.DepartmentCode = deptInfo.Code;
                        dto.DepartmentName = deptInfo.Name;
                    }

                    if (dto.ApproverId.HasValue && userDict.TryGetValue(dto.ApproverId.Value, out var approverName))
                    {
                        dto.ApproverName = approverName;
                    }

                    dto.Comments = dto.Comments.Select(commentDto =>
                    {
                        if (commentDto.CreatorId.HasValue && userDict.TryGetValue(commentDto.CreatorId.Value, out var commentCreatorName))
                        {
                            commentDto.CreatorName = commentCreatorName;
                        }
                        return commentDto;
                    }).ToList();

                    return dto;
                }).ToList();

            return new PagedResultDto<RequestDto>(totalCount, requestDtos);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetListAsync: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Lấy chi tiết đề xuất theo Id
    public virtual async Task<RequestDto> GetAsync(Guid id)
    {
        try
        {
            var request = await _requestRepository.GetWithDetailsAsync(id);
            if (request == null)
            {
                throw new EntityNotFoundException(typeof(Request), id);
            }

            var userIds = new[] { request.CreatorId, request.ApproverId }
                .Concat(request.Comments.Select(x => x.CreatorId))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            var users = userIds.Any()
                ? await _userRepository.GetListAsync(x => userIds.Contains(x.Id))
                : new List<IdentityUser>();

            var userDict = users.ToDictionary(
                x => x.Id,
                x => !string.IsNullOrWhiteSpace(x.Name) ? $"{x.Name} ({x.UserName})" : x.UserName
            );

            var dto = MapWithExceptionHandling<Request, RequestDto>(request);

            if (dto.CreatorId.HasValue && userDict.TryGetValue(dto.CreatorId.Value, out var creatorName))
            {
                dto.CreatorName = creatorName;

                var member = await _memberRepository.FirstOrDefaultAsync(x => x.UserId == dto.CreatorId.Value);
                if (member != null)
                {
                    var dept = await _departmentRepository.FindAsync(member.DepartmentId);
                    if (dept != null)
                    {
                        dto.DepartmentCode = dept.Code;
                        dto.DepartmentName = dept.Name;
                    }
                }
            }

            if (dto.ApproverId.HasValue && userDict.TryGetValue(dto.ApproverId.Value, out var approverName))
            {
                dto.ApproverName = approverName;
            }

            dto.Comments = dto.Comments.Select(commentDto =>
            {
                if (commentDto.CreatorId.HasValue && userDict.TryGetValue(commentDto.CreatorId.Value, out var commentCreatorName))
                {
                    commentDto.CreatorName = commentCreatorName;
                }
                return commentDto;
            }).ToList();

            return dto;
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetAsync cho Id {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Tạo mới một đề xuất
    [Authorize(InternalApprovalPermissions.Requests.Create)]
    public virtual async Task<RequestDto> CreateAsync(CreateRequestDto input)
    {
        try
        {
            ValidateAmount(input.Type, input.Amount);

            var request = new Request(
                GuidGenerator.Create(),
                input.Title,
                input.Type,
                input.Description,
                input.Amount,
                input.StartDate,
                input.EndDate
            );

            await _requestRepository.InsertAsync(request, autoSave: true);

            return MapWithExceptionHandling<Request, RequestDto>(request);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong CreateAsync: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Cập nhật thông tin đề xuất
    [Authorize(InternalApprovalPermissions.Requests.Edit)]
    public virtual async Task<RequestDto> UpdateAsync(Guid id, UpdateRequestDto input)
    {
        try
        {
            ValidateAmount(input.Type, input.Amount);

            var request = await _requestRepository.GetAsync(id);

            if (request.CreatorId.HasValue && request.CreatorId != CurrentUser.GetId())
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.OnlyCreatorCanEdit);
            }

            if (request.Status != RequestStatus.Draft)
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.OnlyDraftCanBeEdited);
            }

            request
                .SetTitle(input.Title)
                .SetType(input.Type)
                .SetDescription(input.Description)
                .SetAmount(input.Amount)
                .SetDateRange(input.StartDate, input.EndDate);

            await _requestRepository.UpdateAsync(request, autoSave: true);

            return MapWithExceptionHandling<Request, RequestDto>(request);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong UpdateAsync cho Id {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Xóa đề xuất
    [Authorize(InternalApprovalPermissions.Requests.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        try
        {
            var request = await _requestRepository.GetAsync(id);

            if (request.CreatorId.HasValue && request.CreatorId != CurrentUser.GetId())
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.OnlyCreatorCanDelete);
            }

            if (request.Status != RequestStatus.Draft)
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.OnlyDraftCanBeDeleted);
            }

            await _requestRepository.DeleteAsync(request);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong DeleteAsync cho Id {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Gửi duyệt đề xuất
    public virtual async Task<RequestDto> SubmitAsync(Guid id)
    {
        try
        {
            var request = await _requestRepository.GetAsync(id);

            await _requestManager.SubmitAsync(request, CurrentUser.GetId());
            await _requestRepository.UpdateAsync(request, autoSave: true);

            return MapWithExceptionHandling<Request, RequestDto>(request);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong SubmitAsync cho Id {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Phê duyệt đề xuất
    [Authorize(InternalApprovalPermissions.Requests.Approve)]
    public virtual async Task<RequestDto> ApproveAsync(Guid id, ApproveRequestDto input)
    {
        try
        {
            var request = await _requestRepository.GetWithDetailsAsync(id);
            if (request == null)
            {
                throw new EntityNotFoundException(typeof(Request), id);
            }

            await _requestManager.ApproveAsync(request, CurrentUser.GetId(), input.Note);

            if (!string.IsNullOrWhiteSpace(input.Note))
            {
                request.AddComment(GuidGenerator.Create(), $"[Phê duyệt] {input.Note.Trim()}");
            }

            await _requestRepository.UpdateAsync(request, autoSave: true);

            return await GetAsync(request.Id);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong ApproveAsync cho Id {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Từ chối đề xuất
    [Authorize(InternalApprovalPermissions.Requests.Reject)]
    public virtual async Task<RequestDto> RejectAsync(Guid id, RejectRequestDto input)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.RejectionReasonRequired);
            }

            var request = await _requestRepository.GetWithDetailsAsync(id);
            if (request == null)
            {
                throw new EntityNotFoundException(typeof(Request), id);
            }

            await _requestManager.RejectAsync(request, CurrentUser.GetId(), input.Reason);
            await _requestRepository.UpdateAsync(request, autoSave: true);

            return await GetAsync(request.Id);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong RejectAsync cho Id {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Thêm bình luận vào đề xuất
    public virtual async Task<RequestCommentDto> AddCommentAsync(Guid id, CreateRequestCommentDto input)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input.Content))
            {
                throw new BusinessException("InternalApproval:00000");
            }

            var request = await _requestRepository.GetWithDetailsAsync(id);
            if (request == null)
            {
                throw new EntityNotFoundException(typeof(Request), id);
            }

            var comment = request.AddComment(GuidGenerator.Create(), input.Content);
            await _requestRepository.UpdateAsync(request, autoSave: true);

            var commentDto = MapWithExceptionHandling<RequestComment, RequestCommentDto>(comment);
            commentDto.CreatorName = CurrentUser.UserName;

            return commentDto;
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong AddCommentAsync cho Id {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    // Kiểm tra hợp lệ số tiền theo loại đề xuất
    private static void ValidateAmount(RequestType type, decimal? amount)
    {
        if (type == RequestType.Equipment || type == RequestType.AdvanceMoney)
        {
            if (!amount.HasValue || amount.Value <= 0)
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.AmountRequiredForType);
            }
        }
    }
}
