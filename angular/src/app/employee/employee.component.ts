import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subject, Subscription, forkJoin } from 'rxjs';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs/operators';
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
import { CurrentUserContextService, ExcelExportService } from '../shared/services';
import {
  extractErrorMessage,
  getAvatarColor,
  getAvatarInitials,
  getTenureBadgeStatus,
} from '../shared/utils';

// Màn hình quản lý danh sách hồ sơ nhân sự, thống kê HR và báo cáo ngày nghỉ phép
@Component({
  selector: 'app-employee',
  templateUrl: './employee.component.html',
  styleUrls: ['./employee.component.scss'],
})
export class EmployeeComponent implements OnInit, OnDestroy {
  @ViewChild('employeeDrawer') employeeDrawer?: EmployeeDetailDrawerComponent;

  // Điều hướng tab chính
  selectedMainTabIndex = 0;

  // Thông tin người dùng hiện tại và phạm vi quyền hạn
  currentUserId?: string;
  currentUserName?: string;
  currentUserFullName?: string;
  isAdmin = false;
  isDepartmentManager = false;
  managedDepartments: DepartmentDto[] = [];
  availableDepartments: DepartmentDto[] = [];

  // Trạng thái Tab 1: Danh sách nhân viên
  employees: EmployeeListItemDto[] = [];
  totalCount = 0;
  loadingEmployees = false;
  pageIndex = 1;
  pageSize = 10;
  sorting = '';

  // Bộ lọc tìm kiếm Tab 1
  searchTerm = '';
  private searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;
  selectedDepartmentId: string | null = null;
  departments: DepartmentDto[] = [];
  loadingDepartments = false;

  // Chỉ số phân tích nhân sự (HR Analytics) Tab 1
  currentYear: number = new Date().getFullYear();
  analytics: HrAnalyticsDto | null = null;
  loadingAnalytics = false;

  // Trạng thái Tab 2: Báo cáo ngày nghỉ phép
  leaveReportItems: EmployeeLeaveReportItemDto[] = [];
  leaveTotalCount = 0;
  loadingLeaveReport = false;
  leavePageIndex = 1;
  leavePageSize = 10;
  leaveSorting = '';

  // Bộ lọc báo cáo nghỉ phép Tab 2
  selectedLeaveYear: number = new Date().getFullYear();
  availableYears: number[] = [2026, 2025, 2024, 2023];
  selectedLeaveDepartmentId: string | null = null;
  leaveSearchTerm = '';
  private leaveSearchSubject = new Subject<string>();
  private leaveSearchSubscription?: Subscription;

  // Tổng hợp số liệu nghỉ phép Tab 2
  totalApprovedLeaveDays = 0;
  totalApprovedLeaveRequests = 0;
  totalSentLeaveRequests = 0;

  // Trạng thái xuất Excel báo cáo nghỉ phép
  isExportingLeaveExcel = false;

  // ID của nhân viên đang được xuất file Excel cá nhân
  exportingEmployeeId: string | null = null;

  constructor(
    private employeeService: EmployeeService,
    private departmentService: DepartmentService,
    private userContext: CurrentUserContextService,
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
    this.currentUserId = this.userContext.userId || undefined;
    this.currentUserName = this.userContext.userName;
    this.currentUserFullName = this.userContext.fullName;
    this.isAdmin = this.userContext.isAdmin(['Employees.ViewAll']);
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
      // Tự động chọn nếu người dùng chỉ quản lý đúng 1 phòng ban
      if (this.managedDepartments.length === 1) {
        this.selectedDepartmentId = this.managedDepartments[0].id ?? null;
        this.selectedLeaveDepartmentId = this.managedDepartments[0].id ?? null;
      }
    } else {
      // Nhân viên thông thường chỉ xem thông tin cá nhân
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

  // Các phương thức xử lý dữ liệu Tab 1 (Danh sách nhân sự)
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
        this.message.error(extractErrorMessage(err, 'Không thể tải danh sách nhân viên!'));
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

  // Các phương thức xử lý dữ liệu Tab 2 (Báo cáo ngày nghỉ phép)
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
        this.message.error(extractErrorMessage(err, 'Không thể tải báo cáo ngày nghỉ phép!'));
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
        this.message.error(extractErrorMessage(err, 'Không thể xuất file Excel báo cáo!'));
      },
    });
  }

  // Xuất file Excel báo cáo chi tiết cá nhân: số lần nghỉ phép và toàn bộ đề xuất của 1 nhân sự
  exportSingleEmployeeExcel(emp: EmployeeListItemDto): void {
    if (!emp?.id) return;

    this.exportingEmployeeId = emp.id;
    forkJoin({
      profile: this.employeeService.get(emp.id),
      yearlyStats: this.employeeService.getYearlyStats(emp.id, this.currentYear),
      requests: this.employeeService.getRequests(emp.id, { skipCount: 0, maxResultCount: 1000 }),
      leaveTimeline: this.employeeService.getLeaveTimeline(emp.id, this.currentYear),
    })
      .pipe(finalize(() => (this.exportingEmployeeId = null)))
      .subscribe({
        next: ({ profile, yearlyStats, requests, leaveTimeline }) => {
          const exporter = this.currentUserFullName || this.currentUserName || 'Quản lý';
          this.excelExportService.exportEmployeeDetailReport(
            profile,
            yearlyStats,
            requests.items || [],
            leaveTimeline || [],
            exporter,
          );
          this.message.success(
            `Đã xuất file Excel báo cáo thống kê cho nhân sự "${profile.fullName}" thành công!`,
          );
        },
        error: err => {
          this.message.error(
            extractErrorMessage(err, 'Không thể xuất file Excel thống kê của nhân sự này!'),
          );
        },
      });
  }

  // Điều hướng tab và làm mới dữ liệu
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

  // Các hàm hỗ trợ hiển thị Avatar và nhãn thâm niên
  getInitials(name?: string): string {
    return getAvatarInitials(name);
  }

  getAvatarColor(name?: string): string {
    return getAvatarColor(name);
  }

  getTenureBadgeStatus(months: number): { color: string; label: string } {
    return getTenureBadgeStatus(months);
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
