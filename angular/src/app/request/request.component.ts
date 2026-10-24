import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PermissionService } from '@abp/ng.core';
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
import { ColumnConfig } from '../shared/models';
import { CurrentUserContextService, ExcelExportService } from '../shared/services';
import { calculateLeaveDays, extractErrorMessage } from '../shared/utils';

// Màn hình quản lý danh sách đề xuất và quy trình xét duyệt
@Component({
  selector: 'app-request',
  templateUrl: './request.component.html',
  styleUrls: ['./request.component.scss'],
})
export class RequestComponent implements OnInit {
  @ViewChild('detailDrawer') detailDrawer?: RequestDetailDrawerComponent;
  @ViewChild('formModal') formModal?: RequestFormModalComponent;
  @ViewChild('employeeDrawer') employeeDrawer?: EmployeeDetailDrawerComponent;

  // Mở ngăn kéo xem hồ sơ chi tiết của nhân viên
  openEmployeeDrawer(userId?: string): void {
    if (userId) {
      this.employeeDrawer?.open(userId);
    }
  }

  // Tham chiếu enum và danh mục dùng chung cho template
  RequestType = RequestType;
  RequestStatus = RequestStatus;
  statusList: RequestStatusMeta[] = REQUEST_STATUS_LIST;
  typeList: RequestTypeMeta[] = REQUEST_TYPE_LIST;

  // Cấu hình hiển thị cột và lưu trữ tùy chọn người dùng
  readonly COLUMN_STORAGE_KEY = 'internal_approval_request_table_columns';

  columns: ColumnConfig[] = [
    { key: 'code', label: 'Mã & Ngày tạo', visible: true, width: '120px' },
    { key: 'title', label: 'Tiêu đề & Loại', visible: true, width: '280px', required: true },
    { key: 'amount', label: 'Số tiền', visible: true, width: '150px' },
    { key: 'creator', label: 'Người tạo', visible: true, width: '160px' },
    { key: 'status', label: 'Trạng thái', visible: true, width: '130px' },
    { key: 'action', label: 'Hành động', visible: true, width: '160px', required: true },
  ];

  // Thông tin người dùng hiện tại và phạm vi dữ liệu
  currentUserId: string | null = null;
  isAdmin = false;
  isApprover = false;

  // Phạm vi dữ liệu: 'my' (cá nhân đăng nhập) hoặc 'all' (toàn hệ thống cho Admin/Approver)
  activeScope: 'my' | 'all' = 'my';

  // Trạng thái bảng dữ liệu
  requests: RequestDto[] = [];
  totalCount = 0;
  loading = false;
  pageIndex = 1;
  pageSize = 10;
  sort = 'CreationTime desc';

  // Bộ lọc tìm kiếm
  searchTerm = '';
  selectedStatus: RequestStatus | null = null;
  selectedType: RequestType | null = null;
  isExporting = false;

  constructor(
    private requestService: RequestService,
    private userContext: CurrentUserContextService,
    private permissionService: PermissionService,
    private message: NzMessageService,
    private excelExportService: ExcelExportService,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.userContext.userId;
    this.isAdmin = this.userContext.isAdmin();
    this.isApprover = this.userContext.isApprover();

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
      // Bỏ qua nếu có lỗi đọc JSON từ localStorage
    }
  }

  onColumnChange(): void {
    try {
      const data = this.columns.map(c => ({ key: c.key, visible: c.visible }));
      localStorage.setItem(this.COLUMN_STORAGE_KEY, JSON.stringify(data));
    } catch {
      // Bỏ qua lỗi lưu trữ localStorage
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
          // Đợi detailDrawer khởi tạo xong trong view
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
      creatorId: this.activeScope === 'my' ? this.currentUserId || undefined : undefined,
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
        this.message.error(extractErrorMessage(err, 'Không thể tải danh sách đề xuất!'));
      },
    });
  }

  exportExcel(): void {
    this.isExporting = true;
    const input: GetRequestListInput = {
      filter: this.searchTerm ? this.searchTerm.trim() : undefined,
      status: this.selectedStatus !== null ? this.selectedStatus : undefined,
      type: this.selectedType !== null ? this.selectedType : undefined,
      creatorId: this.activeScope === 'my' ? this.currentUserId || undefined : undefined,
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

        const exporterName = this.userContext.fullName;
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

  // Gửi đề xuất đi phê duyệt
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
        this.message.error(extractErrorMessage(err, 'Không thể gửi phê duyệt đề xuất!'));
      },
    });
  }

  // Xóa đề xuất ở trạng thái nháp
  delete(req: RequestDto): void {
    this.requestService.delete(req.id).subscribe({
      next: () => {
        this.message.success('Đã xóa đề xuất thành công!');
        this.loadData();
      },
      error: err => {
        this.message.error(extractErrorMessage(err, 'Không thể xóa đề xuất!'));
      },
    });
  }

  // Các hàm hỗ trợ hiển thị nhãn và tính số ngày nghỉ phép
  getTypeTag(type: RequestType): RequestTypeMeta {
    return getRequestTypeMeta(type);
  }

  getStatusTag(status: RequestStatus): RequestStatusMeta {
    return getRequestStatusMeta(status);
  }

  trackById(_: number, item: RequestDto): string {
    return item.id ?? '';
  }

  getLeaveDays(req: RequestDto): number | null {
    return calculateLeaveDays(req?.startDate, req?.endDate);
  }

  hasOtherActions(data: RequestDto): boolean {
    // Admin chỉ đóng vai trò giám sát, không trực tiếp thực hiện sửa/xóa/duyệt/từ chối
    if (this.isAdmin) {
      return false;
    }

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
