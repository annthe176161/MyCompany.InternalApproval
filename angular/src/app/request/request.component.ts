import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ConfigStateService, PermissionService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzTableQueryParams } from 'ng-zorro-antd/table';
import {
  RequestService,
  RequestDto,
  RequestType,
  RequestStatus,
  GetRequestListInput,
} from '../proxy/requests';
import { RequestDetailDrawerComponent } from './components/request-detail-drawer/request-detail-drawer.component';
import { RequestFormModalComponent } from './components/request-form-modal/request-form-modal.component';
import { EmployeeDetailDrawerComponent } from '../shared/components/employee-detail-drawer/employee-detail-drawer.component';
import {
  REQUEST_STATUS_LIST,
  getRequestStatusMeta,
  RequestStatusMeta,
} from '../shared/constants/request-status.constant';
import {
  REQUEST_TYPE_LIST,
  getRequestTypeMeta,
  RequestTypeMeta,
} from '../shared/constants/request-type.constant';
import { ExcelExportService } from '../shared/services/excel-export.service';

export interface ColumnConfig {
  key: string;
  label: string;
  visible: boolean;
  width: string;
  required?: boolean;
}

@Component({
  selector: 'app-request',
  templateUrl: './request.component.html',
  styleUrls: ['./request.component.scss'],
})
export class RequestComponent implements OnInit {
  @ViewChild('detailDrawer') detailDrawer?: RequestDetailDrawerComponent;
  @ViewChild('formModal') formModal?: RequestFormModalComponent;
  @ViewChild('employeeDrawer') employeeDrawer?: EmployeeDetailDrawerComponent;

  openEmployeeDrawer(userId?: string): void {
    if (userId) {
      this.employeeDrawer?.open(userId);
    }
  }

  // Enum & shared metadata references for template
  RequestType = RequestType;
  RequestStatus = RequestStatus;
  statusList: RequestStatusMeta[] = REQUEST_STATUS_LIST;
  typeList: RequestTypeMeta[] = REQUEST_TYPE_LIST;

  // Cấu hình hiển thị cột & lưu trữ tùy chọn người dùng
  readonly COLUMN_STORAGE_KEY = 'internal_approval_request_table_columns';

  columns: ColumnConfig[] = [
    { key: 'code', label: 'Mã & Ngày tạo', visible: true, width: '120px' },
    { key: 'title', label: 'Tiêu đề & Loại', visible: true, width: '280px', required: true },
    { key: 'amount', label: 'Số tiền', visible: true, width: '150px' },
    { key: 'creator', label: 'Người tạo', visible: true, width: '160px' },
    { key: 'status', label: 'Trạng thái', visible: true, width: '130px' },
    { key: 'action', label: 'Hành động', visible: true, width: '160px', required: true },
  ];

  // Current user info & scope
  currentUserId: string | null = null;
  isAdmin = false;
  isApprover = false;

  // Phạm vi dữ liệu: 'my' (Chỉ của tài khoản đăng nhập - MẶC ĐỊNH) hoặc 'all' (Toàn hệ thống cho Admin/Approver)
  activeScope: 'my' | 'all' = 'my';

  // Table state
  requests: RequestDto[] = [];
  totalCount = 0;
  loading = false;
  pageIndex = 1;
  pageSize = 10;
  sort = 'CreationTime desc';

  // Filters
  searchTerm = '';
  selectedStatus: RequestStatus | null = null;
  selectedType: RequestType | null = null;
  isExporting = false;

