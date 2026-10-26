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
import {
  getRequestStatusMeta,
  getRequestTypeMeta,
  RequestStatusMeta,
  RequestTypeMeta,
} from '../../../shared/constants';
import { calculateLeaveDays, extractErrorMessage } from '../../../shared/utils';

// Ngăn kéo hiển thị thông tin chi tiết và lịch sử trao đổi của một đề xuất
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

  constructor(
    private requestService: RequestService,
    private message: NzMessageService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  onCreatorClick(creatorId?: string): void {
    if (creatorId) {
      this.openEmployeeProfile.emit(creatorId);
    }
  }

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
        this.message.error(extractErrorMessage(err, 'Không thể tải chi tiết đề xuất!'));
      },
    });
  }

  // Cập nhật nội dung bình luận ngay lập tức khi gõ phím
  onCommentInput(event: Event): void {
    const target = event.target as HTMLTextAreaElement;
    this.newCommentText = target ? target.value : '';
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
          this.message.error(extractErrorMessage(err, 'Không thể gửi bình luận!'));
          this.isSendingComment = false;
        },
      });
  }

  trackCommentById(_: number, item: RequestCommentDto): string {
    return item.id ?? '';
  }

  getTypeTag(type: RequestType): RequestTypeMeta {
    return getRequestTypeMeta(type);
  }

  getStatusTag(status: RequestStatus): RequestStatusMeta {
    return getRequestStatusMeta(status);
  }

  getLeaveDays(req: RequestDto | null): number | null {
    return calculateLeaveDays(req?.startDate, req?.endDate);
  }
}
