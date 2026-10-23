using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Local;

namespace MyCompany.InternalApproval.Requests;

public class RequestManager : DomainService
{
    private readonly ILocalEventBus _localEventBus;

    public RequestManager(ILocalEventBus localEventBus)
    {
        _localEventBus = localEventBus;
    }

    public virtual async Task SubmitAsync(Request request, Guid currentUserId)
    {
        Check.NotNull(request, nameof(request));

        if (request.CreatorId.HasValue && request.CreatorId.Value != currentUserId)
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.OnlyCreatorCanSubmit);
        }

        if (request.Status != RequestStatus.Draft)
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.OnlyDraftCanBeSubmitted);
        }

        var oldStatus = request.Status;
        request.Submit();

        await _localEventBus.PublishAsync(new RequestStatusChangedEto(
            request.Id,
            request.Title,
            oldStatus,
            request.Status,
            request.CreatorId
        ));
    }

    public virtual async Task ApproveAsync(Request request, Guid approverId, string? approvalNote = null)
    {
        Check.NotNull(request, nameof(request));

        if (request.CreatorId.HasValue && request.CreatorId.Value == approverId)
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.CannotApproveOwnRequest);
        }

        if (request.Status != RequestStatus.Submitted)
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.OnlySubmittedCanBeApproved);
        }

        var oldStatus = request.Status;
        request.Approve(approverId);

        await _localEventBus.PublishAsync(new RequestStatusChangedEto(
            request.Id,
            request.Title,
            oldStatus,
            request.Status,
            request.CreatorId,
            approverId,
            approvalNote: approvalNote
        ));
    }

    public virtual async Task RejectAsync(Request request, Guid approverId, string reason)
    {
        Check.NotNull(request, nameof(request));

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.RejectionReasonRequired);
        }

        if (request.Status != RequestStatus.Submitted)
        {
            throw new BusinessException(InternalApprovalDomainErrorCodes.OnlySubmittedCanBeRejected);
        }

        var oldStatus = request.Status;
        request.Reject(approverId, reason.Trim());

        await _localEventBus.PublishAsync(new RequestStatusChangedEto(
            request.Id,
            request.Title,
            oldStatus,
            request.Status,
            request.CreatorId,
            approverId,
            rejectionReason: reason.Trim()
        ));
    }
}

