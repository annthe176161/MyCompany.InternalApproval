using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyCompany.InternalApproval.Notifications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace MyCompany.InternalApproval.Requests;

// Tiến trình ngầm gửi thông báo phê duyệt đề xuất
[UnitOfWork]
public class SendApprovalNotificationJob : AsyncBackgroundJob<SendApprovalNotificationArgs>, ITransientDependency
{
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IApprovalNotificationRepository _notificationRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IdentityUserManager _identityUserManager;

    public SendApprovalNotificationJob(
        IRepository<IdentityUser, Guid> userRepository,
        IApprovalNotificationRepository notificationRepository,
        IGuidGenerator guidGenerator,
        IdentityUserManager identityUserManager)
    {
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _guidGenerator = guidGenerator;
        _identityUserManager = identityUserManager;
    }

    public override async Task ExecuteAsync(SendApprovalNotificationArgs args)
    {
        try
        {
            string creatorName = "Người dùng";
            string creatorEmail = "Chưa có email";
            if (args.CreatorId.HasValue)
            {
                var creator = await _userRepository.FindAsync(args.CreatorId.Value);
                if (creator != null)
                {
                    creatorName = !string.IsNullOrWhiteSpace(creator.Name) ? creator.Name : creator.UserName;
                    creatorEmail = !string.IsNullOrWhiteSpace(creator.Email) ? creator.Email : "Chưa có email";
                }
            }

            string approverName = "Quản lý / Người phê duyệt";
            if (args.ApproverId.HasValue)
            {
                var approver = await _userRepository.FindAsync(args.ApproverId.Value);
                if (approver != null)
                {
                    approverName = !string.IsNullOrWhiteSpace(approver.Name) ? approver.Name : approver.UserName;
                }
            }

            // TRƯỜNG HỢP 1: Đề xuất mới được gửi duyệt -> Gửi thông báo đến Quản trị viên (Admin / Manager)
            if (args.Status == RequestStatus.Submitted)
            {
                var adminUsers = new List<IdentityUser>();
                try
                {
                    var roleUsers = await _identityUserManager.GetUsersInRoleAsync("admin");
                    if (roleUsers != null && roleUsers.Any())
                    {
                        adminUsers.AddRange(roleUsers);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Không thể lấy danh sách user từ role 'admin'.");
                }

                try
                {
                    var managerUsers = await _identityUserManager.GetUsersInRoleAsync("manager");
                    if (managerUsers != null && managerUsers.Any())
                    {
                        adminUsers.AddRange(managerUsers);
                    }
                }
                catch
                {
                    // Bỏ qua nếu role manager không tồn tại
                }

                if (!adminUsers.Any())
                {
                    var defaultAdmin = await _userRepository.FirstOrDefaultAsync(u =>
                        u.UserName == "admin" || u.NormalizedUserName == "ADMIN" ||
                        u.UserName == "manager" || u.NormalizedUserName == "MANAGER");
                    if (defaultAdmin != null)
                    {
                        adminUsers.Add(defaultAdmin);
                    }
                }

                var notificationsToInsert = adminUsers
                    .DistinctBy(u => u.Id)
                    .Where(admin => !args.CreatorId.HasValue || admin.Id != args.CreatorId.Value)
                    .Select(admin => new ApprovalNotification(
                        _guidGenerator.Create(),
                        args.RequestId,
                        args.Title,
                        args.Status,
                        admin.Id,
                        null,
                        $"Đề xuất '{args.Title}' vừa được gửi bởi {creatorName}. Vui lòng kiểm tra và phê duyệt."
                    ))
                    .ToList();

                if (notificationsToInsert.Any())
                {
                    await _notificationRepository.InsertManyAsync(notificationsToInsert, autoSave: true);
                }

                Logger.LogInformation(
                    "\n================================================================================\n" +
                    "[BACKGROUND JOB - THÔNG BÁO ĐỀ XUẤT MỚI GỬI ĐẾN ADMIN ĐÃ LƯU DATABASE]\n" +
                    "Người gửi: {CreatorName} ({CreatorEmail})\n" +
                    "Đề xuất: #{RequestId} - '{Title}'\n" +
                    "Trạng thái: CHỜ PHÊ DUYỆT\n" +
                    "Số lượng Admin nhận thông báo: {AdminCount}\n" +
                    "Thời gian: {Time}\n" +
                    "================================================================================",
                    creatorName,
                    creatorEmail,
                    args.RequestId.ToString().Substring(0, 8),
                    args.Title,
                    notificationsToInsert.Count,
                    DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                );
            }
            // TRƯỜNG HỢP 2: Đề xuất đã được duyệt hoặc từ chối -> Gửi thông báo kết quả về cho Người tạo (Creator)
            else if (args.Status == RequestStatus.Approved || args.Status == RequestStatus.Rejected)
            {
                if (args.CreatorId.HasValue)
                {
                    string messageContent = args.Status == RequestStatus.Approved
                        ? $"Đề xuất '{args.Title}' của bạn đã được phê duyệt bởi {approverName}. Ghi chú: {(string.IsNullOrWhiteSpace(args.ApprovalNote) ? "Không có ghi chú" : args.ApprovalNote)}"
                        : $"Đề xuất '{args.Title}' của bạn đã bị từ chối bởi {approverName}. Lý do: {args.RejectionReason}";

                    var notification = new ApprovalNotification(
                        _guidGenerator.Create(),
                        args.RequestId,
                        args.Title,
                        args.Status,
                        args.CreatorId.Value,
                        args.ApproverId,
                        messageContent
                    );

                    await _notificationRepository.InsertAsync(notification, autoSave: true);
                }

                if (args.Status == RequestStatus.Approved)
                {
                    Logger.LogInformation(
                        "\n================================================================================\n" +
                        "[BACKGROUND JOB - THÔNG BÁO PHÊ DUYỆT ĐỀ XUẤT ĐÃ LƯU DATABASE]\n" +
                        "Gửi tới: {CreatorName} ({CreatorEmail})\n" +
                        "Đề xuất: #{RequestId} - '{Title}'\n" +
                        "Trạng thái: ĐÃ ĐƯỢC PHÊ DUYỆT bởi {ApproverName}\n" +
                        "Ghi chú phê duyệt: {ApprovalNote}\n" +
                        "Thời gian: {Time}\n" +
                        "================================================================================",
                        creatorName,
                        creatorEmail,
                        args.RequestId.ToString().Substring(0, 8),
                        args.Title,
                        approverName,
                        string.IsNullOrWhiteSpace(args.ApprovalNote) ? "(Không có ghi chú)" : args.ApprovalNote,
                        DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                    );
                }
                else if (args.Status == RequestStatus.Rejected)
                {
                    Logger.LogInformation(
                        "\n================================================================================\n" +
                        "[BACKGROUND JOB - THÔNG BÁO TỪ CHỐI ĐỀ XUẤT ĐÃ LƯU DATABASE]\n" +
                        "Gửi tới: {CreatorName} ({CreatorEmail})\n" +
                        "Đề xuất: #{RequestId} - '{Title}'\n" +
                        "Trạng thái: ĐÃ BỊ TỪ CHỐI bởi {ApproverName}\n" +
                        "Lý do từ chối: {RejectionReason}\n" +
                        "Thời gian: {Time}\n" +
                        "================================================================================",
                        creatorName,
                        creatorEmail,
                        args.RequestId.ToString().Substring(0, 8),
                        args.Title,
                        approverName,
                        args.RejectionReason,
                        DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Lỗi xảy ra trong SendApprovalNotificationJob cho RequestId {RequestId}: {Message}", args.RequestId, ex.Message);
            throw;
        }
    }
}
