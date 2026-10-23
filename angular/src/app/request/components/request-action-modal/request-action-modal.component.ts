import { Component, EventEmitter, Output } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { RequestService, RequestDto } from '../../../proxy/requests';

@Component({
  selector: 'app-request-action-modal',
  templateUrl: './request-action-modal.component.html',
})
export class RequestActionModalComponent {
  @Output() actionSuccess = new EventEmitter<void>();

  // Approve State
  isApproveModalVisible = false;
  isApproving = false;
  selectedRequestForApprove: RequestDto | null = null;
  approvalNote = '';

  // Reject State
  isRejectModalVisible = false;
  isRejecting = false;
  selectedRequestForReject: RequestDto | null = null;
  rejectionReason = '';

  constructor(
    private requestService: RequestService,
    private message: NzMessageService,
  ) {}

  openApprove(req: RequestDto): void {
    this.selectedRequestForApprove = req;
    this.approvalNote = '';
    this.isApproveModalVisible = true;
  }

  confirmApprove(): void {
    if (!this.selectedRequestForApprove) return;

    this.isApproving = true;
    this.requestService
      .approve(this.selectedRequestForApprove.id, {
        note: this.approvalNote ? this.approvalNote.trim() : undefined,
      })
      .subscribe({
        next: () => {
          this.message.success('Phê duyệt đề xuất thành công!');
          this.isApproving = false;
          this.isApproveModalVisible = false;
          this.actionSuccess.emit();
        },
        error: err => {
          const msg = err.error?.error?.message || 'Không thể phê duyệt đề xuất!';
          this.message.error(msg);
          this.isApproving = false;
        },
      });
  }

  openReject(req: RequestDto): void {
    this.selectedRequestForReject = req;
    this.rejectionReason = '';
    this.isRejectModalVisible = true;
  }

  confirmReject(): void {
    if (!this.selectedRequestForReject || !this.rejectionReason.trim()) return;

    this.isRejecting = true;
    this.requestService
      .reject(this.selectedRequestForReject.id, {
        reason: this.rejectionReason.trim(),
      })
      .subscribe({
        next: () => {
          this.message.success('Đã từ chối đề xuất!');
          this.isRejecting = false;
          this.isRejectModalVisible = false;
          this.actionSuccess.emit();
        },
        error: err => {
          const msg = err.error?.error?.message || 'Không thể từ chối đề xuất!';
          this.message.error(msg);
          this.isRejecting = false;
        },
      });
  }
}