  constructor(
    private requestService: RequestService,
    private configState: ConfigStateService,
    private permissionService: PermissionService,
    private message: NzMessageService,
    private excelExportService: ExcelExportService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    const user = this.configState.getOne('currentUser');
    this.currentUserId = user?.id || null;
    const roles = (user?.roles || []).map(r => r.toLowerCase());
    this.isAdmin =
      roles.includes('admin') ||
      user?.userName?.toLowerCase() === 'admin' ||
      this.permissionService.getGrantedPolicy('AbpIdentity.Roles');
    this.isApprover =
      this.isAdmin ||
      this.permissionService.getGrantedPolicy('Requests.Approve') ||
      roles.some(r => r.includes('manager') || r.includes('leader'));

    // Tải cấu hình cột đã lưu từ localStorage
    this.loadColumnsFromStorage();

    // Lắng nghe queryParams để tự động mở chi tiết đề xuất hoặc tạo mới từ Dashboard
    this.route.queryParams.subscribe(params => {
      if (this.isAdmin) {
        this.activeScope = 'all';
      } else if (params['scope'] === 'all' && this.isApprover) {
        this.activeScope = 'all';
      } else if (params['scope'] === 'my') {
        this.activeScope = 'my';
      }

      const targetRequestId = params['requestId'];
      if (targetRequestId) {
        this.openDetailById(targetRequestId);
      }
      if (params['action'] === 'create') {
        setTimeout(() => {
          this.formModal?.openCreate();
        }, 300);
      }
      if (params['status'] !== undefined && params['status'] !== null && params['status'] !== '') {
        this.selectedStatus = Number(params['status']);
        this.pageIndex = 1;
        this.loadData();
      }
    });
  }

  isColumnVisible(key: string): boolean {
    const col = this.columns.find(c => c.key === key);
    return col ? col.visible : true;
  }

  get tableScrollX(): string {
    const totalWidth = this.columns
      .filter(c => c.visible)
      .reduce((sum, col) => sum + parseInt(col.width, 10), 0);
    return `${Math.max(totalWidth, 750)}px`;
  }

  loadColumnsFromStorage(): void {
    try {
      const saved = localStorage.getItem(this.COLUMN_STORAGE_KEY);
      if (saved) {
        const parsed: { key: string; visible: boolean }[] = JSON.parse(saved);
        parsed.forEach(item => {
          const col = this.columns.find(c => c.key === item.key);
          if (col && !col.required) {
            col.visible = item.visible;
          }
        });
      }
    } catch {
      // Bỏ qua nếu có lỗi parse JSON
    }
  }

  onColumnChange(): void {
    try {
      const data = this.columns.map(c => ({ key: c.key, visible: c.visible }));
      localStorage.setItem(this.COLUMN_STORAGE_KEY, JSON.stringify(data));
    } catch {
      // Bỏ qua lỗi lưu storage
    }
  }

  resetColumns(): void {
    this.columns.forEach(col => (col.visible = true));
    this.onColumnChange();
    this.message.success('Đã khôi phục hiển thị tất cả các cột');
  }

  openDetailById(id: string): void {
    this.requestService.get(id).subscribe({
      next: req => {
        if (this.detailDrawer) {
          this.detailDrawer.open(req);
        } else {
          // Trường hợp detailDrawer chưa kịp render trong view
          setTimeout(() => {
            this.detailDrawer?.open(req);
          }, 300);
        }
      },
      error: () => {
        this.message.warning('Không tìm thấy thông tin đề xuất hoặc bạn không có quyền xem!');
      },
    });
  }

