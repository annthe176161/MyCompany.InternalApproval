using System.Threading.Tasks;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace MyCompany.InternalApproval.Requests;

public class RequestStatusChangedEventHandler : ILocalEventHandler<RequestStatusChangedEto>, ITransientDependency
{
    private readonly IBackgroundJobManager _backgroundJobManager;

    public RequestStatusChangedEventHandler(IBackgroundJobManager backgroundJobManager)
    {
        _backgroundJobManager = backgroundJobManager;
    }

    public async Task HandleEventAsync(RequestStatusChangedEto eventData)
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
}

