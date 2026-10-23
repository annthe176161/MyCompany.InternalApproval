import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ConfigStateService, PermissionService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzTableQueryParams } from 'ng-zorro-antd/table';
import { EmployeeService } from '../proxy/employees/employee.service';
import {
  EmployeeListItemDto,
  EmployeeLeaveReportItemDto,
  GetEmployeeListInput,
  GetLeaveReportInput,
  HrAnalyticsDto,
} from '../proxy/employees/models';
import { DepartmentService } from '../proxy/departments/department.service';
import { DepartmentDto } from '../proxy/departments/models';
import { EmployeeDetailDrawerComponent } from '../shared/components/employee-detail-drawer/employee-detail-drawer.component';
import { ExcelExportService } from '../shared/services/excel-export.service';

@Component({
  selector: 'app-employee',
  templateUrl: './employee.component.html',
  styleUrls: ['./employee.component.scss'],
})
export class EmployeeComponent implements OnInit, OnDestroy {
  @ViewChild('employeeDrawer') employeeDrawer?: EmployeeDetailDrawerComponent;

  // State: Main Tab Navigation
  selectedMainTabIndex = 0;

  // State: User context & Role permissions
  currentUserId?: string;
  currentUserName?: string;
  currentUserFullName?: string;
  isAdmin = false;
  isDepartmentManager = false;
  managedDepartments: DepartmentDto[] = [];
  availableDepartments: DepartmentDto[] = [];

  // State: Tab 1 - Employee List Table
  employees: EmployeeListItemDto[] = [];
  totalCount = 0;
  loadingEmployees = false;
  pageIndex = 1;
  pageSize = 10;
  sorting = '';

  // State: Tab 1 - Search & Filters
  searchTerm = '';
  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;
  selectedDepartmentId: string | null = null;
  departments: DepartmentDto[] = [];
  loadingDepartments = false;

  // State: Tab 1 - HR Analytics
  currentYear: number = new Date().getFullYear();
  analytics: HrAnalyticsDto | null = null;
  loadingAnalytics = false;

  // State: Tab 2 - Leave Report Table
  leaveReportItems: EmployeeLeaveReportItemDto[] = [];
  leaveTotalCount = 0;
  loadingLeaveReport = false;
  leavePageIndex = 1;
  leavePageSize = 10;
  leaveSorting = '';

  // State: Tab 2 - Leave Filters
  selectedLeaveYear: number = new Date().getFullYear();
  availableYears: number[] = [2026, 2025, 2024, 2023];
  selectedLeaveDepartmentId: string | null = null;
  leaveSearchTerm = '';
  private leaveSearchSubject = new Subject<string>();
  private leaveSearchSubscription?: Subscription;

  // State: Tab 2 - Summary Metrics
  totalApprovedLeaveDays = 0;
  totalApprovedLeaveRequests = 0;
  totalSentLeaveRequests = 0;

  // State: Tab 2 - Export Excel
  isExportingLeaveExcel = false;

  constructor(
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private configState: ConfigStateService,
    private permissionService: PermissionService,
    private excelExportService: ExcelExportService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.initCurrentUserAndRoles();
    this.initSearchSubscription();
    this.initLeaveSearchSubscription();
    this.loadDepartments();
    this.loadAnalytics();
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
    this.leaveSearchSubscription?.unsubscribe();
  }

  private initCurrentUserAndRoles(): void {
    const user = this.configState.getOne('currentUser');
    this.currentUserId = user?.id;
    this.currentUserName = user?.userName;
    this.currentUserFullName = user?.name
      ? `${user.name} ${user.surName || ''}`.trim()
      : user?.userName;

    const roles = (user?.roles || []).map((r: string) => r.toLowerCase());
    this.isAdmin =
      roles.includes('admin') ||
      user?.userName?.toLowerCase() === 'admin' ||
      this.permissionService.getGrantedPolicy('Employees.ViewAll') ||
      this.permissionService.getGrantedPolicy('AbpIdentity.Roles');
  }

