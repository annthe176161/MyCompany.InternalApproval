import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import {
  PermissionGrantInfoDto,
  PermissionGroupDto,
  PermissionsService,
  UpdatePermissionDto,
} from '@abp/ng.permission-management/proxy';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize } from 'rxjs/operators';

// Cấu trúc dữ liệu mở rộng cho từng quyền để hiển thị cây phân cấp và mô tả dễ hiểu
export interface PermissionTreeItem {
  name: string;
  displayName: string;
  description: string;
  parentName?: string;
  isGranted: boolean;
  isDisabled: boolean;
  grantedByRoles: string[];
  depth: number;
  children: PermissionTreeItem[];
}

@Component({
  selector: 'app-permission-modal',
  standalone: false,
  templateUrl: './permission-modal.component.html',
  styleUrls: ['./permission-modal.component.scss'],
})
export class PermissionModalComponent implements OnChanges {
  @Input() visible = false;
  @Output() visibleChange = new EventEmitter<boolean>();

  // 'R' = Phân quyền theo Vai trò (Role), 'U' = Phân quyền theo Tài khoản (User)
  @Input() providerName: 'R' | 'U' = 'R';
  @Input() providerKey = '';
  @Input() entityTitle = '';

  @Output() saved = new EventEmitter<void>();

  isLoading = false;
  isSaving = false;
  searchKeyword = '';

  groups: PermissionGroupDto[] = [];
  selectedGroupIndex = 0;

  // Lưu trạng thái gốc ban đầu để chỉ gửi những quyền có thay đổi hoặc gửi toàn bộ danh sách phẳng
  private allPermissionsFlat: PermissionGrantInfoDto[] = [];

  // Từ điển giải thích ý nghĩa từng quyền bằng ngôn ngữ bình dân, dễ hiểu nhất cho người dùng
  private readonly permissionDescriptions: Record<string, string> = {
    // 1. Nhóm Tài khoản & Phân quyền (AbpIdentity)
    'AbpIdentity.Roles': 'Xem danh sách các vai trò (chức danh quyền hạn) trong công ty',
    'AbpIdentity.Roles.Create': 'Tạo thêm vai trò mới (ví dụ: Trưởng phòng, Kế toán, Nhân viên...)',
    'AbpIdentity.Roles.Update': 'Chỉnh sửa tên hoặc thiết lập Mặc định / Công khai của vai trò',
    'AbpIdentity.Roles.Delete': 'Xóa các vai trò không còn sử dụng khỏi hệ thống',
    'AbpIdentity.Roles.ManagePermissions': 'Bật/tắt các quyền hạn chi tiết cho từng vai trò',

    'AbpIdentity.Users': 'Xem danh sách tài khoản đăng nhập của nhân viên trong hệ thống',
    'AbpIdentity.Users.Create': 'Tạo tài khoản đăng nhập mới cho nhân viên mới vào công ty',
    'AbpIdentity.Users.Update': 'Cập nhật thông tin cá nhân, mật khẩu hoặc khóa/mở khóa tài khoản',
    'AbpIdentity.Users.Update.ManageRoles': 'Gán hoặc thay đổi vai trò (chức danh) cho tài khoản nhân viên',
    'AbpIdentity.Users.Delete': 'Xóa vĩnh viễn tài khoản nhân viên khỏi hệ thống',
    'AbpIdentity.Users.ManagePermissions': 'Cấp thêm quyền ngoại lệ riêng biệt cho từng cá nhân',
    'AbpIdentity.UserLookup': 'Cho phép tìm kiếm và tra cứu nhanh thông tin người dùng',

    // 2. Nhóm Cài đặt hệ thống (SettingManagement)
    'SettingManagement.Emailing': 'Cấu hình máy chủ gửi email thông báo tự động (SMTP)',
    'SettingManagement.Emailing.Test': 'Gửi email kiểm tra thử kết nối hệ thống',
    'SettingManagement.TimeZone': 'Thiết lập múi giờ chuẩn hiển thị trên toàn hệ thống',

    // 3. Nhóm Quản lý Đề xuất Nội bộ (InternalApproval)
    'Requests.Default': 'Truy cập menu Đề xuất nội bộ và xem danh sách các phiếu đề xuất',
    'Requests.Create': 'Tạo và gửi phiếu đề xuất mới (Xin nghỉ phép, Thanh toán, Mua sắm...)',
    'Requests.Edit': 'Chỉnh sửa nội dung phiếu đề xuất khi đang ở trạng thái Nháp',
    'Requests.Delete': 'Xóa hoặc thu hồi phiếu đề xuất nháp của bản thân',
    'Requests.Approve': 'Duyệt đồng ý các phiếu đề xuất của nhân viên gửi lên',
    'Requests.Reject': 'Từ chối các phiếu đề xuất không hợp lệ kèm lý do phản hồi',

    'Departments.Default': 'Truy cập menu Phòng ban và xem sơ đồ tổ chức công ty',
    'Departments.Create': 'Thành lập phòng ban hoặc bộ phận chuyên môn mới',
    'Departments.Edit': 'Đổi tên, mã phòng ban hoặc bổ nhiệm Trưởng phòng phụ trách',
    'Departments.Delete': 'Giải thể và xóa phòng ban (khi không còn nhân sự bên trong)',
    'Departments.ManageMembers': 'Thêm nhân viên vào phòng ban, chuyển phòng hoặc đổi chức vụ',

    'Employees.Default': 'Truy cập danh bạ Hồ sơ nhân viên trong công ty',
    'Employees.ViewAll': 'Xem toàn bộ hồ sơ nhân sự của tất cả các phòng ban',
    'Employees.Edit': 'Cập nhật thông tin hồ sơ công việc của nhân viên',
    'Employees.Analytics': 'Xem biểu đồ thống kê, báo cáo hiệu suất và tỷ lệ duyệt đề xuất',
  };

