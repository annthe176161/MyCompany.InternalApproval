import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '@abp/ng.core';
import { Subscription, interval } from 'rxjs';
import { NzBadgeModule } from 'ng-zorro-antd/badge';
import { NzPopoverModule } from 'ng-zorro-antd/popover';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzEmptyModule } from 'ng-zorro-antd/empty';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NotificationService, ApprovalNotificationDto } from '../../../proxy/notifications';
import { RequestStatus } from '../../../proxy/requests';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [
    CommonModule,
    NzBadgeModule,
    NzPopoverModule,
    NzButtonModule,
    NzIconModule,
    NzTagModule,
    NzEmptyModule,
    NzSpinModule,
    NzToolTipModule,
  ],
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.scss'],
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  RequestStatus = RequestStatus;

  unreadCount = 0;
  notifications: ApprovalNotificationDto[] = [];
  loading = false;
  isPopoverVisible = false;

  private pollSubscription?: Subscription;

  constructor(
    private notificationService: NotificationService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (!this.authService.isAuthenticated) {
      return;
    }

    this.fetchUnreadCount();

    // Tự động kiểm tra số lượng thông báo mới mỗi 10 giây
    this.pollSubscription = interval(10000).subscribe(() => {
      if (this.authService.isAuthenticated) {
        this.fetchUnreadCount();
        if (this.isPopoverVisible) {
          this.fetchNotifications(false);
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.pollSubscription?.unsubscribe();
  }

  fetchUnreadCount(): void {
    this.notificationService.getUnreadCount().subscribe({
      next: count => {
        this.unreadCount = count || 0;
        this.cdr.markForCheck();
      },
      error: () => {},
    });
  }

  fetchNotifications(showLoading = true): void {
    if (showLoading && this.notifications.length === 0) {
      this.loading = true;
      this.cdr.markForCheck();
    }
    this.notificationService
      .getList({
        maxResultCount: 20,
        skipCount: 0,
        sorting: 'CreationTime desc',
      })
      .subscribe({
        next: res => {
          this.notifications = res.items || [];
          this.loading = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.loading = false;
          this.cdr.markForCheck();
        },
      });
  }

  onVisibleChange(visible: boolean): void {
    this.isPopoverVisible = visible;
    if (visible) {
      this.fetchNotifications(this.notifications.length === 0);
      this.fetchUnreadCount();
    }
    this.cdr.markForCheck();
  }

  markAllAsRead(): void {
    if (this.unreadCount === 0) return;

    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.unreadCount = 0;
        this.notifications.forEach(n => (n.isRead = true));
        this.cdr.markForCheck();
      },
    });
  }

  onItemClick(item: ApprovalNotificationDto): void {
    // Đánh dấu đã đọc nếu chưa đọc
    if (!item.isRead) {
      item.isRead = true;
      if (this.unreadCount > 0) {
        this.unreadCount--;
      }
      this.notificationService.markAsRead(item.id).subscribe();
    }

    // Đóng dropdown popover
    this.isPopoverVisible = false;
    this.cdr.markForCheck();

    // Điều hướng tới trang đề xuất kèm query param requestId để tự động mở chi tiết
    this.router.navigate(['/requests'], {
      queryParams: { requestId: item.requestId, t: Date.now() },
    });
  }
}
