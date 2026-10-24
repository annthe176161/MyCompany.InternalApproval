import { Component, EventEmitter, Output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import {
  RequestService,
  RequestDto,
  RequestCommentDto,
  RequestType,
  RequestStatus,
} from '../../../proxy/requests';

@Component({
  selector: 'app-request-detail-drawer',
  templateUrl: './request-detail-drawer.component.html',
  styleUrls: ['./request-detail-drawer.component.scss'],
})
export class RequestDetailDrawerComponent {
  @Output() openEmployeeProfile = new EventEmitter<string>();

  RequestType = RequestType;
  RequestStatus = RequestStatus;

  isVisible = false;
  detailRequest: RequestDto | null = null;
  newCommentText = '';
  isSendingComment = false;

  onCreatorClick(creatorId?: string): void {
    if (creatorId) {
      this.openEmployeeProfile.emit(creatorId);
    }
  }

  constructor(
    private requestService: RequestService,
    private message: NzMessageService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  open(req: RequestDto): void {
    this.refreshDetail(req.id);
    this.isVisible = true;
  }

  close(): void {
    this.isVisible = false;
    this.detailRequest = null;
    this.newCommentText = '';
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { requestId: null },
      queryParamsHandling: 'merge',
    });
  }

  refreshDetail(id: string): void {
    this.requestService.get(id).subscribe({
      next: res => {
        this.detailRequest = res;
      },
      error: err => {
        const msg = err.error?.error?.message || 'Không thể tải chi tiết đề xuất!';
        this.message.error(msg);
      },
    });
  }

  sendComment(): void {
    if (!this.detailRequest || !this.newCommentText.trim()) return;

    this.isSendingComment = true;
    this.requestService
      .addComment(this.detailRequest.id, {
        content: this.newCommentText.trim(),
      })
      .subscribe({
        next: () => {
          this.message.success('Đã thêm bình luận mới vào đề xuất!');
          this.newCommentText = '';
          this.isSendingComment = false;
          this.refreshDetail(this.detailRequest!.id);
        },
        error: err => {
          const msg = err.error?.error?.message || 'Không thể gửi bình luận!';
          this.message.error(msg);
          this.isSendingComment = false;
        },
      });
  }

  trackCommentById(_: number, item: RequestCommentDto): string {
    return item.id;
  }

  getTypeTag(type: RequestType): { label: string; color: string } {
    switch (type) {
      case RequestType.Leave:
        return { label: 'Nghỉ phép', color: 'purple' };
      case RequestType.Equipment:
        return { label: 'Mua thiết bị', color: 'orange' };
      case RequestType.AdvanceMoney:
        return { label: 'Tạm ứng tiền', color: 'gold' };
      default:
        return { label: 'Khác', color: 'default' };
    }
  }

  getStatusTag(status: RequestStatus): { label: string; badgeStatus: any } {
    switch (status) {
      case RequestStatus.Draft:
        return { label: 'Nháp', badgeStatus: 'default' };
      case RequestStatus.Submitted:
        return { label: 'Đã gửi duyệt', badgeStatus: 'processing' };
      case RequestStatus.Approved:
        return { label: 'Đã duyệt', badgeStatus: 'success' };
      case RequestStatus.Rejected:
        return { label: 'Bị từ chối', badgeStatus: 'error' };
      default:
        return { label: 'Không xác định', badgeStatus: 'default' };
    }
  }

  getLeaveDays(req: RequestDto | null): number | null {
    if (!req || !req.startDate) return null;
    const start = new Date(req.startDate);
    const end = req.endDate ? new Date(req.endDate) : start;
    start.setHours(0, 0, 0, 0);
    end.setHours(0, 0, 0, 0);
    const diffTime = end.getTime() - start.getTime();
    const days = Math.round(diffTime / (1000 * 3600 * 24)) + 1;
    return days > 0 ? days : 1;
  }
}