  // Từ điển tên nhóm quyền thân thiện
  private readonly groupNameOverrides: Record<string, string> = {
    AbpIdentity: 'Tài khoản & Phân quyền',
    IdentityManagement: 'Tài khoản & Phân quyền',
    SettingManagement: 'Cài đặt hệ thống',
    InternalApproval: 'Quản lý Đề xuất & Phòng ban',
  };

  // Từ điển tên quyền ngắn gọn, rõ nghĩa tiếng Việt
  private readonly permissionNameOverrides: Record<string, string> = {
    'AbpIdentity.Roles': 'Quản lý Vai trò',
    'AbpIdentity.Roles.Create': 'Thêm vai trò mới',
    'AbpIdentity.Roles.Update': 'Chỉnh sửa vai trò',
    'AbpIdentity.Roles.Delete': 'Xóa vai trò',
    'AbpIdentity.Roles.ManagePermissions': 'Thiết lập phân quyền cho vai trò',

    'AbpIdentity.Users': 'Quản lý Tài khoản người dùng',
    'AbpIdentity.Users.Create': 'Tạo tài khoản mới',
    'AbpIdentity.Users.Update': 'Cập nhật thông tin & mật khẩu',
    'AbpIdentity.Users.Update.ManageRoles': 'Gán vai trò cho tài khoản',
    'AbpIdentity.Users.Delete': 'Xóa tài khoản',
    'AbpIdentity.Users.ManagePermissions': 'Phân quyền riêng cho từng cá nhân',

    'Requests.Default': 'Quản lý Đề xuất nội bộ',
    'Requests.Create': 'Tạo phiếu đề xuất mới',
    'Requests.Edit': 'Chỉnh sửa phiếu đề xuất',
    'Requests.Delete': 'Xóa phiếu đề xuất nháp',
    'Requests.Approve': 'Phê duyệt đề xuất (Dành cho Quản lý)',
    'Requests.Reject': 'Từ chối đề xuất (Dành cho Quản lý)',

    'Departments.Default': 'Quản lý Phòng ban',
    'Departments.Create': 'Tạo phòng ban mới',
    'Departments.Edit': 'Sửa thông tin & Bổ nhiệm Trưởng phòng',
    'Departments.Delete': 'Xóa phòng ban',
    'Departments.ManageMembers': 'Quản lý nhân sự & chức vụ trong phòng',

    'Employees.Default': 'Quản lý Hồ sơ nhân viên',
    'Employees.ViewAll': 'Xem toàn bộ nhân viên công ty',
    'Employees.Edit': 'Chỉnh sửa hồ sơ nhân viên',
    'Employees.Analytics': 'Xem thống kê & phân tích dữ liệu',
  };

  constructor(
    private permissionsService: PermissionsService,
    private message: NzMessageService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    // Khi mở popup và có đầy đủ thông tin đối tượng cần phân quyền thì tải dữ liệu
    if (changes['visible'] && this.visible && this.providerKey) {
      this.searchKeyword = '';
      this.selectedGroupIndex = 0;
      this.loadPermissions();
    }
  }