  onScopeChange(scope: 'my' | 'all'): void {
    this.activeScope = scope;
    this.pageIndex = 1;
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    const input: GetRequestListInput = {
      filter: this.searchTerm ? this.searchTerm.trim() : undefined,
      status: this.selectedStatus !== null ? this.selectedStatus : undefined,
      type: this.selectedType !== null ? this.selectedType : undefined,
      creatorId: this.activeScope === 'my' ? (this.currentUserId || undefined) : undefined,
      sorting: this.sort,
      skipCount: (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    };

    this.requestService.getList(input).subscribe({
      next: res => {
        this.requests = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        const msg = err.error?.error?.message || 'Không thể tải danh sách đề xuất!';
        this.message.error(msg);
      },
    });
  }

  exportExcel(): void {
    this.isExporting = true;
    const input: GetRequestListInput = {
      filter: this.searchTerm ? this.searchTerm.trim() : undefined,
      status: this.selectedStatus !== null ? this.selectedStatus : undefined,
      type: this.selectedType !== null ? this.selectedType : undefined,
      creatorId: this.activeScope === 'my' ? (this.currentUserId || undefined) : undefined,
      sorting: this.sort,
      skipCount: 0,
      maxResultCount: 1000,
    };

    this.requestService.getList(input).subscribe({
      next: res => {
        const items = res.items || [];
        if (items.length === 0) {
          this.message.warning('Không có dữ liệu đề xuất nào để xuất Excel!');
          this.isExporting = false;
          return;
        }

        const user = this.configState.getOne('currentUser');
        const exporterName = user?.name
          ? `${user.name} ${user.surName || ''}`.trim()
          : user?.userName || 'Người dùng';
        const scopeName = this.activeScope === 'my' ? 'Đề xuất của tôi' : 'Toàn hệ thống';

        let filterText = '';
        if (this.searchTerm) filterText += `Tìm: "${this.searchTerm}"; `;
        if (this.selectedStatus !== null && this.selectedStatus !== undefined) {
          filterText += `Trạng thái: ${this.getStatusTag(this.selectedStatus).label}; `;
        }
        if (this.selectedType !== null && this.selectedType !== undefined) {
          filterText += `Loại: ${this.getTypeTag(this.selectedType).label}; `;
        }

        this.excelExportService.exportRequests(items, exporterName, scopeName, filterText);
        this.message.success(`Xuất file Excel thành công! (${items.length} đề xuất)`);
        this.isExporting = false;
      },
      error: () => {
        this.message.error('Có lỗi xảy ra khi tải dữ liệu xuất Excel!');
        this.isExporting = false;
      },
    });
  }

  onQueryParamsChange(params: NzTableQueryParams): void {
    const { pageIndex, pageSize, sort } = params;
    this.pageIndex = pageIndex;
    this.pageSize = pageSize;

    const currentSort = sort.find(item => item.value !== null);
    if (currentSort) {
      this.sort = `${currentSort.key} ${currentSort.value === 'ascend' ? 'asc' : 'desc'}`;
    } else {
      this.sort = 'CreationTime desc';
    }

    this.loadData();
  }

  onSearch(): void {
    this.pageIndex = 1;
    this.loadData();
  }

  onFilterChange(): void {
    this.pageIndex = 1;
    this.loadData();
  }

  // Row Actions: Submit & Delete
  submit(req: RequestDto): void {
    this.requestService.submit(req.id).subscribe({
      next: () => {
        this.message.success('Đề xuất đã được gửi đi phê duyệt thành công!');
        this.loadData();
        if (this.detailDrawer?.detailRequest?.id === req.id) {
          this.detailDrawer.refreshDetail(req.id);
        }
      },
      error: err => {
        const msg = err.error?.error?.message || 'Không thể gửi phê duyệt đề xuất!';
        this.message.error(msg);
      },
    });
  }

  delete(req: RequestDto): void {
    this.requestService.delete(req.id).subscribe({
      next: () => {
        this.message.success('Đã xóa đề xuất thành công!');
        this.loadData();
      },
      error: err => {
        const msg = err.error?.error?.message || 'Không thể xóa đề xuất!';
        this.message.error(msg);
      },
    });
  }

  // Helpers for display (Dùng chung từ config chuẩn hóa)
  getTypeTag(type: RequestType): RequestTypeMeta {
    return getRequestTypeMeta(type);
  }

  getStatusTag(status: RequestStatus): RequestStatusMeta {
    return getRequestStatusMeta(status);
  }

  trackById(_: number, item: RequestDto): string {
    return item.id;
  }

  hasOtherActions(data: RequestDto): boolean {
    if (data.status === RequestStatus.Draft) {
      const isCreator = !data.creatorId || data.creatorId === this.currentUserId;
      const canEdit = this.permissionService.getGrantedPolicy('Requests.Edit');
      const canDelete = this.permissionService.getGrantedPolicy('Requests.Delete');
      return isCreator || canEdit || canDelete;
    }

    if (data.status === RequestStatus.Submitted) {
      const canApprove =
        this.permissionService.getGrantedPolicy('Requests.Approve') &&
        (!this.currentUserId || data.creatorId !== this.currentUserId);
      const canReject = this.permissionService.getGrantedPolicy('Requests.Reject');
      return canApprove || canReject;
    }

    return false;
  }
}
