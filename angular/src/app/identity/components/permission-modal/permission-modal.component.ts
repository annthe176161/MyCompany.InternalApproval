import { ChangeDetectorRef, Component, EventEmitter, Output } from '@angular/core';
import { ConfigStateService } from '@abp/ng.core';
import {
  PermissionGrantInfoDto,
  PermissionGroupDto,
  PermissionsService,
  UpdatePermissionDto,
} from '@abp/ng.permission-management/proxy';
import { NzMessageService } from 'ng-zorro-antd/message';
import { finalize, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';

export interface PermissionRowView {
  name: string;
  displayName: string;
  parentName?: string;
  isGranted: boolean;
  isDisabled: boolean;
  grantedByRoles: string[];
  level: number;
}

@Component({
  selector: 'app-permission-modal',
  standalone: false,
  templateUrl: './permission-modal.component.html',
  styleUrls: ['./permission-modal.component.scss'],
})
export class PermissionModalComponent {
  @Output() saved = new EventEmitter<void>();

  visible = false;
  isLoading = false;
  isSaving = false;

  providerName: 'R' | 'U' = 'R';
  providerKey = '';
  entityTitle = '';

  searchKeyword = '';
  groups: PermissionGroupDto[] = [];
  selectedGroupIndex = 0;

  // Mảng phẳng lưu toàn bộ quyền để tính toán nhanh, không đệ quy trong getter
  allPermissions: PermissionGrantInfoDto[] = [];
  // Bản chụp ban đầu để chỉ gửi những quyền thực sự thay đổi khi bấm Lưu
  private initialGrantMap = new Map<string, boolean>();
  // Mảng hiển thị của nhóm hiện tại (cập nhật tường minh mỗi khi đổi nhóm / tìm kiếm)
  displayedPermissions: PermissionRowView[] = [];

  private readonly groupNameOverrides: Record<string, string> = {
    AbpIdentity: 'Tài khoản & Phân quyền',
    IdentityManagement: 'Tài khoản & Phân quyền',
    SettingManagement: 'Cài đặt quản lý',
    InternalApproval: 'Quản lý Đề xuất Nội bộ',
  };

  private readonly permissionNameOverrides: Record<string, string> = {
    'AbpIdentity.Roles': 'Quản lý vai trò',
    'AbpIdentity.Roles.Create': 'Thêm mới',
    'AbpIdentity.Roles.Update': 'Chỉnh sửa',
    'AbpIdentity.Roles.Delete': 'Xóa',
    'AbpIdentity.Roles.ManagePermissions': 'Phân quyền',

    'AbpIdentity.Users': 'Quản lý tài khoản',
    'AbpIdentity.Users.Create': 'Thêm mới',
    'AbpIdentity.Users.Update': 'Chỉnh sửa thông tin',
    'AbpIdentity.Users.Update.ManageRoles': 'Gán vai trò',
    'AbpIdentity.Users.Delete': 'Xóa',
    'AbpIdentity.Users.ManagePermissions': 'Phân quyền riêng',
    'AbpIdentity.UserLookup': 'Tra cứu người dùng',

    'Requests.Default': 'Quản lý đề xuất nội bộ',
    'Requests.Create': 'Tạo đề xuất',
    'Requests.Edit': 'Sửa đề xuất',
    'Requests.Delete': 'Xóa đề xuất',
    'Requests.Approve': 'Phê duyệt đề xuất',
    'Requests.Reject': 'Từ chối đề xuất',

    'Departments.Default': 'Quản lý phòng ban',
    'Departments.Create': 'Thêm phòng ban',
    'Departments.Edit': 'Sửa phòng ban',
    'Departments.Delete': 'Xóa phòng ban',
    'Departments.ManageMembers': 'Quản lý thành viên phòng ban',

    'Employees.Default': 'Hồ sơ nhân viên',
    'Employees.ViewAll': 'Xem toàn bộ nhân viên',
    'Employees.Edit': 'Cập nhật hồ sơ',
    'Employees.Analytics': 'Xem thống kê',
  };

  constructor(
    private permissionsService: PermissionsService,
    private configState: ConfigStateService,
    private message: NzMessageService,
    private cdr: ChangeDetectorRef
  ) {}

  // Hàm mở modal trực tiếp (giống formModal.open(...) ở trang Đề xuất)
  open(providerName: 'R' | 'U', providerKey: string, title?: string): void {
    if (!providerKey) return;
    this.providerName = providerName;
    this.providerKey = providerKey;
    this.entityTitle = title || providerKey;
    this.searchKeyword = '';
    this.selectedGroupIndex = 0;
    this.groups = [];
    this.allPermissions = [];
    this.displayedPermissions = [];
    this.visible = true;
    this.loadPermissions();
  }

  close(): void {
    if (this.isSaving) return;
    this.visible = false;
  }

  loadPermissions(): void {
    this.isLoading = true;
    this.permissionsService
      .get(this.providerName, this.providerKey)
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: res => {
          this.groups = (res.groups || []).filter(g => g.permissions && g.permissions.length > 0);
          this.allPermissions = [];
          this.initialGrantMap.clear();

          this.groups.forEach(g => {
            (g.permissions || []).forEach(p => {
              this.allPermissions.push(p);
              if (p.name) {
                this.initialGrantMap.set(p.name, !!p.isGranted);
              }
            });
          });

          if (!this.entityTitle && res.entityDisplayName) {
            this.entityTitle = res.entityDisplayName;
          }

          this.selectedGroupIndex = 0;
          this.rebuildDisplayedList();
        },
        error: () => {
          this.message.error('Không thể tải danh sách quyền hạn. Vui lòng thử lại!');
          this.visible = false;
        },
      });
  }

  selectGroup(index: number): void {
    this.selectedGroupIndex = index;
    this.rebuildDisplayedList();
  }

  onSearchChange(): void {
    this.rebuildDisplayedList();
  }

  getGroupDisplayName(group: PermissionGroupDto): string {
    if (group.name && this.groupNameOverrides[group.name]) {
      return this.groupNameOverrides[group.name];
    }
    return group.displayName || group.name || '';
  }

  getPermissionDisplayName(perm: PermissionGrantInfoDto): string {
    if (perm.name && this.permissionNameOverrides[perm.name]) {
      return this.permissionNameOverrides[perm.name];
    }
    return perm.displayName || perm.name || '';
  }

  getGrantedCountInGroup(group: PermissionGroupDto): number {
    return (group.permissions || []).filter(p => p.isGranted).length;
  }

  isGrantedByOtherProvider(perm: PermissionGrantInfoDto): boolean {
    if (this.providerName !== 'U' || !perm.grantedProviders?.length) {
      return false;
    }
    return perm.grantedProviders.some(p => p.providerName === 'R');
  }

  private getGrantedRoles(perm: PermissionGrantInfoDto): string[] {
    if (this.providerName !== 'U' || !perm.grantedProviders?.length) return [];
    return perm.grantedProviders
      .filter(p => p.providerName === 'R' && p.providerKey)
      .map(p => p.providerKey as string);
  }

  // Tính mức thụt lề (0 = Cha, 1 = Con, 2 = Cháu)
  private calculateLevel(perm: PermissionGrantInfoDto, groupPerms: PermissionGrantInfoDto[]): number {
    let level = 0;
    let currentParentName = perm.parentName;
    while (currentParentName && level < 3) {
      level++;
      const parent = groupPerms.find(p => p.name === currentParentName);
      currentParentName = parent?.parentName;
    }
    return level;
  }

  // Cập nhật mảng hiển thị (chỉ chạy khi có tương tác, không tạo mảng mới trong template)
  rebuildDisplayedList(): void {
    const keyword = this.searchKeyword.trim().toLowerCase();

    if (keyword) {
      this.displayedPermissions = this.allPermissions
        .filter(p => {
          const label = this.getPermissionDisplayName(p).toLowerCase();
          const code = (p.name || '').toLowerCase();
          return label.includes(keyword) || code.includes(keyword);
        })
        .map(p => ({
          name: p.name || '',
          displayName: this.getPermissionDisplayName(p),
          parentName: p.parentName,
          isGranted: !!p.isGranted,
          isDisabled: this.isGrantedByOtherProvider(p),
          grantedByRoles: this.getGrantedRoles(p),
          level: 0,
        }));
      return;
    }

    const currentGroup = this.groups[this.selectedGroupIndex];
    const perms = currentGroup?.permissions || [];

    this.displayedPermissions = perms.map(p => ({
      name: p.name || '',
      displayName: this.getPermissionDisplayName(p),
      parentName: p.parentName,
      isGranted: !!p.isGranted,
      isDisabled: this.isGrantedByOtherProvider(p),
      grantedByRoles: this.getGrantedRoles(p),
      level: this.calculateLevel(p, perms),
    }));
  }

  // Trạng thái "Cấp tất cả quyền"
  get isAllGranted(): boolean {
    return this.allPermissions.length > 0 && this.allPermissions.every(p => p.isGranted);
  }

  get isAllIndeterminate(): boolean {
    const granted = this.allPermissions.filter(p => p.isGranted).length;
    return granted > 0 && granted < this.allPermissions.length;
  }

  toggleAllPermissions(checked: boolean): void {
    this.allPermissions.forEach(p => {
      if (!this.isGrantedByOtherProvider(p)) {
        p.isGranted = checked;
      } else {
        p.isGranted = true;
      }
    });
    this.rebuildDisplayedList();
  }

  // Trạng thái "Chọn tất cả trong nhóm"
  get isCurrentGroupAllGranted(): boolean {
    const perms = this.groups[this.selectedGroupIndex]?.permissions || [];
    return perms.length > 0 && perms.every(p => p.isGranted);
  }

  get isCurrentGroupIndeterminate(): boolean {
    const perms = this.groups[this.selectedGroupIndex]?.permissions || [];
    const granted = perms.filter(p => p.isGranted).length;
    return granted > 0 && granted < perms.length;
  }

  toggleCurrentGroupPermissions(checked: boolean): void {
    const perms = this.groups[this.selectedGroupIndex]?.permissions || [];
    perms.forEach(p => {
      if (!this.isGrantedByOtherProvider(p)) {
        p.isGranted = checked;
      } else {
        p.isGranted = true;
      }
    });
    this.rebuildDisplayedList();
  }

  // Bật / tắt từng quyền đơn lẻ
  onTogglePermission(row: PermissionRowView, checked: boolean): void {
    if (row.isDisabled) return;

    const target = this.allPermissions.find(p => p.name === row.name);
    if (!target) return;

    target.isGranted = checked;

    if (checked) {
      this.grantParentChain(target.parentName);
    } else {
      this.revokeChildrenChain(target.name);
    }

    this.rebuildDisplayedList();
  }

  private grantParentChain(parentName?: string): void {
    if (!parentName) return;
    const parent = this.allPermissions.find(p => p.name === parentName);
    if (parent) {
      parent.isGranted = true;
      this.grantParentChain(parent.parentName);
    }
  }

  private revokeChildrenChain(parentName?: string): void {
    if (!parentName) return;
    this.allPermissions
      .filter(p => p.parentName === parentName)
      .forEach(child => {
        if (!this.isGrantedByOtherProvider(child)) {
          child.isGranted = false;
        }
        this.revokeChildrenChain(child.name);
      });
  }

  private shouldRefreshAppConfig(): boolean {
    const currentUser = this.configState.getOne('currentUser');
    if (!currentUser) return false;
    if (this.providerName === 'R') {
      return (currentUser.roles || []).includes(this.providerKey);
    }
    if (this.providerName === 'U') {
      return currentUser.id === this.providerKey;
    }
    return false;
  }

  save(): void {
    if (this.isSaving) return;

    // Chỉ lọc ra những quyền có thay đổi so với ban đầu
    const changedPermissions: UpdatePermissionDto[] = this.allPermissions
      .filter(p => p.name && this.initialGrantMap.get(p.name) !== !!p.isGranted)
      .map(p => ({
        name: p.name,
        isGranted: !!p.isGranted,
      }));

    if (changedPermissions.length === 0) {
      this.message.info('Không có thay đổi nào về quyền hạn.');
      this.visible = false;
      return;
    }

    this.isSaving = true;
    this.permissionsService
      .update(this.providerName, this.providerKey, { permissions: changedPermissions })
      .pipe(
        switchMap(() =>
          this.shouldRefreshAppConfig() ? this.configState.refreshAppState() : of(null)
        ),
        finalize(() => {
          this.isSaving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: () => {
          this.message.success(`Đã lưu phân quyền cho "${this.entityTitle}" thành công!`);
          this.visible = false;
          this.saved.emit();
        },
        error: () => {
          this.message.error('Không thể lưu phân quyền. Vui lòng thử lại!');
        },
      });
  }
}
