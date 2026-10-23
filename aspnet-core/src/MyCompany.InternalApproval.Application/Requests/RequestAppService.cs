using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MyCompany.InternalApproval.Permissions;
using MyCompany.InternalApproval.Departments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Volo.Abp.Validation;

namespace MyCompany.InternalApproval.Requests;

/// <summary>
/// Dịch vụ ứng dụng (Application Service) quản lý các đề xuất nội bộ.
/// Cung cấp các chức năng CRUD, gửi duyệt, phê duyệt, từ chối và thêm bình luận cho đề xuất.
/// </summary>
[RemoteService(IsEnabled = false)]
[Authorize(InternalApprovalPermissions.Requests.Default)]
public class RequestAppService : ApplicationService, IRequestAppService
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

    /// <summary>
    /// Ánh xạ dữ liệu qua AutoMapper.
    /// </summary>
    protected virtual TDestination MapWithExceptionHandling<TSource, TDestination>(TSource source)
    {
        try
        {
            return ObjectMapper.Map<TSource, TDestination>(source);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Lỗi AutoMapper khi chuyển đổi kiểu từ {Source} sang {Destination}: {Message}", typeof(TSource).Name, typeof(TDestination).Name, ex.Message);
            throw new BusinessException(InternalApprovalDomainErrorCodes.MappingError)
                .WithData("Source", typeof(TSource).Name)
                .WithData("Destination", typeof(TDestination).Name);
        }
    }

    /// <summary>
    /// Lấy danh sách đề xuất có phân trang và tìm kiếm.
    /// </summary>
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
            var userDeptDict = memberList.ToDictionary(
                x => x.UserId,
                x => deptDict.TryGetValue(x.DepartmentId, out var d) ? d : default
            );

            var requestDtos = MapWithExceptionHandling<List<Request>, List<RequestDto>>(requests);

            foreach (var dto in requestDtos)
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

                foreach (var commentDto in dto.Comments)
                {
                    if (commentDto.CreatorId.HasValue && userDict.TryGetValue(commentDto.CreatorId.Value, out var commentCreatorName))
                    {
                        commentDto.CreatorName = commentCreatorName;
                    }
                }
            }

            return new PagedResultDto<RequestDto>(totalCount, requestDtos);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetListAsync: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    /// <summary>
    /// Lấy chi tiết đề xuất theo Id.
    /// </summary>
    public virtual async Task<RequestDto> GetAsync(Guid id)
    {
        try
        {
            var request = await _requestRepository.GetWithDetailsAsync(id);
            if (request == null)
            {
                throw new EntityNotFoundException(typeof(Request), id);
            }

            var userIds = new List<Guid>();
            if (request.CreatorId.HasValue) userIds.Add(request.CreatorId.Value);
            if (request.ApproverId.HasValue) userIds.Add(request.ApproverId.Value);
            userIds.AddRange(request.Comments.Where(x => x.CreatorId.HasValue).Select(x => x.CreatorId!.Value));

            var users = userIds.Distinct().Any()
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

            foreach (var commentDto in dto.Comments)
            {
                if (commentDto.CreatorId.HasValue && userDict.TryGetValue(commentDto.CreatorId.Value, out var commentCreatorName))
                {
                    commentDto.CreatorName = commentCreatorName;
                }
            }

            return dto;
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetAsync cho Id {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    /// <summary>
    /// Tạo mới một đề xuất.
    /// </summary>
    [Authorize(InternalApprovalPermissions.Requests.Create)]
    public virtual async Task<RequestDto> CreateAsync(CreateRequestDto input)
    {
        try
        {
            // Kiểm tra hợp lệ số tiền dựa theo loại đề xuất
            ValidateAmount(input.Type, input.Amount);

            // Khởi tạo đối tượng Request (mặc định trạng thái là Nháp)
            var request = new Request(
                GuidGenerator.Create(),
                input.Title,
                input.Type,
                input.Description,
                input.Amount,
                input.StartDate,
                input.EndDate
            );

            // Lưu đề xuất vào cơ sở dữ liệu
            await _requestRepository.InsertAsync(request, autoSave: true);

            return MapWithExceptionHandling<Request, RequestDto>(request);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not EntityNotFoundException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong CreateAsync: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    /// <summary>
    /// Cập nhật thông tin đề xuất.
    /// </summary>
    [Authorize(InternalApprovalPermissions.Requests.Edit)]
    public virtual async Task<RequestDto> UpdateAsync(Guid id, UpdateRequestDto input)
    {
        try
        {
            // 1. Kiểm tra dữ liệu đầu vào ngay lập tức (không cần chờ gọi Database)
            ValidateAmount(input.Type, input.Amount);

            // 2. Lấy dữ liệu và kiểm tra các ràng buộc nghiệp vụ
            var request = await _requestRepository.GetAsync(id);

            if (request.CreatorId.HasValue && request.CreatorId != CurrentUser.GetId())
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.OnlyCreatorCanEdit);
            }

            if (request.Status != RequestStatus.Draft)
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.OnlyDraftCanBeEdited);
            }

            // 3. Xử lý cập nhật
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

    /// <summary>
    /// Xóa đề xuất.
    /// </summary>
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

    /// <summary>
    /// Gửi duyệt đề xuất.
    /// </summary>
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

    /// <summary>
    /// Phê duyệt đề xuất.
    /// </summary>
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

    /// <summary>
    /// Từ chối đề xuất.
    /// </summary>
    [Authorize(InternalApprovalPermissions.Requests.Reject)]
    public virtual async Task<RequestDto> RejectAsync(Guid id, RejectRequestDto input)
    {
        try
        {
            // 1. Kiểm tra lý do từ chối ngay ở đầu (không cần chờ gọi Database)
            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                throw new BusinessException(InternalApprovalDomainErrorCodes.RejectionReasonRequired);
            }

            // 2. Lấy dữ liệu và kiểm tra tồn tại
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

    /// <summary>
    /// Thêm bình luận vào đề xuất.
    /// </summary>
    public virtual async Task<RequestCommentDto> AddCommentAsync(Guid id, CreateRequestCommentDto input)
    {
        try
        {
            // 1. Kiểm tra nội dung bình luận ngay ở đầu
            if (string.IsNullOrWhiteSpace(input.Content))
            {
                throw new BusinessException("InternalApproval:00000");
            }

            // 2. Lấy dữ liệu và kiểm tra tồn tại
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

    /// <summary>
    /// Kiểm tra hợp lệ số tiền theo loại đề xuất.
    /// </summary>
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

