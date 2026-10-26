import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { PermissionService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { EmployeeService } from '../../../proxy/employees/employee.service';
import {
  EmployeeProfileDto,
  EmployeeYearlyStatsDto,
  LeaveTimelineItemDto,
  UpdateEmployeeJoinDateDto,
} from '../../../proxy/employees/models';
import { RequestDto, RequestStatus, RequestType } from '../../../proxy/requests';
import {
  REQUEST_STATUS_LIST,
  getRequestStatusMeta,
  RequestStatusMeta,
} from '../../constants/request-status.constant';
import {
  REQUEST_TYPE_LIST,
  getRequestTypeMeta,
  RequestTypeMeta,
} from '../../constants/request-type.constant';
import { CurrentUserContextService, ExcelExportService } from '../../services';
import { extractErrorMessage, getSingleInitial, getTenureMilestone } from '../../utils';

// Ngăn kéo hiển thị hồ sơ nhân viên, lịch sử nghỉ phép, đề xuất và thống kê theo năm
@Component({
  selector: 'app-employee-detail-drawer',
  templateUrl: './employee-detail-drawer.component.html',
  styleUrls: ['./employee-detail-drawer.component.scss'],
})
export class EmployeeDetailDrawerComponent implements OnInit {
  @Output() joinDateUpdated = new EventEmitter<EmployeeProfileDto>();

  isVisible = false;
  userId: string | null = null;
  loadingProfile = false;
  profile: EmployeeProfileDto | null = null;

  // Điều hướng tab trong ngăn kéo
  selectedTabIndex = 0;

  // Quyền và trạng thái cập nhật ngày vào làm
  canEditJoinDate = false;
  isEditingJoinDate = false;
  editJoinDate: Date | null = null;
  isSavingJoinDate = false;

  // Tab 2: Lịch sử nghỉ phép (Leave Timeline)
  loadingTimeline = false;
  leaveTimeline: LeaveTimelineItemDto[] = [];
  leaveStats = {
    totalApprovedDays: 0,
    totalLeaves: 0,
    approvedCount: 0,
    pendingCount: 0,
    rejectedCount: 0,
  };

  // Tab 3: Lịch sử đề xuất (Request History)
  loadingRequests = false;
  requests: RequestDto[] = [];
  totalRequests = 0;
  requestPageIndex = 1;
  requestPageSize = 5;
  selectedStatus: RequestStatus | null = null;
  selectedType: RequestType | null = null;

  // Tab 4: Thống kê theo năm (Yearly Stats)
  loadingStats = false;
  selectedYear: number = new Date().getFullYear();
  availableYears: number[] = [2026, 2025, 2024, 2023];
  yearlyStats: EmployeeYearlyStatsDto | null = null;

  // Danh mục dùng chung cho template
  RequestType = RequestType;
  RequestStatus = RequestStatus;
  statusList: RequestStatusMeta[] = REQUEST_STATUS_LIST;
  typeList: RequestTypeMeta[] = REQUEST_TYPE_LIST;

  // Trạng thái xuất báo cáo Excel cá nhân
  isExportingExcel = false;

  constructor(
    private employeeService: EmployeeService,
    private userContext: CurrentUserContextService,
    private permissionService: PermissionService,
    private message: NzMessageService,
    private excelExportService: ExcelExportService,
  ) {}

  ngOnInit(): void {
    this.checkEditPermission();
  }

  private checkEditPermission(): void {
    const isAdmin = this.userContext.isAdmin();
    const hasEditPermission = this.permissionService.getGrantedPolicy('Employees.Edit');
    this.canEditJoinDate = isAdmin || hasEditPermission;
  }

  open(userId: string, tabIndex: number = 0): void {
    if (!userId) return;
    this.userId = userId;
    this.selectedTabIndex = tabIndex;
    this.isVisible = true;
    this.isEditingJoinDate = false;
    this.requestPageIndex = 1;
    this.selectedStatus = null;
    this.selectedType = null;
    this.selectedYear = new Date().getFullYear();

    this.loadProfile();
    this.loadLeaveTimeline();
    this.loadRequests();
    this.loadYearlyStats();
  }

  close(): void {
    this.isVisible = false;
    this.isEditingJoinDate = false;
  }

  loadProfile(): void {
    if (!this.userId) return;
    this.loadingProfile = true;
    this.employeeService.get(this.userId).subscribe({
      next: res => {
        this.profile = res;
        this.loadingProfile = false;
      },
      error: err => {
        this.loadingProfile = false;
        this.message.error(extractErrorMessage(err, 'Không thể tải thông tin hồ sơ nhân viên!'));
      },
    });
  }

  startEditJoinDate(): void {
    if (!this.profile) return;
    this.isEditingJoinDate = true;
    const dateStr = this.profile.rawJoinDate || this.profile.joinDate;
    this.editJoinDate = dateStr ? new Date(dateStr) : new Date();
  }

  cancelEditJoinDate(): void {
    this.isEditingJoinDate = false;
    this.editJoinDate = null;
  }

  saveJoinDate(): void {
    if (!this.userId || !this.editJoinDate) return;
    this.isSavingJoinDate = true;
    const input: UpdateEmployeeJoinDateDto = {
      joinDate: this.editJoinDate.toISOString(),
    };

    this.employeeService.updateJoinDate(this.userId, input).subscribe({
      next: updated => {
        this.isSavingJoinDate = false;
        this.isEditingJoinDate = false;
        this.profile = updated;
        this.joinDateUpdated.emit(updated);
        this.message.success('Đã cập nhật ngày vào làm việc thành công!');
      },
      error: err => {
        this.isSavingJoinDate = false;
        this.message.error(extractErrorMessage(err, 'Không thể cập nhật ngày vào làm việc!'));
      },
    });
  }

  loadLeaveTimeline(): void {
    if (!this.userId) return;
    this.loadingTimeline = true;
    this.employeeService.getLeaveTimeline(this.userId, this.selectedYear).subscribe({
      next: res => {
        this.leaveTimeline = res || [];
        this.calculateLeaveStats();
        this.loadingTimeline = false;
      },
      error: () => {
        this.loadingTimeline = false;
      },
    });
  }

  private calculateLeaveStats(): void {
    const list = this.leaveTimeline;
    let approvedDays = 0;
    let approved = 0;
    let pending = 0;
    let rejected = 0;

    list.forEach(item => {
      if (item.status === RequestStatus.Approved) {
        approvedDays += item.days;
        approved++;
      } else if (item.status === RequestStatus.Submitted) {
        pending++;
      } else if (item.status === RequestStatus.Rejected) {
        rejected++;
      }
    });

    this.leaveStats = {
      totalApprovedDays: Math.round(approvedDays * 10) / 10,
      totalLeaves: list.length,
      approvedCount: approved,
      pendingCount: pending,
      rejectedCount: rejected,
    };
  }

  loadRequests(): void {
    if (!this.userId) return;
    this.loadingRequests = true;
    this.employeeService
      .getRequests(this.userId, {
        status: this.selectedStatus !== null ? this.selectedStatus : undefined,
        type: this.selectedType !== null ? this.selectedType : undefined,
        skipCount: (this.requestPageIndex - 1) * this.requestPageSize,
        maxResultCount: this.requestPageSize,
      })
      .subscribe({
        next: res => {
          this.requests = res.items || [];
          this.totalRequests = res.totalCount || 0;
          this.loadingRequests = false;
        },
        error: () => {
          this.loadingRequests = false;
        },
      });
  }

  onRequestFilterChange(): void {
    this.requestPageIndex = 1;
    this.loadRequests();
  }

  onRequestPageChange(page: number): void {
    this.requestPageIndex = page;
    this.loadRequests();
  }

  loadYearlyStats(): void {
    if (!this.userId) return;
    this.loadingStats = true;
    this.employeeService.getYearlyStats(this.userId, this.selectedYear).subscribe({
      next: res => {
        this.yearlyStats = res;
        this.loadingStats = false;
      },
      error: () => {
        this.loadingStats = false;
      },
    });
  }

  onYearChange(year: number): void {
    this.selectedYear = year;
    this.loadYearlyStats();
    this.loadLeaveTimeline();
  }

  // Các hàm hỗ trợ hiển thị
  getTypeTag(type: RequestType): RequestTypeMeta {
    return getRequestTypeMeta(type);
  }

  getStatusTag(status: RequestStatus): RequestStatusMeta {
    return getRequestStatusMeta(status);
  }

  trackRequestById(_: number, item: RequestDto): string {
    return item.id ?? '';
  }

  trackTimelineById(_: number, item: LeaveTimelineItemDto): string {
    return item.requestId;
  }

  getAvatarInitial(): string {
    return getSingleInitial(this.profile?.fullName);
  }

  getTenureProgressPercentage(): number {
    const months = this.profile?.tenureMonths || 0;
    // Giới hạn tối đa ở mốc 36 tháng (tương ứng 100%)
    return Math.min(100, Math.round((months / 36) * 100));
  }

  getTenureMilestoneLabel(): { label: string; color: string } {
    return getTenureMilestone(this.profile?.tenureMonths || 0);
  }

  // Tải về file Excel thống kê chi tiết số lượt nghỉ phép và lịch sử đề xuất của nhân sự
  exportExcel(): void {
    if (!this.userId || !this.profile) {
      this.message.warning('Không có thông tin nhân sự để xuất Excel!');
      return;
    }

    this.isExportingExcel = true;
    this.employeeService
      .getRequests(this.userId, { skipCount: 0, maxResultCount: 1000 })
      .subscribe({
        next: res => {
          const allRequests = res.items || [];
          const currentUser = this.userContext.currentUser;
          const exporterName = currentUser?.name || currentUser?.userName || 'Quản trị viên';

          this.excelExportService.exportEmployeeDetailReport(
            this.profile!,
            this.yearlyStats,
            allRequests,
            this.leaveTimeline,
            exporterName,
          );

          this.message.success(
            `Đã xuất file Excel báo cáo cho nhân sự "${this.profile?.fullName}" thành công!`,
          );
          this.isExportingExcel = false;
        },
        error: err => {
          this.isExportingExcel = false;
          this.message.error(extractErrorMessage(err, 'Không thể xuất file Excel thống kê!'));
        },
      });
  }
}