  // Tải danh sách quyền từ hệ thống
  loadPermissions(): void {
    this.isLoading = true;
    this.permissionsService
      .get(this.providerName, this.providerKey)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: res => {
          this.groups = (res.groups || []).filter(g => g.permissions && g.permissions.length > 0);
          this.allPermissionsFlat = this.groups.reduce(
            (acc, g) => acc.concat(g.permissions || []),
            [] as PermissionGrantInfoDto[]
          );
          if (!this.entityTitle && res.entityDisplayName) {
            this.entityTitle = res.entityDisplayName;
          }
        },
        error: () => {
          this.message.error('Không thể tải danh sách quyền hạn. Vui lòng thử lại!');
          this.closeModal();
        },
      });
  }

  // Đóng popup phân quyền
  closeModal(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  // Lấy tên hiển thị thân thiện của nhóm quyền
  getGroupDisplayName(group: PermissionGroupDto): string {
    if (group.name && this.groupNameOverrides[group.name]) {
      return this.groupNameOverrides[group.name];
    }
    return group.displayName || group.name || 'Nhóm quyền';
  }

  // Lấy icon đại diện cho từng nhóm quyền
  getGroupIcon(group: PermissionGroupDto): string {
    const name = group.name || '';
    if (name.includes('Identity')) return 'safety-certificate';
    if (name.includes('InternalApproval')) return 'audit';
    if (name.includes('Setting')) return 'setting';
    return 'appstore';
  }

  // Lấy tên hiển thị rõ nghĩa của từng quyền
  getPermissionDisplayName(perm: PermissionGrantInfoDto): string {
    if (perm.name && this.permissionNameOverrides[perm.name]) {
      return this.permissionNameOverrides[perm.name];
    }
    return perm.displayName || perm.name || '';
  }

  // Lấy câu mô tả giải thích chi tiết tác dụng của quyền
  getPermissionDescription(perm: PermissionGrantInfoDto): string {
    if (perm.name && this.permissionDescriptions[perm.name]) {
      return this.permissionDescriptions[perm.name];
    }
    return `Cho phép thực hiện chức năng "${perm.displayName || perm.name}" trên hệ thống`;
  }

  // Kiểm tra quyền này có được cấp sẵn thông qua Vai trò (khi đang phân quyền cho Cá nhân User) hay không
  isGrantedByOtherProvider(perm: PermissionGrantInfoDto): boolean {
    if (this.providerName !== 'U' || !perm.grantedProviders?.length) {
      return false;
    }
    return perm.grantedProviders.some(p => p.providerName === 'R');
  }

  // Lấy danh sách tên Vai trò đã cấp quyền này (để hiển thị nhãn giải thích cho người dùng hiểu)
  getGrantedRoleNames(perm: PermissionGrantInfoDto): string[] {
    if (!perm.grantedProviders?.length) return [];
    return perm.grantedProviders
      .filter(p => p.providerName === 'R' && p.providerKey)
      .map(p => p.providerKey as string);
  }

  // Đếm số quyền đã bật trong 1 nhóm
  getGrantedCountInGroup(group: PermissionGroupDto): number {
    return (group.permissions || []).filter(p => p.isGranted).length;
  }

  // Tổng số quyền trên toàn hệ thống
  get totalPermissionsCount(): number {
    return this.allPermissionsFlat.length;
  }

  // Tổng số quyền đã được cấp trên toàn hệ thống
  get totalGrantedCount(): number {
    return this.allPermissionsFlat.filter(p => p.isGranted).length;
  }

  // Kiểm tra đã cấp toàn bộ quyền hệ thống chưa
  get isAllSystemGranted(): boolean {
    return (
      this.allPermissionsFlat.length > 0 &&
      this.allPermissionsFlat.every(p => p.isGranted)
    );
  }

  // Kiểm tra trạng thái chọn một phần của toàn hệ thống
  get isSystemIndeterminate(): boolean {
    const granted = this.totalGrantedCount;
    return granted > 0 && granted < this.totalPermissionsCount;
  }

  // Bật / Tắt toàn bộ quyền trên hệ thống
  toggleAllSystemPermissions(checked: boolean): void {
    this.allPermissionsFlat.forEach(p => {
      if (!this.isGrantedByOtherProvider(p)) {
        p.isGranted = checked;
      } else if (checked) {
        p.isGranted = true;
      }
    });
  }

  // Nhóm đang được chọn ở thanh bên trái
  get currentGroup(): PermissionGroupDto | null {
    return this.groups[this.selectedGroupIndex] || null;
  }

  // Kiểm tra tất cả quyền trong nhóm hiện tại đã được chọn chưa
  get isCurrentGroupAllGranted(): boolean {
    const perms = this.currentGroup?.permissions || [];
    return perms.length > 0 && perms.every(p => p.isGranted);
  }

  // Kiểm tra trạng thái chọn một phần của nhóm hiện tại
  get isCurrentGroupIndeterminate(): boolean {
    const perms = this.currentGroup?.permissions || [];
    const granted = perms.filter(p => p.isGranted).length;
    return granted > 0 && granted < perms.length;
  }

  // Bật / Tắt toàn bộ quyền trong nhóm đang mở
  toggleCurrentGroupPermissions(checked: boolean): void {
    const perms = this.currentGroup?.permissions || [];
    perms.forEach(p => {
      if (!this.isGrantedByOtherProvider(p)) {
        p.isGranted = checked;
      } else if (checked) {
        p.isGranted = true;
      }
    });
  }

  // Xây dựng danh sách cây phân cấp cho nhóm hiện tại (hoặc kết quả tìm kiếm trên toàn bộ các nhóm)
  get displayedParentPermissions(): PermissionTreeItem[] {
    const keyword = this.searchKeyword.trim().toLowerCase();

    // Nếu người dùng đang gõ tìm kiếm -> Tìm kiếm xuyên suốt tất cả các nhóm quyền
    if (keyword) {
      const matchedPerms = this.allPermissionsFlat.filter(p => {
        const title = this.getPermissionDisplayName(p).toLowerCase();
        const desc = this.getPermissionDescription(p).toLowerCase();
        const code = (p.name || '').toLowerCase();
        return title.includes(keyword) || desc.includes(keyword) || code.includes(keyword);
      });

      return matchedPerms.map(p => this.mapToTreeItem(p, 0, []));
    }

    // Nếu không tìm kiếm -> Hiển thị theo cấu trúc Cha - Con của nhóm đang chọn
    const perms = this.currentGroup?.permissions || [];
    const rootPerms = perms.filter(p => !p.parentName);

    return rootPerms.map(root => {
      const children = this.buildChildren(root.name || '', perms, 1);
      return this.mapToTreeItem(root, 0, children);
    });
  }

  private buildChildren(
    parentName: string,
    allGroupPerms: PermissionGrantInfoDto[],
    depth: number
  ): PermissionTreeItem[] {
    const directChildren = allGroupPerms.filter(p => p.parentName === parentName);
    return directChildren.map(child => {
      const grandChildren = this.buildChildren(child.name || '', allGroupPerms, depth + 1);
      return this.mapToTreeItem(child, depth, grandChildren);
    });
  }

  private mapToTreeItem(
    perm: PermissionGrantInfoDto,
    depth: number,
    children: PermissionTreeItem[]
  ): PermissionTreeItem {
    return {
      name: perm.name || '',
      displayName: this.getPermissionDisplayName(perm),
      description: this.getPermissionDescription(perm),
      parentName: perm.parentName,
      isGranted: !!perm.isGranted,
      isDisabled: this.isGrantedByOtherProvider(perm),
      grantedByRoles: this.getGrantedRoleNames(perm),
      depth,
      children,
    };
  }

  // Khi người dùng bật/tắt 1 quyền cụ thể:
  // - Nếu bật quyền Con -> Tự động bật quyền Cha (vì phải có quyền Xem danh sách mới có thể Thêm/Sửa/Xóa)
  // - Nếu tắt quyền Cha -> Tự động tắt toàn bộ các quyền Con phụ thuộc bên dưới
  onPermissionToggle(permName: string, checked: boolean): void {
    const target = this.allPermissionsFlat.find(p => p.name === permName);
    if (!target || this.isGrantedByOtherProvider(target)) return;

    target.isGranted = checked;

    if (checked) {
      // Tự động cấp quyền Cha nếu quyền Con được bật
      this.grantParentChain(target.parentName);
    } else {
      // Tự động thu hồi các quyền Con nếu quyền Cha bị tắt
      this.revokeChildrenChain(target.name);
    }
  }

  private grantParentChain(parentName?: string): void {
    if (!parentName) return;
    const parent = this.allPermissionsFlat.find(p => p.name === parentName);
    if (parent) {
      parent.isGranted = true;
      this.grantParentChain(parent.parentName);
    }
  }

  private revokeChildrenChain(parentName?: string): void {
    if (!parentName) return;
    const children = this.allPermissionsFlat.filter(p => p.parentName === parentName);
    children.forEach(child => {
      if (!this.isGrantedByOtherProvider(child)) {
        child.isGranted = false;
      }
      this.revokeChildrenChain(child.name);
    });
  }

  // Lưu thay đổi phân quyền lên Server
  savePermissions(): void {
    if (this.isSaving) return;

    const updatePayload: UpdatePermissionDto[] = this.allPermissionsFlat.map(p => ({
      name: p.name,
      isGranted: !!p.isGranted,
    }));

    this.isSaving = true;
    this.permissionsService
      .update(this.providerName, this.providerKey, { permissions: updatePayload })
      .pipe(finalize(() => (this.isSaving = false)))
      .subscribe({
        next: () => {
          const targetLabel =
            this.providerName === 'R'
              ? `vai trò "${this.entityTitle || this.providerKey}"`
              : `tài khoản "${this.entityTitle || this.providerKey}"`;
          this.message.success(`Đã cập nhật quyền hạn cho ${targetLabel} thành công!`);
          this.saved.emit();
          this.closeModal();
        },
        error: () => {
          this.message.error('Có lỗi xảy ra khi lưu thiết lập phân quyền. Vui lòng thử lại!');
        },
      });
  }
}

