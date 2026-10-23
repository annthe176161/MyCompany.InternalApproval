using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Volo.Abp.Validation;

namespace MyCompany.InternalApproval.Notifications;

/// <summary>
/// Dịch vụ ứng dụng quản lý thông báo của người dùng đăng nhập.
/// </summary>
[Authorize]
[RemoteService(IsEnabled = false)]
public class NotificationAppService : ApplicationService, INotificationAppService
{
    private readonly IRepository<ApprovalNotification, Guid> _notificationRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;

    public NotificationAppService(
        IRepository<ApprovalNotification, Guid> notificationRepository,
        IRepository<IdentityUser, Guid> userRepository)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Hàm trợ giúp ánh xạ AutoMapper bọc bắt ngoại lệ chuẩn hóa.
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
    /// Lấy danh sách thông báo của người dùng hiện tại, sắp xếp theo thời gian mới nhất.
    /// </summary>
    public virtual async Task<PagedResultDto<ApprovalNotificationDto>> GetListAsync(GetNotificationListInput input)
    {
        try
        {
            var currentUserId = CurrentUser.GetId();

            var queryable = await _notificationRepository.GetQueryableAsync();
            var userQuery = queryable.Where(x => x.ReceiverId == currentUserId);

            var totalCount = await AsyncExecuter.CountAsync(userQuery);

            var items = await AsyncExecuter.ToListAsync(
                userQuery
                    .OrderByDescending(x => x.CreationTime)
                    .PageBy(input.SkipCount, input.MaxResultCount)
            );

            // Lấy danh sách Id người duyệt để nạp tên hiển thị
            var approverIds = items
                .Where(x => x.ApproverId.HasValue)
                .Select(x => x.ApproverId!.Value)
                .Distinct()
                .ToList();

            var approvers = approverIds.Any()
                ? await _userRepository.GetListAsync(x => approverIds.Contains(x.Id))
                : new List<IdentityUser>();

            var approverDict = approvers.ToDictionary(
                x => x.Id,
                x => !string.IsNullOrWhiteSpace(x.Name) ? $"{x.Name} ({x.UserName})" : x.UserName
            );

            var dtos = MapWithExceptionHandling<List<ApprovalNotification>, List<ApprovalNotificationDto>>(items);

            foreach (var dto in dtos)
            {
                if (dto.ApproverId.HasValue && approverDict.TryGetValue(dto.ApproverId.Value, out var approverName))
                {
                    dto.ApproverName = approverName;
                }
            }

            return new PagedResultDto<ApprovalNotificationDto>(totalCount, dtos);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetListAsync thông báo: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    /// <summary>
    /// Lấy số lượng thông báo chưa đọc của người dùng hiện tại.
    /// </summary>
    public virtual async Task<int> GetUnreadCountAsync()
    {
        try
        {
            var currentUserId = CurrentUser.GetId();
            var queryable = await _notificationRepository.GetQueryableAsync();
            return await AsyncExecuter.CountAsync(queryable.Where(x => x.ReceiverId == currentUserId && !x.IsRead));
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong GetUnreadCountAsync: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    /// <summary>
    /// Đánh dấu một thông báo là đã đọc.
    /// </summary>
    public virtual async Task MarkAsReadAsync(Guid id)
    {
        try
        {
            var currentUserId = CurrentUser.GetId();
            var notification = await _notificationRepository.FindAsync(id);

            // Thoát sớm nếu không tìm thấy, không phải người nhận hoặc đã đọc rồi
            if (notification == null || notification.ReceiverId != currentUserId || notification.IsRead)
            {
                return;
            }

            notification.MarkAsRead();
            await _notificationRepository.UpdateAsync(notification, autoSave: true);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong MarkAsReadAsync cho Id {Id}: {Message}", id, ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo của người dùng hiện tại là đã đọc.
    /// </summary>
    public virtual async Task MarkAllAsReadAsync()
    {
        try
        {
            var currentUserId = CurrentUser.GetId();
            var unreadNotifications = await _notificationRepository.GetListAsync(x => x.ReceiverId == currentUserId && !x.IsRead);

            // Thoát sớm nếu không có thông báo nào chưa đọc
            if (!unreadNotifications.Any())
            {
                return;
            }

            foreach (var notif in unreadNotifications)
            {
                notif.MarkAsRead();
            }
            await _notificationRepository.UpdateManyAsync(unreadNotifications, autoSave: true);
        }
        catch (Exception ex) when (ex is not IBusinessException && ex is not AbpAuthorizationException && ex is not AbpValidationException)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong MarkAllAsReadAsync: {Message}", ex.Message);
            throw new BusinessException("InternalApproval:00000");
        }
    }
}

