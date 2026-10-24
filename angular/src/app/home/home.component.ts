import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import {
  RequestService,
  RequestDto,
  RequestType,
  RequestStatus,
  GetRequestListInput,
} from '../proxy/requests';
import {
  REQUEST_STATUS_LIST,
  REQUEST_STATUS_CONFIG,
  getRequestStatusMeta,
  RequestStatusMeta,
} from '../shared/constants/request-status.constant';
import {
  REQUEST_TYPE_LIST,
  REQUEST_TYPE_CONFIG,
  getRequestTypeMeta,
  RequestTypeMeta,
} from '../shared/constants/request-type.constant';
import { CurrentUserContextService, ExcelExportService } from '../shared/services';
import { formatMoneyVnd } from '../shared/utils';
import { DepartmentStatItem, StatusStatItem, TypeStatItem } from './models';

// Màn hình bảng điều khiển trung tâm (Dashboard) theo vai trò người dùng
@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnInit {
  RequestType = RequestType;
  RequestStatus = RequestStatus;

  // Danh mục trạng thái và loại đề xuất dùng chung toàn hệ thống
  readonly statusList: RequestStatusMeta[] = REQUEST_STATUS_LIST;
  readonly typeList: RequestTypeMeta[] = REQUEST_TYPE_LIST;
  readonly statusConfig = REQUEST_STATUS_CONFIG;
  readonly typeConfig = REQUEST_TYPE_CONFIG;

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  loading = false;
  currentUserId: string | null = null;
  currentUserFullName = 'Người dùng';
  currentUserEmail = '';
  userRoles: string[] = [];
  isAdmin = false;
  isApprover = false;
  isExporting = false;

  // Góc nhìn hiển thị theo vai trò hiện tại
  activeRoleView: 'admin' | 'approver' | 'employee' = 'employee';

  allRequests: RequestDto[] = [];

  // Chỉ số thống kê dành cho Quản trị viên (Admin)
  adminTotalCount = 0;
  adminTotalAmount = 0;
  adminApprovedAmount = 0;
  adminApprovalRate = 0;
  adminStatusCards: StatusStatItem[] = [];
  adminTypeStats: TypeStatItem[] = [];
  adminDepartmentStats: DepartmentStatItem[] = [];
  selectedDepartmentFilter = 'all';
  adminRecentRequests: RequestDto[] = [];

  // Chỉ số thống kê dành cho Người phê duyệt (Approver / Manager)
  approverPendingCount = 0;
  approverApprovedCount = 0;
  approverRejectedCount = 0;
  approverTotalApprovedAmount = 0;
  approverActionableRequests: RequestDto[] = [];

  // Chỉ số thống kê dành cho Nhân viên (Employee / Requester)
  employeeTotalCount = 0;
  employeeTotalAmount = 0;
  employeeDraftCount = 0;
  employeeStatusCards: StatusStatItem[] = [];
  employeeRecentRequests: RequestDto[] = [];

  constructor(
    private authService: AuthService,
    private userContext: CurrentUserContextService,
    private requestService: RequestService,
    private router: Router,
    private excelExportService: ExcelExportService,
    private message: NzMessageService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    if (this.hasLoggedIn) {
      this.initUserInfo();
      this.loadDashboardData();
    }
  }

  login(): void {
    this.authService.navigateToLogin();
  }

  initUserInfo(): void {
    this.currentUserId = this.userContext.userId;
    this.currentUserFullName = this.userContext.fullName;
    this.currentUserEmail = this.userContext.email;
    this.userRoles = this.userContext.roles;
    this.isAdmin = this.userContext.isAdmin();
    this.isApprover = this.userContext.isApprover();

    // Thiết lập góc nhìn mặc định theo quyền hạn cao nhất của người dùng
    if (this.isAdmin) {
      this.activeRoleView = 'admin';
    } else if (this.isApprover) {
      this.activeRoleView = 'approver';
    } else {
      this.activeRoleView = 'employee';
    }
  }

  loadDashboardData(): void {
    if (this.isAdmin && this.activeRoleView === 'employee') {
      this.activeRoleView = 'admin';
    }
    this.loading = true;
    const input: GetRequestListInput = {
      skipCount: 0,
      maxResultCount: 1000,
      sorting: 'CreationTime desc',
    };

    this.requestService.getList(input).subscribe({
      next: res => {
        this.allRequests = res.items || [];
        this.calculateStatistics();
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }

  calculateStatistics(): void {
    const requests = this.allRequests;
    const myId = this.currentUserId;

    // 1. Thống kê góc nhìn Quản trị viên (Admin)
    this.adminTotalCount = requests.length;
    this.adminTotalAmount = requests.reduce((sum, r) => sum + (r.amount || 0), 0);
    this.adminApprovedAmount = requests
      .filter(r => r.status === RequestStatus.Approved)
      .reduce((sum, r) => sum + (r.amount || 0), 0);

    const approvedCount = requests.filter(r => r.status === RequestStatus.Approved).length;
    const rejectedCount = requests.filter(r => r.status === RequestStatus.Rejected).length;
    const completedCount = approvedCount + rejectedCount;
    this.adminApprovalRate =
      completedCount > 0
        ? Math.round((approvedCount / completedCount) * 100)
        : approvedCount > 0
          ? 100
          : 0;

    // Tạo danh sách thẻ trạng thái động cho Admin
    this.adminStatusCards = this.statusList.map(st => {
      const count = requests.filter(r => r.status === st.value).length;
      let subtext = st.subtext;
      if (st.value === RequestStatus.Approved) {
        subtext = `Tỷ lệ duyệt thành công: ${this.adminApprovalRate}%`;
      }
      return {
        meta: st,
        count,
        subtext,
      };
    });

    this.calculateTypeStats(requests);
    this.calculateDepartmentStats(requests);
    this.onDepartmentFilterChange(this.selectedDepartmentFilter);

    // 2. Thống kê góc nhìn Người phê duyệt (Approver)
    this.approverActionableRequests = requests
      .filter(r => r.status === RequestStatus.Submitted && (!myId || r.creatorId !== myId))
      .slice(0, 5);

    this.approverPendingCount = requests.filter(
      r => r.status === RequestStatus.Submitted && (!myId || r.creatorId !== myId),
    ).length;

    const approvedByMe = requests.filter(
      r => r.status === RequestStatus.Approved && (!r.approverId || r.approverId === myId),
    );
    this.approverApprovedCount = approvedByMe.length;
    this.approverTotalApprovedAmount = approvedByMe.reduce((sum, r) => sum + (r.amount || 0), 0);

    this.approverRejectedCount = requests.filter(
      r => r.status === RequestStatus.Rejected && (!r.approverId || r.approverId === myId),
    ).length;

    // 3. Thống kê góc nhìn Cá nhân (Employee / Requester)
    const myRequests = requests.filter(r => !myId || r.creatorId === myId);
    this.employeeTotalCount = myRequests.length;
    this.employeeTotalAmount = myRequests.reduce((sum, r) => sum + (r.amount || 0), 0);
    this.employeeDraftCount = myRequests.filter(r => r.status === RequestStatus.Draft).length;

    // Tạo các thẻ trạng thái cá nhân động
    this.employeeStatusCards = this.statusList.map(st => {
      const count = myRequests.filter(r => r.status === st.value).length;
      return {
        meta: st,
        count,
        subtext: st.mySubtext,
      };
    });

    this.employeeRecentRequests = myRequests.slice(0, 5);
  }

  private calculateTypeStats(requests: RequestDto[]): void {
    const total = requests.length || 1;
    this.adminTypeStats = this.typeList.map(tp => {
      const matchingReqs = requests.filter(r => r.type === tp.value);
      return {
        type: tp.value,
        label: tp.label,
        count: matchingReqs.length,
        amount: matchingReqs.reduce((s, r) => s + (r.amount || 0), 0),
        percent: Math.round((matchingReqs.length / total) * 100),
        color: '#4b5563',
      };
    });
  }

  private calculateDepartmentStats(requests: RequestDto[]): void {
    const total = requests.length || 1;
    const deptMap = new Map<
      string,
      { code: string; name: string; count: number; totalAmount: number }
    >();
    requests.forEach(r => {
      const code = r.departmentCode || 'OTHER';
      const name = r.departmentName || 'Chưa gán phòng ban';
      if (!deptMap.has(code)) {
        deptMap.set(code, { code, name, count: 0, totalAmount: 0 });
      }
      const item = deptMap.get(code)!;
      item.count++;
      item.totalAmount += r.amount || 0;
    });

    this.adminDepartmentStats = Array.from(deptMap.values())
      .map(d => ({
        code: d.code,
        name: d.name,
        count: d.count,
        totalAmount: d.totalAmount,
        percent: Math.round((d.count / total) * 100),
        color: '#4b5563',
      }))
      .sort((a, b) => b.count - a.count);
  }

  onDepartmentFilterChange(code: string): void {
    this.selectedDepartmentFilter = code;
    if (code === 'all') {
      this.adminRecentRequests = this.allRequests.slice(0, 5);
    } else {
      this.adminRecentRequests = this.allRequests
        .filter(r => r.departmentCode === code)
        .slice(0, 5);
    }
    this.cdr.markForCheck();
  }

  // Đếm số lượng đề xuất theo trạng thái trong danh sách truyền vào
  getStatusCount(requests: RequestDto[], status: RequestStatus): number {
    return requests.filter(r => r.status === status).length;
  }

  // Điều hướng nhanh sang trang quản lý đề xuất
  navigateToCreate(): void {
    this.router.navigate(['/requests'], { queryParams: { action: 'create' } });
  }

  navigateToRequests(status?: RequestStatus, scope: 'my' | 'all' = 'my'): void {
    const queryParams: any = { scope };
    if (status !== undefined) {
      queryParams.status = status;
    }
    this.router.navigate(['/requests'], { queryParams });
  }

  viewRequest(id: string): void {
    this.router.navigate(['/requests'], { queryParams: { requestId: id } });
  }

  exportDashboardReport(): void {
    if (this.allRequests.length === 0) {
      this.message.warning('Chưa có dữ liệu đề xuất nào để xuất báo cáo!');
      return;
    }
    this.isExporting = true;
    const scopeLabel =
      this.activeRoleView === 'admin'
        ? 'Toàn hệ thống (Admin)'
        : this.activeRoleView === 'approver'
          ? 'Người phê duyệt (Approver)'
          : 'Cá nhân (Employee)';

    try {
      this.excelExportService.exportDashboardReport({
        totalCount:
          this.activeRoleView === 'admin' ? this.adminTotalCount : this.employeeTotalCount,
        totalAmount:
          this.activeRoleView === 'admin' ? this.adminTotalAmount : this.employeeTotalAmount,
        approvedAmount: this.activeRoleView === 'admin' ? this.adminApprovedAmount : 0,
        approvalRate: this.adminApprovalRate,
        statusStats: (this.activeRoleView === 'admin'
          ? this.adminStatusCards
          : this.employeeStatusCards
        ).map(s => ({
          label: s.meta.label,
          count: s.count,
        })),
        typeStats: this.adminTypeStats.map(t => ({
          label: t.label,
          count: t.count,
          amount: t.amount,
          percent: t.percent,
        })),
        requests: this.allRequests,
        scopeName: scopeLabel,
        exporterName: this.currentUserFullName,
      });

      this.message.success('Xuất file báo cáo Excel thành công!');
    } catch {
      this.message.error('Có lỗi xảy ra khi tạo file Excel!');
    } finally {
      this.isExporting = false;
    }
  }

  // Các hàm hỗ trợ hiển thị nhãn và định dạng tiền tệ
  getTypeTag(type: RequestType): RequestTypeMeta {
    return getRequestTypeMeta(type);
  }

  getStatusTag(status: RequestStatus): RequestStatusMeta {
    return getRequestStatusMeta(status);
  }

  getDepartmentColor(code?: string): string {
    switch ((code || '').toUpperCase()) {
      case 'IT':
        return '#3b82f6';
      case 'HR':
        return '#10b981';
      case 'FINANCE':
        return '#f59e0b';
      case 'SALES':
        return '#8b5cf6';
      default:
        return '#64748b';
    }
  }

  formatMoney(amount?: number): string {
    return formatMoneyVnd(amount);
  }
}
