using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace MyCompany.InternalApproval.Requests;

// Bộ lắng nghe sự kiện thay đổi trạng thái đề xuất để đẩy vào hàng đợi thông báo
public class RequestStatusChangedEventHandler : ILocalEventHandler<RequestStatusChangedEto>, ITransientDependency
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly ILogger<RequestStatusChangedEventHandler> _logger;

    public RequestStatusChangedEventHandler(
        IBackgroundJobManager backgroundJobManager,
        ILogger<RequestStatusChangedEventHandler> logger)
    {
        _backgroundJobManager = backgroundJobManager;
        _logger = logger;
    }

    public async Task HandleEventAsync(RequestStatusChangedEto eventData)
    {
        try
        {
            if (eventData.NewStatus == RequestStatus.Submitted ||
                eventData.NewStatus == RequestStatus.Approved ||
                eventData.NewStatus == RequestStatus.Rejected)
            {
                await _backgroundJobManager.EnqueueAsync(new SendApprovalNotificationArgs
                {
                    RequestId = eventData.RequestId,
                    Title = eventData.Title,
                    Status = eventData.NewStatus,
                    CreatorId = eventData.CreatorId,
                    ApproverId = eventData.ApproverId,
                    ApprovalNote = eventData.ApprovalNote,
                    RejectionReason = eventData.RejectionReason
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong RequestStatusChangedEventHandler cho RequestId {RequestId}: {Message}", eventData.RequestId, ex.Message);
            throw;
        }
    }
}