  private initSearchSubscription(): void {
    this.searchSubscription = this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged())
      .subscribe(() => {
        this.pageIndex = 1;
        this.loadEmployees();
      });
  }

  loadDepartments(): void {
    this.loadingDepartments = true;
    this.departmentService.getList({ skipCount: 0, maxResultCount: 100 }).subscribe({
      next: res => {
        this.departments = res.items || [];
        this.evaluateDepartmentPermissions();
        this.loadingDepartments = false;
      },
      error: () => {
        this.loadingDepartments = false;
      },
    });
  }

  private initLeaveSearchSubscription(): void {
    this.leaveSearchSubscription = this.leaveSearchSubject
      .pipe(debounceTime(350), distinctUntilChanged())
      .subscribe(() => {
        this.leavePageIndex = 1;
        this.loadLeaveReport();
      });
  }

  private evaluateDepartmentPermissions(): void {
    if (this.currentUserId) {
      this.managedDepartments = this.departments.filter(
        d => d.managerId && d.managerId.toLowerCase() === this.currentUserId?.toLowerCase(),
      );
    } else {
      this.managedDepartments = [];
    }

    this.isDepartmentManager = !this.isAdmin && this.managedDepartments.length > 0;

    if (this.isAdmin) {
      this.availableDepartments = this.departments;
    } else if (this.isDepartmentManager) {
      this.availableDepartments = this.managedDepartments;
      // Tự động chọn nếu chỉ quản lý đúng 1 phòng ban
      if (this.managedDepartments.length === 1) {
        this.selectedDepartmentId = this.managedDepartments[0].id ?? null;
        this.selectedLeaveDepartmentId = this.managedDepartments[0].id ?? null;
      }
    } else {
      // Nhân viên thông thường: chỉ xem cá nhân
      this.availableDepartments = [];
    }
  }

  loadAnalytics(): void {
    this.loadingAnalytics = true;
    this.employeeService.getAnalytics(this.currentYear).subscribe({
      next: res => {
        this.analytics = res;
        this.loadingAnalytics = false;
      },
      error: () => {
        this.loadingAnalytics = false;
      },
    });
  }

  // ================= TAB 1 METHODS =================
  loadEmployees(): void {
    this.loadingEmployees = true;
    const input: GetEmployeeListInput = {
      filter: this.searchTerm ? this.searchTerm.trim() : undefined,
      departmentId: this.selectedDepartmentId || undefined,
      sorting: this.sorting || undefined,
      skipCount: (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    };

    this.employeeService.getList(input).subscribe({
      next: res => {
        this.employees = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.loadingEmployees = false;
      },
      error: err => {
        this.loadingEmployees = false;
        const msg = err.error?.error?.message || 'Không thể tải danh sách nhân viên!';
        this.message.error(msg);
      },
    });
  }

  onSearchChange(value: string): void {
    this.searchSubject.next(value);
  }

  onSearchClear(): void {
    this.searchTerm = '';
    this.pageIndex = 1;
    this.loadEmployees();
  }

  onDepartmentChange(): void {
    this.pageIndex = 1;
    this.loadEmployees();
  }

  onQueryParamsChange(params: NzTableQueryParams): void {
    const { pageSize, pageIndex, sort } = params;
    const currentSort = sort.find(item => item.value !== null);
    const sortField = (currentSort && currentSort.key) || '';
    const sortOrder = (currentSort && currentSort.value) || '';

    this.pageIndex = pageIndex;
    this.pageSize = pageSize;
    this.sorting = sortField ? `${sortField} ${sortOrder === 'ascend' ? 'asc' : 'desc'}` : '';

    this.loadEmployees();
  }

  // ================= TAB 2 METHODS (LEAVE REPORT) =================
  loadLeaveReport(): void {
    this.loadingLeaveReport = true;
    const input: GetLeaveReportInput = {
      year: this.selectedLeaveYear,
      departmentId: this.selectedLeaveDepartmentId || undefined,
      filter: this.leaveSearchTerm ? this.leaveSearchTerm.trim() : undefined,
      sorting: this.leaveSorting || undefined,
      skipCount: (this.leavePageIndex - 1) * this.leavePageSize,
      maxResultCount: this.leavePageSize,
    };

    this.employeeService.getLeaveReport(input).subscribe({
      next: res => {
        this.leaveReportItems = res.items || [];
        this.leaveTotalCount = res.totalCount || 0;
        this.calculateLeaveSummary();
        this.loadingLeaveReport = false;
      },
      error: err => {
        this.loadingLeaveReport = false;
        const msg = err.error?.error?.message || 'Không thể tải báo cáo ngày nghỉ phép!';
        this.message.error(msg);
      },
    });
  }

  private calculateLeaveSummary(): void {
    let days = 0;
    let approved = 0;
    let sent = 0;
    for (const item of this.leaveReportItems) {
      days += item.totalApprovedLeaveDays || 0;
      approved += item.totalApprovedLeaveRequests || 0;
      sent += item.totalSentLeaveRequests || 0;
    }
    this.totalApprovedLeaveDays = Math.round(days * 10) / 10;
    this.totalApprovedLeaveRequests = approved;
    this.totalSentLeaveRequests = sent;
  }

  onLeaveSearchChange(value: string): void {
    this.leaveSearchSubject.next(value);
  }

  onLeaveSearchClear(): void {
    this.leaveSearchTerm = '';
    this.leavePageIndex = 1;
    this.loadLeaveReport();
  }

  onLeaveYearChange(): void {
    this.leavePageIndex = 1;
    this.loadLeaveReport();
  }

  onLeaveDepartmentChange(): void {
    this.leavePageIndex = 1;
    this.loadLeaveReport();
  }

  onLeaveQueryParamsChange(params: NzTableQueryParams): void {
    const { pageSize, pageIndex, sort } = params;
    const currentSort = sort.find(item => item.value !== null);
    const sortField = (currentSort && currentSort.key) || '';
    const sortOrder = (currentSort && currentSort.value) || '';

    this.leavePageIndex = pageIndex;
    this.leavePageSize = pageSize;
    this.leaveSorting = sortField ? `${sortField} ${sortOrder === 'ascend' ? 'asc' : 'desc'}` : '';

    this.loadLeaveReport();
  }

  exportLeaveReport(): void {
    this.isExportingLeaveExcel = true;
    const input: GetLeaveReportInput = {
      year: this.selectedLeaveYear,
      departmentId: this.selectedLeaveDepartmentId || undefined,
      filter: this.leaveSearchTerm ? this.leaveSearchTerm.trim() : undefined,
      sorting: this.leaveSorting || undefined,
      skipCount: 0,
      maxResultCount: 1000,
    };

    this.employeeService.getLeaveReport(input).subscribe({
      next: res => {
        const items = res.items || [];
        let scopeName = 'Toàn công ty';
        if (this.selectedLeaveDepartmentId) {
          const dept = this.departments.find(d => d.id === this.selectedLeaveDepartmentId);
          scopeName = dept ? dept.name : 'Phòng ban';
        } else if (this.isDepartmentManager) {
          scopeName =
            this.managedDepartments.length === 1
              ? this.managedDepartments[0].name
              : 'Các phòng ban quản lý';
        } else if (!this.isAdmin) {
          scopeName = 'Cá nhân';
        }

        const exporter = this.currentUserFullName || this.currentUserName || 'Người dùng';
        this.excelExportService.exportLeaveStatistics(
          items,
          exporter,
          scopeName,
          this.selectedLeaveYear,
          this.leaveSearchTerm ? this.leaveSearchTerm.trim() : undefined,
        );
        this.message.success(
          `Xuất file Excel báo cáo ngày nghỉ phép thành công! (${items.length} bản ghi)`,
        );
        this.isExportingLeaveExcel = false;
      },
      error: err => {
        this.isExportingLeaveExcel = false;
        const msg = err.error?.error?.message || 'Không thể xuất file Excel báo cáo!';
        this.message.error(msg);
      },
    });
  }

  // ================= NAVIGATION & COMMON =================
  onMainTabChange(index: number): void {
    this.selectedMainTabIndex = index;
    if (index === 1 && this.leaveReportItems.length === 0 && !this.loadingLeaveReport) {
      this.loadLeaveReport();
    }
  }

  refreshCurrentTab(): void {
    if (this.selectedMainTabIndex === 0) {
      this.refreshAll();
    } else {
      this.loadLeaveReport();
      this.message.success('Đã làm mới báo cáo ngày nghỉ phép thành công!');
    }
  }

  openEmployeeDrawer(employeeId?: string, tabIndex: number = 0): void {
    if (!employeeId) return;
    this.employeeDrawer?.open(employeeId, tabIndex);
  }

  onJoinDateUpdated(): void {
    this.loadEmployees();
    this.loadAnalytics();
    if (this.selectedMainTabIndex === 1) {
      this.loadLeaveReport();
    }
  }

  refreshAll(): void {
    this.loadAnalytics();
    this.loadEmployees();
    this.message.success('Đã làm mới dữ liệu thành công!');
  }

  // Visual helper methods
  getInitials(name?: string): string {
    if (!name) return 'U';
    const parts = name.trim().split(/\s+/);
    if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
    return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
  }

  getAvatarColor(name?: string): string {
    if (!name) return '#475569';
    const colors = [
      '#0284c7', // Sky
      '#059669', // Emerald
      '#7c3aed', // Violet
      '#d97706', // Amber
      '#dc2626', // Red
      '#0891b2', // Cyan
      '#4f46e5', // Indigo
      '#be185d', // Pink
    ];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    const index = Math.abs(hash) % colors.length;
    return colors[index];
  }

  getTenureBadgeStatus(months: number): { color: string; label: string } {
    if (months < 2) {
      return { color: 'default', label: 'Thử việc' };
    }
    if (months < 12) {
      return { color: 'blue', label: 'Dưới 1 năm' };
    }
    if (months < 36) {
      return { color: 'green', label: '1 - 3 năm' };
    }
    return { color: 'purple', label: 'Kỳ cựu (> 3 năm)' };
  }

  trackById(_: number, item: EmployeeListItemDto): string {
    return item.id ?? '';
  }

  trackDeptById(_: number, item: DepartmentDto): string {
    return item.id ?? '';
  }

  trackByLeaveItem(index: number, item: EmployeeLeaveReportItemDto): string {
    return item.id || item.userName || String(index);
  }
}
